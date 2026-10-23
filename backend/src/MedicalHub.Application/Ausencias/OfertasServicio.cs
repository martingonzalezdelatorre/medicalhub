using MedicalHub.Application.Comun;
using MedicalHub.Application.Notificaciones;
using MedicalHub.Application.Planificacion;
using MedicalHub.Application.Reglas;
using MedicalHub.Domain.Notificaciones;
using MedicalHub.Domain.Planificacion;
using MedicalHub.Domain.Profesionales;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Ausencias;

public sealed record OfertaDto(Guid Id, Guid VacanteId, Guid ProfesionalId, string Profesional, EstadoOferta Estado, DateTimeOffset FechaOferta,
    DateTimeOffset? Caduca, DateTimeOffset? FechaRespuesta, string? MotivoRespuesta, DateTimeOffset? FechaDecision, string? MotivoDecision, string OfrecidaPor,
    Guid NecesidadId, DateOnly Fecha, string Turno, string Servicio, string Centro, EstadoVacante EstadoVacante, string ProfesionalAusente);

/// <param name="CaducaEnHoras">Horas de plazo para responder (1–720). Null = sin caducidad.</param>
public sealed record SolicitudOfrecer(IReadOnlyList<Guid> ProfesionalIds, int? CaducaEnHoras);

public sealed record SolicitudRespuestaOferta(string? Motivo);

public sealed record SolicitudAprobarOferta(SolicitudExcepcion? Excepcion);

public sealed record SolicitudDenegarOferta(string Motivo);

/// <summary>
/// Ofertas de sustitución: el coordinador ofrece una vacante a uno o varios profesionales compatibles, el profesional acepta o rechaza
/// y el coordinador aprueba (se crea la asignación, con las mismas reglas y excepciones que cualquier otra) o deniega.
/// </summary>
public sealed class OfertasServicio(IAppDbContext db, IContextoActual contexto, TimeProvider reloj, AsignacionesServicio asignaciones, NotificacionesServicio notificaciones)
{
    public const int MaxDestinatarios = 10;
    public const int MaxHorasPlazo = 720;

    // ───────────── Gestión (coordinación) ─────────────

    public async Task<IReadOnlyList<OfertaDto>> ListarAsync(EstadoOferta? estado, Guid? vacanteId, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var q = db.Ofertas.AsQueryable();
        if (vacanteId is { } v) q = q.Where(o => o.VacanteId == v);
        // El estado «caducada» es derivado: se filtra después de calcularlo.
        var lista = await q.OrderByDescending(o => o.FechaOferta).Take(300).ToListAsync(ct);
        var dtos = await ComponerAsync(lista, ct);
        return estado is { } e ? dtos.Where(d => d.Estado == e).ToList() : dtos;
    }

    public async Task<IReadOnlyList<OfertaDto>> OfrecerAsync(Guid vacanteId, SolicitudOfrecer s, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        var uid = UsuarioActual();
        var ids = (s.ProfesionalIds ?? []).Distinct().ToList();
        if (ids.Count is 0 or > MaxDestinatarios)
            throw new ExcepcionValidacion(new Dictionary<string, string[]> { [nameof(s.ProfesionalIds)] = [$"Indique entre 1 y {MaxDestinatarios} profesionales."] });
        if (s.CaducaEnHoras is < 1 or > MaxHorasPlazo)
            throw new ExcepcionValidacion(new Dictionary<string, string[]> { [nameof(s.CaducaEnHoras)] = [$"El plazo debe estar entre 1 y {MaxHorasPlazo} horas."] });

        var creadas = await db.EnTransaccionAsync(async token =>
        {
            var previa = await db.Vacantes.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vacanteId, token) ?? throw new ExcepcionNoEncontrado("Vacante no encontrada.");
            await db.BloquearAsync($"necesidad:{previa.NecesidadId}", token);
            var vacante = await db.Vacantes.FirstAsync(v => v.Id == vacanteId, token);
            if (vacante.Estado != EstadoVacante.Abierta) throw new ExcepcionConflicto("La vacante ya está cubierta o cancelada.");

            var ahora = reloj.GetUtcNow();
            var candidatos = (await asignaciones.CandidatosAsync(vacante.NecesidadId, token)).ToDictionary(c => c.ProfesionalId);
            var profesionales = await db.Profesionales.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, token);
            var yaOfertados = (await db.Ofertas.Where(o => o.VacanteId == vacanteId && (o.Estado == EstadoOferta.Pendiente || o.Estado == EstadoOferta.Aceptada))
                .Select(o => o.ProfesionalId).ToListAsync(token)).ToHashSet();
            var info = (await InfoVacantesAsync([vacanteId], token))[vacanteId];
            var texto = $"{info.Fecha:dd/MM/yyyy} · {info.Turno} · {info.Servicio} ({info.Centro})";
            DateTimeOffset? caduca = s.CaducaEnHoras is { } h ? ahora.AddHours(h) : null;

            var nuevas = new List<OfertaSustitucion>();
            foreach (var id in ids)
            {
                var prof = profesionales.GetValueOrDefault(id) ?? throw new ExcepcionNoEncontrado("Profesional no encontrado.");
                if (!prof.Activo) throw new ExcepcionConflicto($"{prof.NombreCompleto} no está activo.");
                if (prof.UsuarioId is null) throw new ExcepcionConflicto($"{prof.NombreCompleto} no tiene un usuario vinculado y no podría responder a la oferta.");
                if (yaOfertados.Contains(id)) throw new ExcepcionConflicto($"{prof.NombreCompleto} ya tiene una oferta abierta para esta vacante.");
                // Solo se ofrece a quien podría ser asignado: sin infracciones, o solo con infracciones que admiten excepción.
                if (!candidatos.TryGetValue(id, out var c) || !(c.Elegible || c.RequiereExcepcion))
                    throw new ExcepcionConflicto($"{prof.NombreCompleto} no es un candidato válido para este turno: {string.Join(" ", c?.Motivos ?? [])}".Trim());

                var oferta = new OfertaSustitucion(org, vacanteId, id, uid, ahora, caduca);
                nuevas.Add(oferta);
                db.Ofertas.Add(oferta);
                notificaciones.Crear(org, prof.UsuarioId.Value, TipoNotificacion.OfertaRecibida, "Oferta de sustitución",
                    $"Se le ofrece cubrir el turno {texto}.{(caduca is { } cd ? $" Responda antes de {cd:dd/MM/yyyy HH:mm} (UTC)." : "")}", oferta.Id);
            }
            await db.SaveChangesAsync(token);
            return nuevas.Select(o => o.Id).ToList();
        }, ct);

        return await ComponerAsync(await db.Ofertas.Where(o => creadas.Contains(o.Id)).ToListAsync(ct), ct);
    }

    public async Task RetirarAsync(Guid id, string? motivo, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        var uid = UsuarioActual();
        await db.EnTransaccionAsync(async token =>
        {
            var oferta = await CargarBloqueandoAsync(id, null, token);
            if (!oferta.EstaAbierta) throw new ExcepcionConflicto("La oferta ya no está abierta.");
            oferta.Retirar(uid, reloj.GetUtcNow(), motivo);
            await notificaciones.CrearParaProfesionalAsync(org, oferta.ProfesionalId, TipoNotificacion.OfertaRetirada, "Oferta retirada",
                "La coordinación ha retirado una oferta de sustitución.", oferta.Id, token);
            await db.SaveChangesAsync(token);
            return true;
        }, ct);
    }

    /// <summary>Descarta una oferta que el profesional había aceptado.</summary>
    public async Task DenegarAsync(Guid id, string motivo, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        var uid = UsuarioActual();
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ExcepcionValidacion(new Dictionary<string, string[]> { [nameof(motivo)] = ["Indique el motivo."] });
        await db.EnTransaccionAsync(async token =>
        {
            var oferta = await CargarBloqueandoAsync(id, null, token);
            if (oferta.Estado != EstadoOferta.Aceptada) throw new ExcepcionConflicto("Solo se puede denegar una oferta aceptada por el profesional.");
            oferta.Denegar(uid, reloj.GetUtcNow(), motivo.Trim());
            await notificaciones.CrearParaProfesionalAsync(org, oferta.ProfesionalId, TipoNotificacion.SustitucionDenegada, "Sustitución no confirmada",
                "La coordinación no ha confirmado su aceptación de la sustitución.", oferta.Id, token);
            await db.SaveChangesAsync(token);
            return true;
        }, ct);
    }

    /// <summary>
    /// Confirma la sustitución: crea la asignación (en cuadrantes publicados también) evaluando de nuevo las reglas, porque el estado
    /// puede haber cambiado desde la aceptación. Cierra el resto de ofertas de la vacante en la misma transacción.
    /// </summary>
    public async Task<ResultadoAsignacionDto> AprobarAsync(Guid id, SolicitudExcepcion? excepcion, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var oferta = await db.Ofertas.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct) ?? throw new ExcepcionNoEncontrado("Oferta no encontrada.");
        if (oferta.Estado != EstadoOferta.Aceptada) throw new ExcepcionConflicto("Solo se puede aprobar una oferta aceptada por el profesional.");
        var vacante = await db.Vacantes.AsNoTracking().FirstAsync(v => v.Id == oferta.VacanteId, ct);
        // AsignarAsync bloquea, revalida el estado de la vacante y marca esta oferta como aprobada dentro de su transacción.
        return await asignaciones.AsignarAsync(vacante.NecesidadId, oferta.ProfesionalId, excepcion, ct, vacante.Id);
    }

    // ───────────── Del propio profesional ─────────────

    public async Task<IReadOnlyList<OfertaDto>> MisOfertasAsync(CancellationToken ct)
    {
        var p = await ProfesionalPropioAsync(ct);
        var lista = await db.Ofertas.Where(o => o.ProfesionalId == p.Id).OrderByDescending(o => o.FechaOferta).Take(100).ToListAsync(ct);
        return await ComponerAsync(lista, ct);
    }

    public async Task<OfertaDto> AceptarAsync(Guid id, string? motivo, CancellationToken ct) => await ResponderAsync(id, aceptar: true, motivo, ct);

    public async Task<OfertaDto> RechazarAsync(Guid id, string? motivo, CancellationToken ct) => await ResponderAsync(id, aceptar: false, motivo, ct);

    private async Task<OfertaDto> ResponderAsync(Guid id, bool aceptar, string? motivo, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        var p = await ProfesionalPropioAsync(ct);

        // Una oferta vencida se marca como caducada (fuera de la transacción de respuesta, que se revertiría al fallar).
        var previa = await db.Ofertas.FirstOrDefaultAsync(o => o.Id == id && o.ProfesionalId == p.Id, ct) ?? throw new ExcepcionNoEncontrado("Oferta no encontrada.");
        if (previa.HaVencido(reloj.GetUtcNow()))
        {
            previa.Caducar(reloj.GetUtcNow());
            await db.SaveChangesAsync(ct);
            throw new ExcepcionConflicto("La oferta ha caducado.");
        }

        await db.EnTransaccionAsync(async token =>
        {
            var oferta = await CargarBloqueandoAsync(id, p.Id, token);
            var vacante = await db.Vacantes.FirstAsync(v => v.Id == oferta.VacanteId, token);
            if (oferta.Estado != EstadoOferta.Pendiente) throw new ExcepcionConflicto("La oferta ya ha sido respondida o cerrada.");
            if (vacante.Estado != EstadoVacante.Abierta) throw new ExcepcionConflicto("La vacante ya está cubierta o cancelada.");

            var ahora = reloj.GetUtcNow();
            var info = (await InfoVacantesAsync([vacante.Id], token))[vacante.Id];
            var texto = $"{info.Fecha:dd/MM/yyyy} · {info.Turno} · {info.Servicio}";
            if (aceptar)
            {
                oferta.Aceptar(ahora, motivo);
                await notificaciones.CrearParaCoordinacionAsync(org, TipoNotificacion.OfertaAceptada, "Oferta aceptada",
                    $"{p.NombreCompleto} acepta cubrir el turno {texto}. Pendiente de su aprobación.", oferta.Id, token);
            }
            else
            {
                oferta.Rechazar(ahora, motivo);
                notificaciones.Crear(org, oferta.OfrecidaPorUsuarioId, TipoNotificacion.OfertaRechazada, "Oferta rechazada",
                    $"{p.NombreCompleto} rechaza cubrir el turno {texto}.", oferta.Id);
            }
            await db.SaveChangesAsync(token);
            return true;
        }, ct);

        return (await ComponerAsync([await db.Ofertas.FirstAsync(o => o.Id == id, ct)], ct)).Single();
    }

    // ───────────── Internos ─────────────

    /// <summary>Carga la oferta (de un profesional concreto si se indica) tras bloquear la necesidad de su vacante.</summary>
    private async Task<OfertaSustitucion> CargarBloqueandoAsync(Guid id, Guid? profesionalId, CancellationToken ct)
    {
        var previa = await db.Ofertas.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id && (profesionalId == null || o.ProfesionalId == profesionalId), ct)
            ?? throw new ExcepcionNoEncontrado("Oferta no encontrada.");
        var necesidad = await db.Vacantes.Where(v => v.Id == previa.VacanteId).Select(v => v.NecesidadId).FirstAsync(ct);
        await db.BloquearAsync($"necesidad:{necesidad}", ct);
        return await db.Ofertas.FirstAsync(o => o.Id == id, ct);
    }

    private sealed record InfoVacante(Guid NecesidadId, DateOnly Fecha, string Turno, string Servicio, string Centro, EstadoVacante Estado, string Ausente);

    private async Task<Dictionary<Guid, InfoVacante>> InfoVacantesAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        var filas = await (
            from v in db.Vacantes where ids.Contains(v.Id)
            join t in db.Turnos on v.TurnoId equals t.Id
            join tt in db.TiposTurno on t.TipoTurnoId equals tt.Id
            join s in db.Servicios on t.ServicioId equals s.Id
            join c in db.Centros on s.CentroId equals c.Id
            join p in db.Profesionales on v.ProfesionalAusenteId equals p.Id
            select new { v.Id, v.NecesidadId, v.Estado, t.Fecha, t.InicioUtc, t.FinUtc, Tipo = tt.Nombre, Servicio = s.Nombre, Centro = c.Nombre, c.ZonaHoraria, Ausente = p.Nombre + " " + p.Apellidos })
            .ToListAsync(ct);
        return filas.ToDictionary(f => f.Id, f => new InfoVacante(f.NecesidadId, f.Fecha,
            $"{f.Tipo} {CalculadoraHorario.ALocal(f.ZonaHoraria, f.InicioUtc):HH\\:mm}–{CalculadoraHorario.ALocal(f.ZonaHoraria, f.FinUtc):HH\\:mm}", f.Servicio, f.Centro, f.Estado, f.Ausente));
    }

    private async Task<IReadOnlyList<OfertaDto>> ComponerAsync(List<OfertaSustitucion> lista, CancellationToken ct)
    {
        if (lista.Count == 0) return [];
        var ahora = reloj.GetUtcNow();
        var info = await InfoVacantesAsync(lista.Select(o => o.VacanteId).Distinct().ToList(), ct);
        var profIds = lista.Select(o => o.ProfesionalId).Distinct().ToList();
        var profesionales = await db.Profesionales.Where(p => profIds.Contains(p.Id)).Select(p => new { p.Id, p.Nombre, p.Apellidos })
            .ToDictionaryAsync(p => p.Id, p => $"{p.Nombre} {p.Apellidos}", ct);
        var usIds = lista.Select(o => o.OfrecidaPorUsuarioId).Distinct().ToList();
        var usuarios = await db.Usuarios.Where(u => usIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.NombreCompleto, ct);

        return lista.Select(o =>
        {
            var i = info[o.VacanteId];
            return new OfertaDto(o.Id, o.VacanteId, o.ProfesionalId, profesionales.GetValueOrDefault(o.ProfesionalId, "—"), o.EstadoEfectivo(ahora), o.FechaOferta,
                o.Caduca, o.FechaRespuesta, o.MotivoRespuesta, o.FechaDecision, o.MotivoDecision, usuarios.GetValueOrDefault(o.OfrecidaPorUsuarioId, "—"),
                i.NecesidadId, i.Fecha, i.Turno, i.Servicio, i.Centro, i.Estado, i.Ausente);
        }).ToList();
    }

    private async Task<Profesional> ProfesionalPropioAsync(CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var uid = UsuarioActual();
        return await db.Profesionales.FirstOrDefaultAsync(p => p.UsuarioId == uid, ct)
            ?? throw new ExcepcionNoEncontrado("Su usuario no está vinculado a ninguna ficha profesional.");
    }

    private Guid UsuarioActual() => contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");
}
