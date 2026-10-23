using MedicalHub.Application.Comun;
using MedicalHub.Domain.Planificacion;
using MedicalHub.Domain.Reglas;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Reglas;

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
        return new TurnoLigero(turno.Id, turno.ServicioId, turno.Fecha, turno.InicioUtc, turno.FinUtc, tipo.EsGuardia,
            Etiqueta(tipo.Nombre, turno.Fecha, datos.Servicios.GetValueOrDefault(turno.ServicioId, "")));
    }

    public static string Etiqueta(string tipo, DateOnly fecha, string servicio) => $"{tipo} del {fecha:dd/MM/yyyy} ({servicio})";

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
            select new { a.Id, a.ProfesionalId, a.Estado, a.FechaAsignacion, TurnoId = t.Id, t.ServicioId, t.Fecha, t.InicioUtc, t.FinUtc, tt.EsGuardia, Tipo = tt.Nombre }
        ).ToListAsync(ct);

        return filas.GroupBy(f => f.ProfesionalId).ToDictionary(g => g.Key, g => g.Select(f => new AsignacionLigera(f.Id, f.Estado,
            new TurnoLigero(f.TurnoId, f.ServicioId, f.Fecha, f.InicioUtc, f.FinUtc, f.EsGuardia,
                Etiqueta(f.Tipo, f.Fecha, datos.Servicios.GetValueOrDefault(f.ServicioId, ""))), f.FechaAsignacion)).ToList());
    }
}
