using MedicalHub.Application.Comun;
using MedicalHub.Domain.Ausencias;
using MedicalHub.Domain.Planificacion;
using MedicalHub.Domain.Reglas;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Reglas;

/// <summary>Ausencias y disponibilidades aprobadas de un profesional en el entorno de un turno.</summary>
public sealed record DatosPersona(IReadOnlyList<AusenciaLigera> Ausencias, IReadOnlyList<DisponibilidadLigera> Disponibilidades)
{
    public static readonly DatosPersona Vacio = new([], []);
}

/// <summary>Carga los datos que necesita el motor de reglas (reglas de la sociedad y asignaciones del entorno) y lo invoca.</summary>
public sealed class EvaluacionServicio(IAppDbContext db)
{
    public sealed class Datos
    {
        public required List<ReglaPlanificacion> Reglas { get; init; }
        public required Dictionary<Guid, string> Competencias { get; init; }
        public required Dictionary<Guid, string> Servicios { get; init; }
        public required int HorizonteDias { get; init; }
        public TimeSpan Horizonte => TimeSpan.FromDays(HorizonteDias);
    }

    public async Task<Datos> CargarDatosAsync(CancellationToken ct)
    {
        var reglas = await db.Reglas.ToListAsync(ct);
        return new Datos
        {
            Reglas = reglas,
            Competencias = await db.Competencias.ToDictionaryAsync(c => c.Id, c => c.Nombre, ct),
            Servicios = await db.Servicios.ToDictionaryAsync(s => s.Id, s => s.Nombre, ct),
            HorizonteDias = MotorReglas.HorizonteDias(reglas)
        };
    }

    public async Task<TurnoLigero> LigeroAsync(Turno turno, Datos datos, CancellationToken ct)
    {
        var tipo = await db.TiposTurno.Where(t => t.Id == turno.TipoTurnoId).Select(t => new { t.Nombre, t.EsGuardia }).FirstAsync(ct);
        var zona = await ZonaDeServicioAsync(turno.ServicioId, ct);
        return new TurnoLigero(turno.Id, turno.ServicioId, turno.Fecha, turno.InicioUtc, turno.FinUtc, tipo.EsGuardia,
            Etiqueta(tipo.Nombre, turno.Fecha, datos.Servicios.GetValueOrDefault(turno.ServicioId, "")),
            FechaFinLocal(zona, turno.Fecha, turno.FinUtc), turno.TipoTurnoId);
    }

    public async Task<string> ZonaDeServicioAsync(Guid servicioId, CancellationToken ct)
    {
        var centroId = await db.Servicios.Where(s => s.Id == servicioId).Select(s => s.CentroId).FirstAsync(ct);
        return await db.Centros.Where(c => c.Id == centroId).Select(c => c.ZonaHoraria).FirstAsync(ct);
    }

    public static string Etiqueta(string tipo, DateOnly fecha, string servicio) => $"{tipo} del {fecha:dd/MM/yyyy} ({servicio})";

    /// <summary>Último día local que ocupa un turno (un fin exactamente a medianoche no ocupa el día siguiente).</summary>
    public static DateOnly FechaFinLocal(string zona, DateOnly fechaInicio, DateTimeOffset finUtc)
    {
        var local = CalculadoraHorario.ALocal(zona, finUtc);
        var dia = DateOnly.FromDateTime(local);
        if (local.TimeOfDay == TimeSpan.Zero) dia = dia.AddDays(-1);
        return dia < fechaInicio ? fechaInicio : dia;
    }

    /// <summary>Asignaciones activas de los profesionales indicados que se solapan con el intervalo dado.</summary>
    public async Task<Dictionary<Guid, List<AsignacionLigera>>> AsignacionesAsync(IEnumerable<Guid> profesionalIds, DateTimeOffset desde, DateTimeOffset hasta,
        Datos datos, CancellationToken ct)
    {
        var ids = profesionalIds.Distinct().ToList();
        var filas = await (
            from a in db.Asignaciones
            where ids.Contains(a.ProfesionalId) && a.Estado != EstadoAsignacion.Cancelada && a.FinUtc > desde && a.InicioUtc < hasta
            join t in db.Turnos on a.TurnoId equals t.Id
            join tt in db.TiposTurno on t.TipoTurnoId equals tt.Id
            join s in db.Servicios on t.ServicioId equals s.Id
            join c in db.Centros on s.CentroId equals c.Id
            select new { a.Id, a.ProfesionalId, a.Estado, a.FechaAsignacion, TurnoId = t.Id, t.ServicioId, t.Fecha, t.InicioUtc, t.FinUtc, TipoId = tt.Id,
                         tt.EsGuardia, Tipo = tt.Nombre, c.ZonaHoraria }
        ).ToListAsync(ct);

        return filas.GroupBy(f => f.ProfesionalId).ToDictionary(g => g.Key, g => g.Select(f => new AsignacionLigera(f.Id, f.Estado,
            new TurnoLigero(f.TurnoId, f.ServicioId, f.Fecha, f.InicioUtc, f.FinUtc, f.EsGuardia,
                Etiqueta(f.Tipo, f.Fecha, datos.Servicios.GetValueOrDefault(f.ServicioId, "")),
                FechaFinLocal(f.ZonaHoraria, f.Fecha, f.FinUtc), f.TipoId), f.FechaAsignacion)).ToList());
    }

    /// <summary>Ausencias y disponibilidades APROBADAS (las pendientes no cuentan) de los profesionales en un intervalo.</summary>
    public async Task<Dictionary<Guid, DatosPersona>> PersonasAsync(IEnumerable<Guid> profesionalIds, DateTimeOffset desde, DateTimeOffset hasta, CancellationToken ct)
    {
        var ids = profesionalIds.Distinct().ToList();
        var dia1 = DateOnly.FromDateTime(desde.UtcDateTime).AddDays(-2);
        var dia2 = DateOnly.FromDateTime(hasta.UtcDateTime).AddDays(2);

        var ausencias = await db.Ausencias
            .Where(a => ids.Contains(a.ProfesionalId) && a.Estado == EstadoAusencia.Aprobada && a.Desde <= dia2 && a.Hasta >= dia1).ToListAsync(ct);
        var disponibilidades = await db.Disponibilidades
            .Where(d => ids.Contains(d.ProfesionalId) && d.Estado == EstadoDisponibilidad.Aprobada && d.Desde <= dia2 && d.Hasta >= dia1).ToListAsync(ct);

        return ids.ToDictionary(id => id, id => new DatosPersona(
            ausencias.Where(a => a.ProfesionalId == id).Select(a => new AusenciaLigera(a.Tipo, a.Desde, a.Hasta)).ToList(),
            disponibilidades.Where(d => d.ProfesionalId == id).Select(d => new DisponibilidadLigera(d.Tipo, d.Desde, d.Hasta, d.Dias, d.TipoTurnoId, d.Comentario)).ToList()));
    }
}
