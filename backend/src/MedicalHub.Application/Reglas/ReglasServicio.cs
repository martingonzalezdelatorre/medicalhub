using System.Text.Json;
using MedicalHub.Application.Comun;
using MedicalHub.Domain.Reglas;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Reglas;

/// <summary>Configuración de las reglas de planificación de la sociedad.</summary>
public sealed class ReglasServicio(IAppDbContext db, IContextoActual contexto, TimeProvider reloj)
{
    public IReadOnlyList<DefinicionTipoRegla> Tipos() => ParametrosRegla.Catalogo;

    public async Task<IReadOnlyList<ReglaDto>> ListarAsync(CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var reglas = await db.Reglas.OrderBy(r => r.Tipo).ThenBy(r => r.Nombre).ToListAsync(ct);
        return await ComponerAsync(reglas, ct);
    }

    public async Task<ReglaDto> CrearAsync(SolicitudRegla s, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        var parametros = await ValidarAsync(s, ct);
        var regla = new ReglaPlanificacion(org, s.Tipo, s.Nombre, s.Descripcion, s.ServicioId, s.Severidad, s.PermiteExcepcion,
            s.ExcepcionRequiereAdministrador, parametros, s.Fundamento, reloj.GetUtcNow());
        db.Reglas.Add(regla);
        await db.SaveChangesAsync(ct);
        return (await ComponerAsync([regla], ct)).Single();
    }

    public async Task<ReglaDto> ActualizarAsync(Guid id, SolicitudRegla s, CancellationToken ct)
    {
        var regla = await BuscarAsync(id, ct);
        if (s.Tipo != regla.Tipo)
            throw new ExcepcionValidacion(new Dictionary<string, string[]> { [nameof(s.Tipo)] = ["No se puede cambiar el tipo de una regla; cree una nueva."] });
        var parametros = await ValidarAsync(s, ct);
        regla.Actualizar(s.Nombre, s.Descripcion, s.ServicioId, s.Severidad, s.PermiteExcepcion, s.ExcepcionRequiereAdministrador, parametros, s.Fundamento);
        await db.SaveChangesAsync(ct);
        return (await ComponerAsync([regla], ct)).Single();
    }

    public async Task CambiarEstadoAsync(Guid id, bool activa, CancellationToken ct)
    {
        var regla = await BuscarAsync(id, ct);
        if (activa) regla.Activar(); else regla.Desactivar();
        await db.SaveChangesAsync(ct);
    }

    /// <summary>La sociedad confirma que ha revisado la regla (queda registrado quién y cuándo).</summary>
    public async Task<ReglaDto> RevisarAsync(Guid id, CancellationToken ct)
    {
        var uid = contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");
        var regla = await BuscarAsync(id, ct);
        regla.MarcarRevisada(uid, reloj.GetUtcNow());
        await db.SaveChangesAsync(ct);
        return (await ComponerAsync([regla], ct)).Single();
    }

    private async Task<ReglaPlanificacion> BuscarAsync(Guid id, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        return await db.Reglas.FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw new ExcepcionNoEncontrado("Regla no encontrada.");
    }

    // ── Validación de parámetros según el tipo ──

    private async Task<string?> ValidarAsync(SolicitudRegla s, CancellationToken ct)
    {
        var v = new Validador().Requerido(nameof(s.Nombre), s.Nombre, 150);
        v.Cuando(!Enum.IsDefined(s.Tipo), nameof(s.Tipo), "El tipo de regla no es válido.");
        v.Cuando(!Enum.IsDefined(s.Severidad), nameof(s.Severidad), "La severidad no es válida.");
        v.LanzarSiHayErrores();

        if (s.ServicioId is { } sid && !await db.Servicios.AnyAsync(x => x.Id == sid, ct))
            v.Agregar(nameof(s.ServicioId), "El servicio no existe.");

        var definicion = ParametrosRegla.Definicion(s.Tipo);
        var valores = new Dictionary<string, object>();
        var entrada = s.Parametros is { ValueKind: JsonValueKind.Object } p
            ? p.EnumerateObject().ToDictionary(x => x.Name.ToLowerInvariant(), x => x.Value) : [];

        foreach (var def in definicion.Parametros)
        {
            var campo = $"Parametros.{def.Clave}";
            if (!entrada.TryGetValue(def.Clave.ToLowerInvariant(), out var valor)) { v.Agregar(campo, "Es obligatorio."); continue; }

            if (def.Tipo == "entero")
            {
                if (valor.ValueKind != JsonValueKind.Number || !valor.TryGetInt32(out var n)) v.Agregar(campo, "Debe ser un número entero.");
                else if (n < def.Minimo || n > def.Maximo) v.Agregar(campo, $"Debe estar entre {def.Minimo} y {def.Maximo}.");
                else valores[def.Clave] = n;
            }
            else // servicio
            {
                if (valor.ValueKind != JsonValueKind.String || !Guid.TryParse(valor.GetString(), out var g)) v.Agregar(campo, "Debe ser un servicio válido.");
                else if (!await db.Servicios.AnyAsync(x => x.Id == g, ct)) v.Agregar(campo, "El servicio no existe.");
                else valores[def.Clave] = g;
            }
        }
        if (s.Tipo == TipoRegla.IncompatibilidadServicios && valores.TryGetValue("servicioAId", out var a) && valores.TryGetValue("servicioBId", out var b) && a.Equals(b))
            v.Agregar("Parametros.servicioBId", "Los dos servicios deben ser distintos.");
        v.LanzarSiHayErrores();

        return definicion.Parametros.Count == 0 ? null : JsonSerializer.Serialize(valores, ParametrosRegla.Json);
    }

    private async Task<IReadOnlyList<ReglaDto>> ComponerAsync(List<ReglaPlanificacion> reglas, CancellationToken ct)
    {
        var servicios = await db.Servicios.ToDictionaryAsync(x => x.Id, x => x.Nombre, ct);
        var usuarioIds = reglas.Where(r => r.RevisadaPorUsuarioId != null).Select(r => r.RevisadaPorUsuarioId!.Value).Distinct().ToList();
        var usuarios = await db.Usuarios.Where(u => usuarioIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.NombreCompleto, ct);

        return reglas.Select(r => new ReglaDto(r.Id, r.Tipo, r.Nombre, r.Descripcion, r.ServicioId,
            r.ServicioId is { } s ? servicios.GetValueOrDefault(s) : null, r.Severidad, r.Activa, r.PermiteExcepcion, r.ExcepcionRequiereAdministrador,
            r.ParametrosJson is null ? null : JsonDocument.Parse(r.ParametrosJson).RootElement.Clone(), r.Fundamento, r.RevisadaEl,
            r.RevisadaPorUsuarioId is { } u ? usuarios.GetValueOrDefault(u) : null, r.RevisadaEl is null)).ToList();
    }
}

/// <summary>Consulta del registro de excepciones autorizadas.</summary>
public sealed class ExcepcionesServicio(IAppDbContext db, IContextoActual contexto)
{
    public async Task<IReadOnlyList<ExcepcionDto>> ListarAsync(DateOnly? desde, DateOnly? hasta, Guid? profesionalId, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var q = db.Excepciones.AsQueryable();
        if (profesionalId is { } p) q = q.Where(e => e.ProfesionalId == p);
        if (desde is { } d) { var ini = new DateTimeOffset(d.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero); q = q.Where(e => e.Fecha >= ini); }
        if (hasta is { } h) { var fin = new DateTimeOffset(h.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero); q = q.Where(e => e.Fecha < fin); }

        var lista = await q.OrderByDescending(e => e.Fecha).Take(500).ToListAsync(ct);
        var profIds = lista.Select(e => e.ProfesionalId).Distinct().ToList();
        var turnoIds = lista.Select(e => e.TurnoId).Distinct().ToList();
        var asigIds = lista.Select(e => e.AsignacionId).ToList();
        var usuarioIds = lista.Select(e => e.AutorizadaPorUsuarioId).Distinct().ToList();

        var profesionales = await db.Profesionales.Where(x => profIds.Contains(x.Id)).Select(x => new { x.Id, x.Nombre, x.Apellidos }).ToDictionaryAsync(x => x.Id, x => $"{x.Nombre} {x.Apellidos}", ct);
        var turnos = await (from t in db.Turnos where turnoIds.Contains(t.Id)
                            join tt in db.TiposTurno on t.TipoTurnoId equals tt.Id
                            join s in db.Servicios on t.ServicioId equals s.Id
                            select new { t.Id, t.Fecha, Tipo = tt.Nombre, Servicio = s.Nombre }).ToListAsync(ct);
        var textos = turnos.ToDictionary(t => t.Id, t => $"{t.Tipo} del {t.Fecha:dd/MM/yyyy} ({t.Servicio})");
        var activas = (await db.Asignaciones.Where(a => asigIds.Contains(a.Id) && a.Estado != Domain.Planificacion.EstadoAsignacion.Cancelada).Select(a => a.Id).ToListAsync(ct)).ToHashSet();
        var usuarios = await db.Usuarios.Where(u => usuarioIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.NombreCompleto, ct);
        var reglas = await db.Reglas.ToDictionaryAsync(r => r.Id, r => r.Nombre, ct);

        return lista.Select(e => new ExcepcionDto(e.Id, e.Fecha, e.ProfesionalId, profesionales.GetValueOrDefault(e.ProfesionalId, "—"), e.TurnoId,
            textos.GetValueOrDefault(e.TurnoId, "—"), e.Codigo, e.ReglaId is { } r && reglas.TryGetValue(r, out var n) ? n : e.Codigo, e.Mensaje,
            e.Justificacion, usuarios.GetValueOrDefault(e.AutorizadaPorUsuarioId, "—"), activas.Contains(e.AsignacionId))).ToList();
    }
}
