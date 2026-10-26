using MedicalHub.Application.Comun;
using MedicalHub.Domain.Planificacion;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Planificacion;

/// <summary>Consulta de turnos para las vistas de calendario (día, semana, mes, agenda) y para «mi planificación».</summary>
public sealed class CalendarioServicio(IAppDbContext db, IContextoActual contexto)
{
    public const int MaxDiasConsulta = 100;

    public async Task<IReadOnlyList<TurnoDto>> ConsultarAsync(FiltroCalendario f, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        if (f.Hasta < f.Desde || f.Hasta.DayNumber - f.Desde.DayNumber + 1 > MaxDiasConsulta)
            throw new ExcepcionValidacion(new Dictionary<string, string[]> { ["Hasta"] = [$"El rango debe estar entre 1 y {MaxDiasConsulta} días."] });

        var servicios = AlcancePlanificacion.Servicios(db, contexto);
        if (f.ServicioId is { } s) servicios = servicios.Where(x => x.Id == s);
        if (f.CentroId is { } c) servicios = servicios.Where(x => x.CentroId == c);
        var servicioIds = servicios.Select(x => x.Id);

        var turnos = db.Turnos.Where(t => t.Fecha >= f.Desde && t.Fecha <= f.Hasta && servicioIds.Contains(t.ServicioId));
        if (f.CuadranteId is { } cu) turnos = turnos.Where(t => t.CuadranteId == cu);
        if (f.ProfesionalId is { } p)
            turnos = turnos.Where(t => db.Asignaciones.Any(a => a.TurnoId == t.Id && a.ProfesionalId == p && a.Estado != EstadoAsignacion.Cancelada));
        if (f.CategoriaId is { } cat)
            turnos = turnos.Where(t => db.Necesidades.Any(n => n.TurnoId == t.Id && n.CategoriaProfesionalId == cat));

        var resultado = await ComponerAsync(turnos.OrderBy(t => t.InicioUtc), null, ct);
        return f.SoloSinCubrir ? resultado.Where(t => t.SinCubrir).ToList() : resultado;
    }

    /// <summary>Planificación del profesional conectado: solo sus turnos de cuadrantes publicados y solo su propia asignación.</summary>
    public async Task<IReadOnlyList<TurnoDto>> MiPlanificacionAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var uid = contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");
        if (hasta < desde || hasta.DayNumber - desde.DayNumber + 1 > MaxDiasConsulta)
            throw new ExcepcionValidacion(new Dictionary<string, string[]> { ["Hasta"] = [$"El rango debe estar entre 1 y {MaxDiasConsulta} días."] });

        var profesionalId = await db.Profesionales.Where(p => p.UsuarioId == uid).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct);
        if (profesionalId is null) return [];

        var turnos = db.Turnos.Where(t => t.Fecha >= desde && t.Fecha <= hasta
            && db.Cuadrantes.Any(c => c.Id == t.CuadranteId && c.Estado == EstadoCuadrante.Publicado)
            && db.Asignaciones.Any(a => a.TurnoId == t.Id && a.ProfesionalId == profesionalId && a.Estado != EstadoAsignacion.Cancelada));
        return await ComponerAsync(turnos.OrderBy(t => t.InicioUtc), profesionalId, ct);
    }

    /// <summary>Todos los turnos de un cuadrante (para instantáneas de versión y exportaciones).</summary>
    public Task<IReadOnlyList<TurnoDto>> TurnosDeCuadranteAsync(Guid cuadranteId, CancellationToken ct)
        => ComponerAsync(db.Turnos.Where(t => t.CuadranteId == cuadranteId).OrderBy(t => t.InicioUtc), null, ct);

    /// <param name="soloProfesionalId">Si se indica, de cada necesidad solo se muestra la asignación de ese profesional.</param>
    private async Task<IReadOnlyList<TurnoDto>> ComponerAsync(IQueryable<Turno> consulta, Guid? soloProfesionalId, CancellationToken ct)
    {
        var turnos = await consulta.ToListAsync(ct);
        if (turnos.Count == 0) return [];

        var turnoIds = turnos.Select(t => t.Id).ToList();
        var servicioIds = turnos.Select(t => t.ServicioId).Distinct().ToList();
        var tipoIds = turnos.Select(t => t.TipoTurnoId).Distinct().ToList();
        var cuadranteIds = turnos.Select(t => t.CuadranteId).Distinct().ToList();

        var necesidades = await db.Necesidades.Where(n => turnoIds.Contains(n.TurnoId)).ToListAsync(ct);
        var asignaciones = await db.Asignaciones.Where(a => turnoIds.Contains(a.TurnoId) && a.Estado != EstadoAsignacion.Cancelada).ToListAsync(ct);
        var profesionalIds = asignaciones.Select(a => a.ProfesionalId).Distinct().ToList();
        var profesionales = await db.Profesionales.Where(p => profesionalIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Nombre, p.Apellidos }).ToDictionaryAsync(p => p.Id, p => $"{p.Nombre} {p.Apellidos}".Trim(), ct);
        var categorias = await db.CategoriasProfesionales.ToDictionaryAsync(c => c.Id, c => c.Nombre, ct);
        var servicios = await db.Servicios.Where(s => servicioIds.Contains(s.Id)).Select(s => new { s.Id, s.Nombre, s.CentroId }).ToDictionaryAsync(s => s.Id, ct);
        var centroIds = servicios.Values.Select(s => s.CentroId).Distinct().ToList();
        var centros = await db.Centros.Where(c => centroIds.Contains(c.Id)).Select(c => new { c.Id, c.Nombre, c.ZonaHoraria }).ToDictionaryAsync(c => c.Id, ct);
        var tipos = await db.TiposTurno.Where(t => tipoIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, ct);
        var estados = await db.Cuadrantes.Where(c => cuadranteIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Estado, ct);

        return turnos.Select(t =>
        {
            var servicio = servicios[t.ServicioId];
            var centro = centros[servicio.CentroId];
            var tipo = tipos[t.TipoTurnoId];
            var necesidadesTurno = necesidades.Where(n => n.TurnoId == t.Id).OrderBy(n => n.CategoriaProfesionalId == null ? 0 : 1).ToList();

            var necesidadesDto = necesidadesTurno.Select(n => new NecesidadDto(
                n.Id, n.NumeroPlazas, n.CategoriaProfesionalId,
                n.CategoriaProfesionalId is { } c ? categorias.GetValueOrDefault(c) : null,
                asignaciones.Where(a => a.NecesidadId == n.Id && (soloProfesionalId == null || a.ProfesionalId == soloProfesionalId))
                    .OrderBy(a => a.FechaAsignacion)
                    .Select(a => new AsignacionDto(a.Id, a.ProfesionalId, profesionales.GetValueOrDefault(a.ProfesionalId, "—"), a.Estado)).ToList())).ToList();

            var cubiertas = necesidadesTurno.Sum(n => Math.Min(n.NumeroPlazas, asignaciones.Count(a => a.NecesidadId == n.Id)));
            var excedidas = necesidadesTurno.Sum(n => Math.Max(0, asignaciones.Count(a => a.NecesidadId == n.Id) - n.NumeroPlazas));
            var inicioLocal = CalculadoraHorario.ALocal(centro.ZonaHoraria, t.InicioUtc);
            var finLocal = CalculadoraHorario.ALocal(centro.ZonaHoraria, t.FinUtc);

            return new TurnoDto(t.Id, t.CuadranteId, estados.GetValueOrDefault(t.CuadranteId), t.Fecha,
                inicioLocal.ToString("HH:mm"), finLocal.ToString("HH:mm"), t.InicioUtc, t.FinUtc,
                DateOnly.FromDateTime(finLocal) > t.Fecha, t.DuracionHoras,
                servicio.CentroId, centro.Nombre, centro.ZonaHoraria, t.ServicioId, servicio.Nombre,
                t.TipoTurnoId, tipo.Nombre, tipo.EsGuardia,
                necesidadesDto, necesidadesTurno.Sum(n => n.NumeroPlazas), cubiertas, excedidas);
        }).ToList();
    }
}
