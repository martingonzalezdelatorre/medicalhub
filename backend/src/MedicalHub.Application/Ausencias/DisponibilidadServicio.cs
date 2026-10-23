using MedicalHub.Application.Comun;
using MedicalHub.Domain.Ausencias;
using MedicalHub.Domain.Profesionales;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Ausencias;

/// <summary>
/// Disponibilidad declarada por el profesional (disponible, bloqueo o preferencias). Solo lo APROBADO por el coordinador
/// se tiene en cuenta en las reglas; una preferencia no es una prohibición salvo que la sociedad lo configure.
/// </summary>
public sealed class DisponibilidadServicio(IAppDbContext db, IContextoActual contexto, TimeProvider reloj)
{
    // ───────────── Del propio profesional ─────────────

    public async Task<IReadOnlyList<DisponibilidadDto>> MiDisponibilidadAsync(CancellationToken ct)
    {
        var p = await ProfesionalPropioAsync(ct);
        var lista = await db.Disponibilidades.Where(d => d.ProfesionalId == p.Id).OrderByDescending(d => d.Desde).Take(300).ToListAsync(ct);
        return await ComponerAsync(lista, ct);
    }

    public async Task<DisponibilidadDto> DeclararAsync(SolicitudDisponibilidad s, CancellationToken ct)
    {
        var p = await ProfesionalPropioAsync(ct);
        return await CrearAsync(p, s.Tipo, s.Desde, s.Hasta, s.Dias, s.TipoTurnoId, s.Comentario, aprobar: false, ct);
    }

    /// <summary>El profesional retira una declaración suya que aún no está aprobada. Las aprobadas las revoca el coordinador.</summary>
    public async Task EliminarPropiaAsync(Guid id, CancellationToken ct)
    {
        var p = await ProfesionalPropioAsync(ct);
        var d = await db.Disponibilidades.FirstOrDefaultAsync(x => x.Id == id && x.ProfesionalId == p.Id, ct) ?? throw new ExcepcionNoEncontrado("Declaración no encontrada.");
        if (d.Estado == EstadoDisponibilidad.Aprobada)
            throw new ExcepcionConflicto("Esta declaración ya está aprobada: solicite a su coordinador que la revoque.");
        db.Disponibilidades.Remove(d);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Tipos de turno de los servicios donde el profesional está autorizado (para indicar preferencias por turno).</summary>
    public async Task<IReadOnlyList<TipoTurnoPropioDto>> MisTiposTurnoAsync(CancellationToken ct)
    {
        var p = await ProfesionalPropioAsync(ct);
        var servicios = p.ServiciosAutorizados.Select(s => s.ServicioId).ToList();
        var filas = await (from t in db.TiposTurno where servicios.Contains(t.ServicioId) && t.Activo
                           join s in db.Servicios on t.ServicioId equals s.Id
                           join c in db.Centros on s.CentroId equals c.Id
                           orderby c.Nombre, s.Nombre, t.HoraInicio
                           select new { t.Id, t.Nombre, t.HoraInicio, t.HoraFin, Servicio = s.Nombre, Centro = c.Nombre }).ToListAsync(ct);
        return filas.Select(f => new TipoTurnoPropioDto(f.Id, f.Nombre, $"{f.HoraInicio:HH\\:mm}–{f.HoraFin:HH\\:mm}", f.Servicio, f.Centro)).ToList();
    }

    // ───────────── Gestión (administrador y coordinador) ─────────────

    public async Task<IReadOnlyList<DisponibilidadDto>> ListarAsync(EstadoDisponibilidad? estado, Guid? profesionalId, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var q = db.Disponibilidades.AsQueryable();
        if (estado is { } e) q = q.Where(d => d.Estado == e);
        if (profesionalId is { } p) q = q.Where(d => d.ProfesionalId == p);
        var lista = await q.OrderBy(d => d.Estado == EstadoDisponibilidad.Declarada ? 0 : 1).ThenByDescending(d => d.FechaDeclaracion).Take(300).ToListAsync(ct);
        return await ComponerAsync(lista, ct);
    }

    public async Task<DisponibilidadDto> RegistrarAsync(SolicitudDisponibilidadDeProfesional s, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var p = await db.Profesionales.FirstOrDefaultAsync(x => x.Id == s.ProfesionalId, ct) ?? throw new ExcepcionNoEncontrado("Profesional no encontrado.");
        return await CrearAsync(p, s.Tipo, s.Desde, s.Hasta, s.Dias, s.TipoTurnoId, s.Comentario, s.Aprobar, ct);
    }

    public Task<DisponibilidadDto> AprobarAsync(Guid id, string? motivo, CancellationToken ct) => DecidirAsync(id, true, motivo, ct);

    public Task<DisponibilidadDto> RechazarAsync(Guid id, string? motivo, CancellationToken ct) => DecidirAsync(id, false, motivo, ct);

    /// <summary>Elimina una declaración en cualquier estado (queda en la auditoría).</summary>
    public async Task EliminarAsync(Guid id, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var d = await db.Disponibilidades.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new ExcepcionNoEncontrado("Declaración no encontrada.");
        db.Disponibilidades.Remove(d);
        await db.SaveChangesAsync(ct);
    }

    // ───────────── Internos ─────────────

    private async Task<DisponibilidadDto> DecidirAsync(Guid id, bool aprobar, string? motivo, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var uid = contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");
        var d = await db.Disponibilidades.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new ExcepcionNoEncontrado("Declaración no encontrada.");
        if (d.Estado != EstadoDisponibilidad.Declarada) throw new ExcepcionConflicto("La declaración ya fue resuelta.");
        if (aprobar) d.Aprobar(uid, reloj.GetUtcNow(), motivo); else d.Rechazar(uid, reloj.GetUtcNow(), motivo);
        await db.SaveChangesAsync(ct);
        return (await ComponerAsync([d], ct)).Single();
    }

    private async Task<DisponibilidadDto> CrearAsync(Profesional p, TipoDisponibilidad tipo, DateOnly desde, DateOnly hasta, int? dias, Guid? tipoTurnoId,
        string? comentario, bool aprobar, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        var uid = contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");
        if (tipoTurnoId is { } tt)
        {
            var autorizados = p.ServiciosAutorizados.Select(s => s.ServicioId).ToList();
            if (!await db.TiposTurno.AnyAsync(t => t.Id == tt && autorizados.Contains(t.ServicioId), ct))
                throw new ExcepcionValidacion(new Dictionary<string, string[]> { [nameof(tipoTurnoId)] = ["El tipo de turno no existe o pertenece a un servicio donde no está autorizado."] });
        }

        var ahora = reloj.GetUtcNow();
        var disponibilidad = new Disponibilidad(org, p.Id, tipo, desde, hasta, dias ?? DiasSemana.Todos, tipoTurnoId, comentario, uid, ahora);
        if (aprobar) disponibilidad.Aprobar(uid, ahora, null);
        db.Disponibilidades.Add(disponibilidad);
        await db.SaveChangesAsync(ct);
        return (await ComponerAsync([disponibilidad], ct)).Single();
    }

    private async Task<Profesional> ProfesionalPropioAsync(CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var uid = contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");
        return await db.Profesionales.FirstOrDefaultAsync(p => p.UsuarioId == uid, ct)
            ?? throw new ExcepcionNoEncontrado("Su usuario no está vinculado a ninguna ficha profesional.");
    }

    private async Task<IReadOnlyList<DisponibilidadDto>> ComponerAsync(List<Disponibilidad> lista, CancellationToken ct)
    {
        if (lista.Count == 0) return [];
        var profIds = lista.Select(d => d.ProfesionalId).Distinct().ToList();
        var profesionales = await db.Profesionales.Where(p => profIds.Contains(p.Id)).Select(p => new { p.Id, p.Nombre, p.Apellidos })
            .ToDictionaryAsync(p => p.Id, p => $"{p.Nombre} {p.Apellidos}", ct);
        var tipoIds = lista.Where(d => d.TipoTurnoId != null).Select(d => d.TipoTurnoId!.Value).Distinct().ToList();
        var tipos = await db.TiposTurno.Where(t => tipoIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Nombre, ct);

        return lista.Select(d => new DisponibilidadDto(d.Id, d.ProfesionalId, profesionales.GetValueOrDefault(d.ProfesionalId, "—"), d.Tipo, d.Desde, d.Hasta,
            d.Dias, d.TipoTurnoId, d.TipoTurnoId is { } t ? tipos.GetValueOrDefault(t) : null, d.Comentario, d.Estado, d.FechaDeclaracion, d.MotivoDecision)).ToList();
    }
}
