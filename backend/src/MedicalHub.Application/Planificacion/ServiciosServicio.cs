using MedicalHub.Application.Comun;
using MedicalHub.Domain.Estructura;
using MedicalHub.Domain.Identidad;
using MedicalHub.Domain.Profesionales;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Planificacion;

public sealed class ServiciosServicio(IAppDbContext db, IContextoActual contexto, TimeProvider reloj)
{
    public async Task<IReadOnlyList<ServicioDto>> ListarAsync(Guid? centroId, bool incluirInactivos, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var q = Visibles();
        if (centroId is { } c) q = q.Where(s => s.CentroId == c);
        if (!incluirInactivos) q = q.Where(s => s.Activo);
        var servicios = await q.OrderBy(s => s.Nombre).ToListAsync(ct);
        return await ComponerAsync(servicios, ct);
    }

    public async Task<ServicioDto> ObtenerAsync(Guid id, CancellationToken ct)
        => (await ComponerAsync([await BuscarAsync(id, ct)], ct)).Single();

    public async Task<ServicioDto> CrearAsync(SolicitudServicio s, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        var v = new Validador().Requerido(nameof(s.Nombre), s.Nombre);
        await ValidarReferenciasAsync(s, v, ct);
        v.LanzarSiHayErrores();

        if (!await db.Centros.AnyAsync(c => c.Id == s.CentroId, ct))
            throw new ExcepcionNoEncontrado("Centro no encontrado.");
        await ComprobarDuplicadoAsync(s.CentroId, s.Nombre, null, ct);

        var servicio = new Servicio(org, s.CentroId, s.Nombre, s.Descripcion, reloj.GetUtcNow());
        Aplicar(servicio, s);
        db.Servicios.Add(servicio);
        await db.SaveChangesAsync(ct);
        return await ObtenerAsync(servicio.Id, ct);
    }

    public async Task<ServicioDto> ActualizarAsync(Guid id, SolicitudServicio s, CancellationToken ct)
    {
        var v = new Validador().Requerido(nameof(s.Nombre), s.Nombre);
        await ValidarReferenciasAsync(s, v, ct);
        v.LanzarSiHayErrores();

        var servicio = await BuscarAsync(id, ct);
        await ComprobarDuplicadoAsync(servicio.CentroId, s.Nombre, id, ct);
        servicio.Actualizar(s.Nombre, s.Descripcion);
        Aplicar(servicio, s);
        await db.SaveChangesAsync(ct);
        return await ObtenerAsync(id, ct);
    }

    public async Task CambiarEstadoAsync(Guid id, bool activo, CancellationToken ct)
    {
        var servicio = await BuscarAsync(id, ct);
        if (activo) servicio.Activar(); else servicio.Desactivar();
        await db.SaveChangesAsync(ct);
    }

    // ── Tipos de turno ──

    public async Task<TipoTurnoDto> CrearTipoTurnoAsync(Guid servicioId, SolicitudTipoTurno s, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        new Validador().Requerido(nameof(s.Nombre), s.Nombre, 100).LanzarSiHayErrores();
        await BuscarAsync(servicioId, ct);
        await ComprobarDuplicadoTipoAsync(servicioId, s.Nombre, null, ct);

        var tipo = new TipoTurno(org, servicioId, s.Nombre, s.HoraInicio, s.HoraFin, s.EsGuardia);
        db.TiposTurno.Add(tipo);
        await db.SaveChangesAsync(ct);
        return ATipoDto(tipo);
    }

    public async Task<TipoTurnoDto> ActualizarTipoTurnoAsync(Guid servicioId, Guid tipoId, SolicitudTipoTurno s, CancellationToken ct)
    {
        new Validador().Requerido(nameof(s.Nombre), s.Nombre, 100).LanzarSiHayErrores();
        var tipo = await BuscarTipoAsync(servicioId, tipoId, ct);
        await ComprobarDuplicadoTipoAsync(servicioId, s.Nombre, tipoId, ct);
        tipo.Actualizar(s.Nombre, s.HoraInicio, s.HoraFin, s.EsGuardia);
        await db.SaveChangesAsync(ct);
        return ATipoDto(tipo);
    }

    public async Task CambiarEstadoTipoTurnoAsync(Guid servicioId, Guid tipoId, bool activo, CancellationToken ct)
    {
        var tipo = await BuscarTipoAsync(servicioId, tipoId, ct);
        if (activo) tipo.Activar(); else tipo.Desactivar();
        await db.SaveChangesAsync(ct);
    }

    // ── Requisitos de cobertura ──

    /// <summary>Sustituye el conjunto de requisitos del servicio (conserva los existentes que no cambian).</summary>
    public async Task<IReadOnlyList<RequisitoDto>> ReemplazarRequisitosAsync(Guid servicioId, SolicitudRequisitos s, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        await BuscarAsync(servicioId, ct);
        var lineas = s.Requisitos ?? [];

        var v = new Validador();
        v.Cuando(lineas.GroupBy(l => (l.TipoTurnoId, l.CategoriaProfesionalId)).Any(g => g.Count() > 1),
            nameof(s.Requisitos), "Hay requisitos duplicados para el mismo tipo de turno y categoría.");
        var tiposServicio = await db.TiposTurno.Where(t => t.ServicioId == servicioId && t.Activo).Select(t => t.Id).ToListAsync(ct);
        v.Cuando(lineas.Any(l => !tiposServicio.Contains(l.TipoTurnoId)), nameof(s.Requisitos),
            "Todos los tipos de turno deben pertenecer al servicio y estar activos.");
        await db.CategoriasProfesionales.ExigirExistentesAsync(lineas.Where(l => l.CategoriaProfesionalId != null).Select(l => l.CategoriaProfesionalId!.Value),
            nameof(s.Requisitos), v, ct);
        v.LanzarSiHayErrores();

        var existentes = await db.RequisitosCobertura.Where(r => r.ServicioId == servicioId).ToListAsync(ct);
        foreach (var r in existentes.Where(r => !lineas.Any(l => l.TipoTurnoId == r.TipoTurnoId && l.CategoriaProfesionalId == r.CategoriaProfesionalId)))
            db.RequisitosCobertura.Remove(r);
        foreach (var l in lineas)
        {
            var actual = existentes.FirstOrDefault(r => r.TipoTurnoId == l.TipoTurnoId && r.CategoriaProfesionalId == l.CategoriaProfesionalId);
            if (actual is null) db.RequisitosCobertura.Add(new RequisitoCobertura(org, servicioId, l.TipoTurnoId, l.NumeroProfesionales, l.CategoriaProfesionalId));
            else actual.CambiarNumero(l.NumeroProfesionales);
        }
        await db.SaveChangesAsync(ct);
        return (await db.RequisitosCobertura.Where(r => r.ServicioId == servicioId).ToListAsync(ct)).Select(ARequisitoDto).ToList();
    }

    // ── Candidatos ──

    /// <summary>Profesionales que cumplen (o no) las condiciones de dominio para el servicio en una fecha, con el motivo.</summary>
    public async Task<IReadOnlyList<ElegibilidadDto>> EvaluarProfesionalesAsync(Guid servicioId, DateOnly fecha, bool incluirNoElegibles, CancellationToken ct)
    {
        var servicio = await BuscarAsync(servicioId, ct);
        var profesionales = await db.Profesionales.Where(p => p.Activo).OrderBy(p => p.Apellidos).ThenBy(p => p.Nombre).ToListAsync(ct);
        var competencias = await db.Competencias.ToDictionaryAsync(c => c.Id, c => c.Nombre, ct);

        return profesionales
            .Select(p => (p, r: EvaluadorElegibilidad.Evaluar(p, servicio, fecha, competencias)))
            .Where(x => incluirNoElegibles || x.r.Elegible)
            .Select(x => new ElegibilidadDto(x.p.Id, x.p.NombreCompleto, x.r.Elegible, x.r.Motivos))
            .ToList();
    }

    // ── Internos ──

    private IQueryable<Servicio> Visibles()
    {
        if (!contexto.EsAlcanceRestringido()) return db.Servicios;
        var centros = CentrosServicio.Visibles(db, contexto).Select(c => c.Id);
        return db.Servicios.Where(s => centros.Contains(s.CentroId));
    }

    private async Task<Servicio> BuscarAsync(Guid id, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        return await Visibles().FirstOrDefaultAsync(s => s.Id == id, ct) ?? throw new ExcepcionNoEncontrado("Servicio no encontrado.");
    }

    private async Task<TipoTurno> BuscarTipoAsync(Guid servicioId, Guid tipoId, CancellationToken ct)
    {
        await BuscarAsync(servicioId, ct);
        return await db.TiposTurno.FirstOrDefaultAsync(t => t.Id == tipoId && t.ServicioId == servicioId, ct)
            ?? throw new ExcepcionNoEncontrado("Tipo de turno no encontrado.");
    }

    private async Task ValidarReferenciasAsync(SolicitudServicio s, Validador v, CancellationToken ct)
    {
        await db.CategoriasProfesionales.ExigirExistentesAsync(s.CategoriasAdmitidasIds, nameof(s.CategoriasAdmitidasIds), v, ct);
        await db.Competencias.ExigirExistentesAsync(s.CompetenciasRequeridasIds, nameof(s.CompetenciasRequeridasIds), v, ct);
        if (s.CoordinadorUsuarioId is { } cu
            && !await db.Usuarios.AnyAsync(u => u.Id == cu && u.Activo && u.Roles.Any(r => r.Rol == Rol.CoordinadorMedico), ct))
            v.Agregar(nameof(s.CoordinadorUsuarioId), "El coordinador debe ser un usuario activo con rol de coordinador médico.");
    }

    private static void Aplicar(Servicio servicio, SolicitudServicio s)
    {
        servicio.EstablecerCoordinador(s.CoordinadorUsuarioId);
        servicio.EstablecerConfiguracionReglas(s.ConfiguracionReglas);
        servicio.EstablecerCategoriasAdmitidas(s.CategoriasAdmitidasIds ?? []);
        servicio.EstablecerCompetenciasRequeridas(s.CompetenciasRequeridasIds ?? []);
    }

    private async Task ComprobarDuplicadoAsync(Guid centroId, string nombre, Guid? excepto, CancellationToken ct)
    {
        var n = nombre.Trim();
        if (await db.Servicios.AnyAsync(x => x.CentroId == centroId && x.Nombre == n && x.Id != excepto, ct))
            throw new ExcepcionConflicto("Ya existe un servicio con ese nombre en el centro.");
    }

    private async Task ComprobarDuplicadoTipoAsync(Guid servicioId, string nombre, Guid? excepto, CancellationToken ct)
    {
        var n = nombre.Trim();
        if (await db.TiposTurno.AnyAsync(t => t.ServicioId == servicioId && t.Nombre == n && t.Id != excepto, ct))
            throw new ExcepcionConflicto("Ya existe un tipo de turno con ese nombre en el servicio.");
    }

    private async Task<IReadOnlyList<ServicioDto>> ComponerAsync(List<Servicio> servicios, CancellationToken ct)
    {
        var ids = servicios.Select(s => s.Id).ToList();
        var centroIds = servicios.Select(x => x.CentroId).Distinct().ToList();
        var centros = await db.Centros.Where(c => centroIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Nombre, ct);
        var tipos = await db.TiposTurno.Where(t => ids.Contains(t.ServicioId)).OrderBy(t => t.HoraInicio).ToListAsync(ct);
        var requisitos = await db.RequisitosCobertura.Where(r => ids.Contains(r.ServicioId)).ToListAsync(ct);

        return servicios.Select(s => new ServicioDto(
            s.Id, s.CentroId, centros.GetValueOrDefault(s.CentroId, ""), s.Nombre, s.Descripcion, s.CoordinadorUsuarioId,
            s.ConfiguracionReglas, s.Activo,
            s.CategoriasAdmitidas.Select(c => c.CategoriaProfesionalId).ToList(),
            s.CompetenciasRequeridas.Select(c => c.CompetenciaId).ToList(),
            tipos.Where(t => t.ServicioId == s.Id).Select(ATipoDto).ToList(),
            requisitos.Where(r => r.ServicioId == s.Id).Select(ARequisitoDto).ToList())).ToList();
    }

    private static TipoTurnoDto ATipoDto(TipoTurno t) => new(t.Id, t.Nombre, t.HoraInicio, t.HoraFin, t.EsGuardia, t.Activo,
        t.CruzaMedianoche, t.Duracion.TotalHours);

    private static RequisitoDto ARequisitoDto(RequisitoCobertura r) => new(r.Id, r.TipoTurnoId, r.NumeroProfesionales, r.CategoriaProfesionalId);
}
