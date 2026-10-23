using MedicalHub.Application.Comun;
using MedicalHub.Application.Planificacion;
using MedicalHub.Domain.Horas;
using MedicalHub.Domain.Planificacion;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Horas;

/// <summary>
/// Control horario: horas planificadas (del turno), registradas (las que se trabajaron de verdad) y ajustes con motivo.
/// Las horas nocturnas son las comprendidas entre las 22:00 y las 06:00 locales del centro.
/// </summary>
public sealed class HorasServicio(IAppDbContext db, IContextoActual contexto, TimeProvider reloj)
{
    public const int MaxDiasInforme = 366;
    public const int MaxLineas = 5000;

    // ───────────── Registro y ajuste (coordinación) ─────────────

    public async Task<LineaHorasDto> RegistrarAsync(Guid asignacionId, SolicitudRegistroHoras s, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        var uid = contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");
        await db.EnTransaccionAsync(async token =>
        {
            await db.BloquearAsync($"horas:{asignacionId}", token);
            var a = await db.Asignaciones.FirstOrDefaultAsync(x => x.Id == asignacionId, token) ?? throw new ExcepcionNoEncontrado("Asignación no encontrada.");
            if (!a.EstaActiva) throw new ExcepcionConflicto("La asignación está cancelada.");
            var ahora = reloj.GetUtcNow();
            if (a.InicioUtc > ahora) throw new ExcepcionConflicto("El turno aún no ha empezado: no se pueden registrar horas trabajadas.");
            if (s.InicioUtc > ahora) throw new ExcepcionConflicto("El inicio registrado no puede estar en el futuro.");

            var registro = await db.RegistrosHoras.FirstOrDefaultAsync(r => r.AsignacionId == asignacionId, token);
            if (registro is null) db.RegistrosHoras.Add(new RegistroHoras(org, a.Id, a.ProfesionalId, s.InicioUtc, s.FinUtc, uid, ahora));
            else registro.Actualizar(s.InicioUtc, s.FinUtc, uid, ahora);
            await db.SaveChangesAsync(token);
            return true;
        }, ct);
        return (await LineasAsync(new FiltroHoras(DateOnly.MinValue, DateOnly.MaxValue), asignacionId, ct)).Single();
    }

    public async Task<LineaHorasDto> AjustarAsync(Guid asignacionId, SolicitudAjusteHoras s, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        var uid = contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");
        if (!await db.Asignaciones.AnyAsync(x => x.Id == asignacionId, ct)) throw new ExcepcionNoEncontrado("Asignación no encontrada.");
        var registro = await db.RegistrosHoras.AsNoTracking().FirstOrDefaultAsync(r => r.AsignacionId == asignacionId, ct)
            ?? throw new ExcepcionConflicto("Primero hay que registrar las horas trabajadas de esta asignación.");
        db.AjustesHoras.Add(new AjusteHoras(org, registro.Id, s.Minutos, s.Motivo, uid, reloj.GetUtcNow()));
        await db.SaveChangesAsync(ct);
        return (await LineasAsync(new FiltroHoras(DateOnly.MinValue, DateOnly.MaxValue), asignacionId, ct)).Single();
    }

    // ───────────── Consulta ─────────────

    public Task<IReadOnlyList<LineaHorasDto>> ListarAsync(FiltroHoras f, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        ValidarRango(f.Desde, f.Hasta);
        return LineasAsync(f, null, ct);
    }

    /// <summary>Horas del profesional vinculado al usuario actual.</summary>
    public async Task<IReadOnlyList<LineaHorasDto>> MisHorasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        ValidarRango(desde, hasta);
        var uid = contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");
        var id = await db.Profesionales.Where(p => p.UsuarioId == uid).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct)
            ?? throw new ExcepcionNoEncontrado("Su usuario no está vinculado a ninguna ficha profesional.");
        return await LineasAsync(new FiltroHoras(desde, hasta, ProfesionalId: id), null, ct, soloPropio: true);
    }

    public static void ValidarRango(DateOnly desde, DateOnly hasta)
    {
        if (hasta < desde || hasta.DayNumber - desde.DayNumber + 1 > MaxDiasInforme)
            throw new ExcepcionValidacion(new Dictionary<string, string[]> { ["periodo"] = [$"El periodo debe ir de 1 a {MaxDiasInforme} días y la fecha final no puede ser anterior a la inicial."] });
    }

    /// <summary>
    /// Asignaciones activas con su horario y registro. Respeta el alcance del usuario (el responsable de centro solo ve sus centros);
    /// con <paramref name="soloPropio"/> se omite porque el filtro ya es el profesional del propio usuario.
    /// </summary>
    internal async Task<IReadOnlyList<LineaHorasDto>> LineasAsync(FiltroHoras f, Guid? asignacionId, CancellationToken ct, bool soloPropio = false)
    {
        var servicios = soloPropio ? db.Servicios : AlcancePlanificacion.Servicios(db, contexto);
        var q = from a in db.Asignaciones
                where a.Estado != EstadoAsignacion.Cancelada && (asignacionId == null || a.Id == asignacionId)
                join t in db.Turnos on a.TurnoId equals t.Id
                where t.Fecha >= f.Desde && t.Fecha <= f.Hasta
                join s in servicios on t.ServicioId equals s.Id
                join c in db.Centros on s.CentroId equals c.Id
                join tt in db.TiposTurno on t.TipoTurnoId equals tt.Id
                join p in db.Profesionales on a.ProfesionalId equals p.Id
                select new { a.Id, a.ProfesionalId, Prof = p.Nombre + " " + p.Apellidos, t.Fecha, a.InicioUtc, a.FinUtc, Tipo = tt.Nombre, tt.EsGuardia,
                             Servicio = s.Nombre, ServicioId = s.Id, Centro = c.Nombre, CentroId = c.Id, c.ZonaHoraria };
        if (f.ProfesionalId is { } pid) q = q.Where(x => x.ProfesionalId == pid);
        if (f.ServicioId is { } sid) q = q.Where(x => x.ServicioId == sid);
        if (f.CentroId is { } cid) q = q.Where(x => x.CentroId == cid);

        var filas = await q.OrderBy(x => x.InicioUtc).ThenBy(x => x.Prof).Take(MaxLineas).ToListAsync(ct);
        if (filas.Count == 0) return [];

        var ids = filas.Select(x => x.Id).ToList();
        var registros = await db.RegistrosHoras.Where(r => ids.Contains(r.AsignacionId)).ToDictionaryAsync(r => r.AsignacionId, ct);
        var regIds = registros.Values.Select(r => r.Id).ToList();
        var ajustes = regIds.Count == 0 ? [] : await db.AjustesHoras.Where(x => regIds.Contains(x.RegistroHorasId)).OrderBy(x => x.Fecha).ToListAsync(ct);
        var usIds = ajustes.Select(x => x.UsuarioId).Distinct().ToList();
        var usuarios = usIds.Count == 0 ? [] : await db.Usuarios.Where(u => usIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.NombreCompleto, ct);

        return filas.Select(x =>
        {
            var planificadas = CalculadoraHoras.MinutosTotales(x.InicioUtc, x.FinUtc);
            var nocPlan = CalculadoraHoras.MinutosNocturnos(x.ZonaHoraria, x.InicioUtc, x.FinUtc);
            registros.TryGetValue(x.Id, out var r);
            var suyos = r is null ? [] : ajustes.Where(j => j.RegistroHorasId == r.Id).ToList();
            var minAjustes = suyos.Sum(j => j.Minutos);
            return new LineaHorasDto(x.Id, x.ProfesionalId, x.Prof, x.Fecha,
                $"{x.Tipo} {CalculadoraHorario.ALocal(x.ZonaHoraria, x.InicioUtc):HH\\:mm}–{CalculadoraHorario.ALocal(x.ZonaHoraria, x.FinUtc):HH\\:mm}",
                x.Servicio, x.Centro, x.EsGuardia, x.InicioUtc, x.FinUtc, planificadas, nocPlan,
                r?.Id, r?.InicioRealUtc, r?.FinRealUtc, r?.Minutos,
                r is null ? null : CalculadoraHoras.MinutosNocturnos(x.ZonaHoraria, r.InicioRealUtc, r.FinRealUtc),
                minAjustes, r is null ? null : r.Minutos + minAjustes,
                suyos.Select(j => new AjusteHorasDto(j.Minutos, j.Motivo, usuarios.GetValueOrDefault(j.UsuarioId, "—"), j.Fecha)).ToList());
        }).ToList();
    }
}
