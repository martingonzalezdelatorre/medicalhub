using MedicalHub.Application.Comun;
using MedicalHub.Application.Reglas;
using MedicalHub.Domain.Ausencias;
using MedicalHub.Domain.Comun;
using MedicalHub.Domain.Planificacion;
using MedicalHub.Domain.Profesionales;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Ausencias;

/// <summary>
/// Solicitud, decisión y efectos de las ausencias. Al aprobar una ausencia, las asignaciones futuras que caen en ella se
/// cancelan y cada plaza liberada pasa a ser una <see cref="Vacante"/> abierta, de forma transaccional.
/// No se piden ni guardan diagnósticos: solo el tipo y un comentario opcional.
/// </summary>
public sealed class AusenciasServicio(IAppDbContext db, IContextoActual contexto, TimeProvider reloj, EvaluacionServicio evaluacion)
{
    public const string MotivoVacante = "Ausencia aprobada";

    // ───────────── Del propio profesional ─────────────

    public async Task<IReadOnlyList<AusenciaDto>> MisAusenciasAsync(CancellationToken ct)
    {
        var p = await ProfesionalPropioAsync(ct);
        var lista = await db.Ausencias.Where(a => a.ProfesionalId == p.Id).OrderByDescending(a => a.Desde).Take(200).ToListAsync(ct);
        return await ComponerAsync(lista, ct);
    }

    public async Task<AusenciaDto> SolicitarPropiaAsync(SolicitudAusencia s, CancellationToken ct)
    {
        var p = await ProfesionalPropioAsync(ct);
        var hoy = Hoy();
        if (s.Desde < hoy.AddDays(-1))
            throw new ExcepcionValidacion(new Dictionary<string, string[]> { [nameof(s.Desde)] = ["No se pueden solicitar ausencias que ya han empezado. Contacte con su coordinador."] });
        return await CrearAsync(p.Id, s.Tipo, s.Desde, s.Hasta, s.Comentario, aprobar: false, ct);
    }

    public async Task<AusenciaDto> CancelarPropiaAsync(Guid id, CancellationToken ct)
    {
        var p = await ProfesionalPropioAsync(ct);
        var a = await db.Ausencias.FirstOrDefaultAsync(x => x.Id == id && x.ProfesionalId == p.Id, ct) ?? throw new ExcepcionNoEncontrado("Ausencia no encontrada.");
        if (a.Estado != EstadoAusencia.Solicitada)
            throw new ExcepcionConflicto("Solo puede cancelar una solicitud pendiente. Para una ausencia ya aprobada, contacte con su coordinador.");
        a.Cancelar();
        db.DecisionesAusencia.Add(new DecisionAusencia(a.OrganizacionId!.Value, a.Id, EstadoAusencia.Cancelada, UsuarioActual(), reloj.GetUtcNow(), "Cancelada por el profesional"));
        await db.SaveChangesAsync(ct);
        return (await ComponerAsync([a], ct)).Single();
    }

    // ───────────── Gestión (administrador y coordinador) ─────────────

    public async Task<IReadOnlyList<AusenciaDto>> ListarAsync(EstadoAusencia? estado, Guid? profesionalId, DateOnly? desde, DateOnly? hasta, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var q = db.Ausencias.AsQueryable();
        if (estado is { } e) q = q.Where(a => a.Estado == e);
        if (profesionalId is { } p) q = q.Where(a => a.ProfesionalId == p);
        if (desde is { } d) q = q.Where(a => a.Hasta >= d);
        if (hasta is { } h) q = q.Where(a => a.Desde <= h);
        var lista = await q.OrderBy(a => a.Estado == EstadoAusencia.Solicitada ? 0 : 1).ThenByDescending(a => a.Desde).Take(300).ToListAsync(ct);
        return await ComponerAsync(lista, ct);
    }

    public async Task<AusenciaDto> ObtenerAsync(Guid id, CancellationToken ct)
        => (await ComponerAsync([await BuscarAsync(id, ct)], ct)).Single();

    /// <summary>El coordinador registra una ausencia de cualquier profesional (con o sin aprobación inmediata).</summary>
    public async Task<ResultadoAusenciaDto> RegistrarAsync(SolicitudAusenciaDeProfesional s, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        if (!await db.Profesionales.AnyAsync(p => p.Id == s.ProfesionalId, ct)) throw new ExcepcionNoEncontrado("Profesional no encontrado.");
        var ausencia = await CrearAsync(s.ProfesionalId, s.Tipo, s.Desde, s.Hasta, s.Comentario, s.Aprobar, ct);
        return s.Aprobar
            ? await DecidirAsync(ausencia.Id, aprobar: true, motivo: "Registrada y aprobada por el coordinador", yaCreada: true, ct)
            : new ResultadoAusenciaDto(ausencia, []);
    }

    /// <summary>Turnos futuros del profesional que caen dentro de la ausencia (antes o después de decidir).</summary>
    public async Task<IReadOnlyList<TurnoComprometidoDto>> ComprometidosAsync(Guid id, CancellationToken ct)
    {
        var a = await BuscarAsync(id, ct);
        return await TurnosComprometidosAsync(a, ct);
    }

    public Task<ResultadoAusenciaDto> AprobarAsync(Guid id, string? motivo, CancellationToken ct)
        => DecidirAsync(id, aprobar: true, motivo, yaCreada: false, ct);

    public async Task<ResultadoAusenciaDto> RechazarAsync(Guid id, string? motivo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ExcepcionValidacion(new Dictionary<string, string[]> { [nameof(motivo)] = ["Indique el motivo del rechazo."] });
        return await DecidirAsync(id, aprobar: false, motivo, yaCreada: false, ct);
    }

    /// <summary>Cancela una ausencia vigente. Las vacantes ya generadas se conservan: el coordinador decide si seguir cubriéndolas.</summary>
    public async Task<AusenciaDto> CancelarAsync(Guid id, string? motivo, CancellationToken ct)
    {
        var a = await BuscarAsync(id, ct);
        if (!a.EstaVigente) throw new ExcepcionConflicto("La ausencia ya no está vigente.");
        a.Cancelar();
        db.DecisionesAusencia.Add(new DecisionAusencia(a.OrganizacionId!.Value, a.Id, EstadoAusencia.Cancelada, UsuarioActual(), reloj.GetUtcNow(), motivo));
        await db.SaveChangesAsync(ct);
        return (await ComponerAsync([a], ct)).Single();
    }

    // ───────────── Internos ─────────────

    private async Task<ResultadoAusenciaDto> DecidirAsync(Guid id, bool aprobar, string? motivo, bool yaCreada, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        var uid = UsuarioActual();

        var vacantes = await db.EnTransaccionAsync(async token =>
        {
            var previa = await db.Ausencias.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, token) ?? throw new ExcepcionNoEncontrado("Ausencia no encontrada.");
            // Serializa con las asignaciones de ese profesional mientras se decide
            await db.BloquearAsync($"profesional:{previa.ProfesionalId}", token);
            var ausencia = await db.Ausencias.FirstAsync(a => a.Id == id, token);
            var ahora = reloj.GetUtcNow();

            if (ausencia.Estado != EstadoAusencia.Solicitada) throw new ExcepcionConflicto("La solicitud ya ha sido resuelta.");

            var resultado = new List<TurnoComprometidoDto>();
            if (aprobar)
            {
                ausencia.Aprobar();
                var comprometidos = await TurnosComprometidosAsync(ausencia, token);
                foreach (var recurso in comprometidos.Select(c => $"necesidad:{c.NecesidadId}").Distinct().Order(StringComparer.Ordinal))
                    await db.BloquearAsync(recurso, token);

                var idsAsignacion = comprometidos.Select(c => c.AsignacionId).ToList();
                var asignaciones = await db.Asignaciones.Where(a => idsAsignacion.Contains(a.Id)).ToListAsync(token);
                foreach (var asignacion in asignaciones)
                {
                    asignacion.Cancelar(ahora, MotivoVacante);
                    db.Vacantes.Add(new Vacante(org, asignacion.NecesidadId, asignacion.TurnoId, asignacion.Id, ausencia.ProfesionalId, ausencia.Id, MotivoVacante, ahora));
                }
                resultado.AddRange(comprometidos);
            }
            else ausencia.Rechazar();

            db.DecisionesAusencia.Add(new DecisionAusencia(org, ausencia.Id, aprobar ? EstadoAusencia.Aprobada : EstadoAusencia.Rechazada, uid, ahora, motivo));
            await db.SaveChangesAsync(token);
            return resultado;
        }, ct);

        var dto = (await ComponerAsync([await db.Ausencias.FirstAsync(a => a.Id == id, ct)], ct)).Single();
        return new ResultadoAusenciaDto(dto, vacantes);
    }

    private async Task<AusenciaDto> CrearAsync(Guid profesionalId, TipoAusencia tipo, DateOnly desde, DateOnly hasta, string? comentario, bool aprobar, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        var uid = UsuarioActual();
        if (await db.Ausencias.AnyAsync(a => a.ProfesionalId == profesionalId && (a.Estado == EstadoAusencia.Solicitada || a.Estado == EstadoAusencia.Aprobada)
                                             && a.Desde <= hasta && desde <= a.Hasta, ct))
            throw new ExcepcionConflicto("Ya existe una ausencia solicitada o aprobada que se solapa con esas fechas.");

        var ausencia = new Ausencia(org, profesionalId, tipo, desde, hasta, comentario, uid, reloj.GetUtcNow());
        db.Ausencias.Add(ausencia);
        db.DecisionesAusencia.Add(new DecisionAusencia(org, ausencia.Id, EstadoAusencia.Solicitada, uid, reloj.GetUtcNow(), null));
        await db.SaveChangesAsync(ct);
        return (await ComponerAsync([ausencia], ct)).Single();
    }

    /// <summary>Asignaciones activas, aún no terminadas, cuyo turno ocupa algún día de la ausencia (en la zona del centro).</summary>
    private async Task<List<TurnoComprometidoDto>> TurnosComprometidosAsync(Ausencia a, CancellationToken ct)
    {
        var datos = await evaluacion.CargarDatosAsync(ct);
        var desde = new DateTimeOffset(a.Desde.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(-2);
        var hasta = new DateTimeOffset(a.Hasta.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(3);
        var ahora = reloj.GetUtcNow();

        var ligeras = (await evaluacion.AsignacionesAsync([a.ProfesionalId], desde, hasta, datos, ct)).GetValueOrDefault(a.ProfesionalId) ?? [];
        var afectadas = ligeras.Where(l => l.Turno.FinUtc > ahora && l.Turno.Fecha <= a.Hasta && a.Desde <= l.Turno.FechaFin).ToList();
        if (afectadas.Count == 0) return [];

        var ids = afectadas.Select(l => l.AsignacionId).ToList();
        var necesidades = await db.Asignaciones.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.NecesidadId, ct);
        var turnoIds = afectadas.Select(l => l.Turno.TurnoId).ToList();
        var turnos = await (from t in db.Turnos where turnoIds.Contains(t.Id)
                            join c in db.Cuadrantes on t.CuadranteId equals c.Id
                            join s in db.Servicios on t.ServicioId equals s.Id
                            join ce in db.Centros on s.CentroId equals ce.Id
                            select new { t.Id, Estado = c.Estado, Centro = ce.Nombre }).ToDictionaryAsync(x => x.Id, ct);

        return afectadas.OrderBy(l => l.Turno.InicioUtc).Select(l => new TurnoComprometidoDto(l.AsignacionId, l.Turno.TurnoId, necesidades[l.AsignacionId],
            l.Turno.Fecha, l.Turno.Etiqueta, datos.Servicios.GetValueOrDefault(l.Turno.ServicioId, ""), turnos[l.Turno.TurnoId].Centro, l.Turno.InicioUtc,
            l.Turno.FinUtc, turnos[l.Turno.TurnoId].Estado)).ToList();
    }

    private async Task<Ausencia> BuscarAsync(Guid id, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        return await db.Ausencias.FirstOrDefaultAsync(a => a.Id == id, ct) ?? throw new ExcepcionNoEncontrado("Ausencia no encontrada.");
    }

    private async Task<Profesional> ProfesionalPropioAsync(CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var uid = UsuarioActual();
        return await db.Profesionales.FirstOrDefaultAsync(p => p.UsuarioId == uid, ct)
            ?? throw new ExcepcionNoEncontrado("Su usuario no está vinculado a ninguna ficha profesional.");
    }

    private Guid UsuarioActual() => contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");
    private DateOnly Hoy() => DateOnly.FromDateTime(reloj.GetUtcNow().UtcDateTime);

    private async Task<IReadOnlyList<AusenciaDto>> ComponerAsync(List<Ausencia> lista, CancellationToken ct)
    {
        if (lista.Count == 0) return [];
        var ids = lista.Select(a => a.Id).ToList();
        var decisiones = await db.DecisionesAusencia.Where(d => ids.Contains(d.AusenciaId)).OrderBy(d => d.Fecha).ToListAsync(ct);
        var profIds = lista.Select(a => a.ProfesionalId).Distinct().ToList();
        var profesionales = await db.Profesionales.Where(p => profIds.Contains(p.Id)).Select(p => new { p.Id, p.Nombre, p.Apellidos })
            .ToDictionaryAsync(p => p.Id, p => $"{p.Nombre} {p.Apellidos}", ct);
        var usuarioIds = decisiones.Select(d => d.UsuarioId).Concat(lista.Select(a => a.SolicitadaPorUsuarioId)).Distinct().ToList();
        var usuarios = await db.Usuarios.Where(u => usuarioIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.NombreCompleto, ct);

        return lista.Select(a => new AusenciaDto(a.Id, a.ProfesionalId, profesionales.GetValueOrDefault(a.ProfesionalId, "—"), a.Tipo, a.Desde, a.Hasta, a.Dias,
            a.Comentario, a.Estado, a.FechaSolicitud, usuarios.GetValueOrDefault(a.SolicitadaPorUsuarioId, "—"),
            decisiones.Where(d => d.AusenciaId == a.Id).Select(d => new DecisionDto(d.Decision, usuarios.GetValueOrDefault(d.UsuarioId, "—"), d.Fecha, d.Motivo)).ToList())).ToList();
    }
}
