using MedicalHub.Application.Comun;
using MedicalHub.Application.Planificacion;
using MedicalHub.Domain.Comun;
using MedicalHub.Domain.Estructura;
using MedicalHub.Domain.Horas;
using MedicalHub.Domain.Planificacion;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Horas;

/// <summary>
/// Fichaje del propio profesional: entrada, pausas y salida con ubicación. La jornada se asocia a su asignación activa más cercana
/// (con una tolerancia de entrada anticipada) y, al cerrarse, se vuelca a <see cref="RegistroHoras"/>; las pausas se descuentan
/// con un ajuste negativo para que informes y «mis horas» no cambien.
/// </summary>
public sealed class FichajesServicio(IAppDbContext db, IContextoActual contexto, TimeProvider reloj)
{
    public static readonly TimeSpan ToleranciaEntrada = TimeSpan.FromHours(2);
    public const string MotivoPausas = "Pausas registradas en el fichaje";

    // ───────────── Fichar ─────────────

    public async Task<FichajeDto> EntradaAsync(SolicitudFichaje s, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        var ubicacion = Ubicar(s);
        var profesionalId = await ProfesionalIdAsync(ct);
        var id = await db.EnTransaccionAsync(async token =>
        {
            await db.BloquearAsync($"fichaje:{profesionalId}", token);
            var ahora = reloj.GetUtcNow();
            await DescartarCaducadosAsync(profesionalId, ahora, token);
            if (await db.Fichajes.AnyAsync(f => f.ProfesionalId == profesionalId && (f.Estado == EstadoFichaje.EnCurso || f.Estado == EstadoFichaje.EnPausa), token))
                throw new ExcepcionConflicto("Ya tiene una jornada abierta. Ciérrela antes de volver a fichar la entrada.");

            var candidata = await BuscarFichableAsync(profesionalId, ahora, token)
                ?? throw new ExcepcionConflicto("No tiene ningún turno asignado en este momento sobre el que fichar.");
            var dentro = CalculadoraGeovalla.EstaDentro(candidata.Latitud, candidata.Longitud, candidata.RadioMetros, ubicacion);
            var fichaje = new Fichaje(org, candidata.AsignacionId, profesionalId, ahora, ubicacion, dentro is null ? null : !dentro);
            db.Fichajes.Add(fichaje);
            await db.SaveChangesAsync(token);
            return fichaje.Id;
        }, ct);
        return await ObtenerAsync(id, ct);
    }

    public Task<FichajeDto> PausaAsync(CancellationToken ct) => CambiarAsync((f, ahora) => f.Pausar(ahora), ct);

    public Task<FichajeDto> ReanudarAsync(CancellationToken ct) => CambiarAsync((f, ahora) => f.Reanudar(ahora), ct);

    private async Task<FichajeDto> CambiarAsync(Action<Fichaje, DateTimeOffset> accion, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var profesionalId = await ProfesionalIdAsync(ct);
        var id = await db.EnTransaccionAsync(async token =>
        {
            await db.BloquearAsync($"fichaje:{profesionalId}", token);
            var f = await AbiertoAsync(profesionalId, token);
            accion(f, reloj.GetUtcNow());
            await db.SaveChangesAsync(token);
            return f.Id;
        }, ct);
        return await ObtenerAsync(id, ct);
    }

    public async Task<FichajeDto> SalidaAsync(SolicitudFichaje s, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        var uid = contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");
        var ubicacion = Ubicar(s);
        var profesionalId = await ProfesionalIdAsync(ct);
        var id = await db.EnTransaccionAsync(async token =>
        {
            await db.BloquearAsync($"fichaje:{profesionalId}", token);
            var ahora = reloj.GetUtcNow();
            var f = await AbiertoAsync(profesionalId, token);
            if (f.Caducado(ahora))
            {
                f.Descartar(ahora);
                await db.SaveChangesAsync(token);
                throw new ExcepcionConflicto("La jornada llevaba abierta más de 24 horas y se ha descartado. Solicite a la coordinación el registro de sus horas.");
            }

            var centro = await CentroDeAsync(f.AsignacionId, token);
            var dentro = CalculadoraGeovalla.EstaDentro(centro?.Latitud, centro?.Longitud, centro?.RadioMetros, ubicacion);
            f.Cerrar(ahora, ubicacion, dentro is null ? null : !dentro);

            var registro = await db.RegistrosHoras.FirstOrDefaultAsync(r => r.AsignacionId == f.AsignacionId, token);
            if (registro is null)
            {
                registro = new RegistroHoras(org, f.AsignacionId, f.ProfesionalId, f.InicioUtc, ahora, uid, ahora);
                db.RegistrosHoras.Add(registro);
            }
            else registro.Actualizar(f.InicioUtc, ahora, uid, ahora);

            var pausa = f.MinutosPausa(ahora);
            while (pausa > 0)
            {
                var trozo = Math.Min(pausa, AjusteHoras.MaxMinutos);
                db.AjustesHoras.Add(new AjusteHoras(org, registro.Id, -trozo, MotivoPausas, uid, ahora));
                pausa -= trozo;
            }
            await db.SaveChangesAsync(token);
            return f.Id;
        }, ct);
        return await ObtenerAsync(id, ct);
    }

    // ───────────── Consulta ─────────────

    public async Task<EstadoFichajeDto> EstadoAsync(CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var profesionalId = await ProfesionalIdAsync(ct);
        var ahora = reloj.GetUtcNow();
        var abierto = await db.Fichajes.AsNoTracking()
            .Where(f => f.ProfesionalId == profesionalId && (f.Estado == EstadoFichaje.EnCurso || f.Estado == EstadoFichaje.EnPausa))
            .OrderByDescending(f => f.InicioUtc).FirstOrDefaultAsync(ct);
        if (abierto is not null && !abierto.Caducado(ahora)) return new EstadoFichajeDto((await ADtosAsync([abierto], ahora, ct))[0], null);

        var c = await BuscarFichableAsync(profesionalId, ahora, ct);
        return new EstadoFichajeDto(null, c is null ? null : new TurnoFichableDto(c.AsignacionId, c.Turno, c.Centro, c.Latitud is not null, c.InicioUtc, c.FinUtc));
    }

    public async Task<IReadOnlyList<FichajeDto>> MisFichajesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        HorasServicio.ValidarRango(desde, hasta);
        var profesionalId = await ProfesionalIdAsync(ct);
        var d = new DateTimeOffset(desde.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var h = new DateTimeOffset(hasta.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var filas = await db.Fichajes.AsNoTracking()
            .Where(f => f.ProfesionalId == profesionalId && f.Estado != EstadoFichaje.Descartado && f.InicioUtc >= d && f.InicioUtc < h)
            .OrderByDescending(f => f.InicioUtc).Take(HorasServicio.MaxLineas).ToListAsync(ct);
        return await ADtosAsync(filas, reloj.GetUtcNow(), ct);
    }

    /// <summary>Fichajes con entrada o salida fuera de la geovalla, dentro del alcance del usuario.</summary>
    public async Task<IReadOnlyList<FichajeFueraDeZonaDto>> FueraDeZonaAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        HorasServicio.ValidarRango(desde, hasta);
        var d = new DateTimeOffset(desde.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var h = new DateTimeOffset(hasta.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var servicios = AlcancePlanificacion.Servicios(db, contexto);
        return await (from f in db.Fichajes.AsNoTracking()
                      where f.InicioUtc >= d && f.InicioUtc < h && (f.EntradaFueraDeZona == true || f.SalidaFueraDeZona == true)
                      join a in db.Asignaciones on f.AsignacionId equals a.Id
                      join t in db.Turnos on a.TurnoId equals t.Id
                      join s in servicios on t.ServicioId equals s.Id
                      join c in db.Centros on s.CentroId equals c.Id
                      join p in db.Profesionales on f.ProfesionalId equals p.Id
                      orderby f.InicioUtc descending
                      select new FichajeFueraDeZonaDto(f.Id, p.Id, p.Nombre + " " + p.Apellidos, s.Nombre, c.Nombre, f.InicioUtc, f.FinUtc,
                          f.EntradaFueraDeZona, f.SalidaFueraDeZona)).Take(HorasServicio.MaxLineas).ToListAsync(ct);
    }

    // ───────────── Internos ─────────────

    private sealed record Candidata(Guid AsignacionId, string Turno, string Centro, DateTimeOffset InicioUtc, DateTimeOffset FinUtc, double? Latitud, double? Longitud, int? RadioMetros);

    private async Task<Candidata?> BuscarFichableAsync(Guid profesionalId, DateTimeOffset ahora, CancellationToken ct)
    {
        var desde = ahora + ToleranciaEntrada;
        var fila = await (from a in db.Asignaciones.AsNoTracking()
                          where a.ProfesionalId == profesionalId && a.Estado != EstadoAsignacion.Cancelada && a.InicioUtc <= desde && a.FinUtc > ahora
                          where !db.RegistrosHoras.Any(r => r.AsignacionId == a.Id) && !db.Fichajes.Any(f => f.AsignacionId == a.Id && f.Estado == EstadoFichaje.Cerrado)
                          join t in db.Turnos on a.TurnoId equals t.Id
                          join s in db.Servicios on t.ServicioId equals s.Id
                          join c in db.Centros on s.CentroId equals c.Id
                          join tt in db.TiposTurno on t.TipoTurnoId equals tt.Id
                          orderby a.InicioUtc descending
                          select new { a.Id, a.InicioUtc, a.FinUtc, Tipo = tt.Nombre, Servicio = s.Nombre, Centro = c.Nombre, c.ZonaHoraria, c.Latitud, c.Longitud, c.RadioMetros })
            .FirstOrDefaultAsync(ct);
        if (fila is null) return null;
        var texto = $"{fila.Tipo} {CalculadoraHorario.ALocal(fila.ZonaHoraria, fila.InicioUtc):HH\\:mm}–{CalculadoraHorario.ALocal(fila.ZonaHoraria, fila.FinUtc):HH\\:mm} · {fila.Servicio}";
        return new Candidata(fila.Id, texto, fila.Centro, fila.InicioUtc, fila.FinUtc, fila.Latitud, fila.Longitud, fila.RadioMetros);
    }

    private async Task<Centro?> CentroDeAsync(Guid asignacionId, CancellationToken ct)
        => await (from a in db.Asignaciones
                  where a.Id == asignacionId
                  join t in db.Turnos on a.TurnoId equals t.Id
                  join s in db.Servicios on t.ServicioId equals s.Id
                  join c in db.Centros on s.CentroId equals c.Id
                  select c).AsNoTracking().FirstOrDefaultAsync(ct);

    private async Task DescartarCaducadosAsync(Guid profesionalId, DateTimeOffset ahora, CancellationToken ct)
    {
        var limite = ahora.AddHours(-RegistroHoras.MaxHoras);
        var caducados = await db.Fichajes
            .Where(f => f.ProfesionalId == profesionalId && (f.Estado == EstadoFichaje.EnCurso || f.Estado == EstadoFichaje.EnPausa) && f.InicioUtc < limite)
            .ToListAsync(ct);
        foreach (var f in caducados) f.Descartar(ahora);
        if (caducados.Count > 0) await db.SaveChangesAsync(ct);
    }

    private async Task<Fichaje> AbiertoAsync(Guid profesionalId, CancellationToken ct)
        => await db.Fichajes.Include(f => f.Pausas)
               .Where(f => f.ProfesionalId == profesionalId && (f.Estado == EstadoFichaje.EnCurso || f.Estado == EstadoFichaje.EnPausa))
               .OrderByDescending(f => f.InicioUtc).FirstOrDefaultAsync(ct)
           ?? throw new ExcepcionConflicto("No tiene ninguna jornada abierta.");

    private async Task<Guid> ProfesionalIdAsync(CancellationToken ct)
    {
        var uid = contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");
        return await db.Profesionales.Where(p => p.UsuarioId == uid).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct)
            ?? throw new ExcepcionNoEncontrado("Su usuario no está vinculado a ninguna ficha profesional.");
    }

    private static Ubicacion? Ubicar(SolicitudFichaje s)
    {
        if (s.Latitud is null && s.Longitud is null) return null;
        if (s.Latitud is null || s.Longitud is null)
            throw new ExcepcionValidacion(new Dictionary<string, string[]> { ["ubicacion"] = ["Hay que indicar latitud y longitud, o ninguna de las dos."] });
        try { return new Ubicacion(s.Latitud.Value, s.Longitud.Value, s.PrecisionMetros); }
        catch (ExcepcionDominio e) { throw new ExcepcionValidacion(new Dictionary<string, string[]> { ["ubicacion"] = [e.Message] }); }
    }

    private async Task<FichajeDto> ObtenerAsync(Guid id, CancellationToken ct)
    {
        var f = await db.Fichajes.AsNoTracking().Include(x => x.Pausas).FirstAsync(x => x.Id == id, ct);
        return (await ADtosAsync([f], reloj.GetUtcNow(), ct))[0];
    }

    private async Task<IReadOnlyList<FichajeDto>> ADtosAsync(IReadOnlyList<Fichaje> fichajes, DateTimeOffset ahora, CancellationToken ct)
    {
        if (fichajes.Count == 0) return [];
        var ids = fichajes.Select(f => f.Id).ToList();
        var conPausas = await db.Fichajes.AsNoTracking().Include(f => f.Pausas).Where(f => ids.Contains(f.Id)).ToDictionaryAsync(f => f.Id, ct);
        var asigIds = fichajes.Select(f => f.AsignacionId).Distinct().ToList();
        var info = await (from a in db.Asignaciones.AsNoTracking()
                          where asigIds.Contains(a.Id)
                          join t in db.Turnos on a.TurnoId equals t.Id
                          join s in db.Servicios on t.ServicioId equals s.Id
                          join c in db.Centros on s.CentroId equals c.Id
                          join tt in db.TiposTurno on t.TipoTurnoId equals tt.Id
                          select new { a.Id, a.InicioUtc, a.FinUtc, Tipo = tt.Nombre, Servicio = s.Nombre, Centro = c.Nombre, c.ZonaHoraria, Geovalla = c.Latitud != null && c.Longitud != null && c.RadioMetros != null })
            .ToDictionaryAsync(x => x.Id, ct);

        return fichajes.Select(f =>
        {
            var p = conPausas[f.Id];
            info.TryGetValue(f.AsignacionId, out var i);
            var turno = i is null ? "" : $"{i.Tipo} {CalculadoraHorario.ALocal(i.ZonaHoraria, i.InicioUtc):HH\\:mm}–{CalculadoraHorario.ALocal(i.ZonaHoraria, i.FinUtc):HH\\:mm} · {i.Servicio}";
            return new FichajeDto(f.Id, f.AsignacionId, f.Estado, turno, i?.Centro ?? "", i?.Geovalla ?? false, f.InicioUtc, f.FinUtc,
                p.MinutosPausa(ahora), p.MinutosTrabajados(ahora), f.EntradaFueraDeZona, f.SalidaFueraDeZona, f.EntradaLatitud is null,
                f.Estado == EstadoFichaje.Cerrado ? f.SalidaLatitud is null : null,
                p.Pausas.OrderBy(x => x.InicioUtc).Select(x => new PausaFichajeDto(x.InicioUtc, x.FinUtc)).ToList());
        }).ToList();
    }
}
