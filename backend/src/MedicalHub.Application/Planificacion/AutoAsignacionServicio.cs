using MedicalHub.Application.Comun;
using MedicalHub.Application.Reglas;
using MedicalHub.Domain.Ausencias;
using MedicalHub.Domain.Planificacion;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Planificacion;

/// <summary>
/// Asigna automáticamente a los turnos de un cuadrante en borrador a los profesionales cuya disponibilidad («Disponible») está APROBADA
/// para ese día, hospital y tipo de turno. Pasa por el motor de reglas (quien tenga una infracción bloqueante se salta) y puede superar
/// las plazas de la necesidad: el turno queda en rojo y la administración decide a quién quitar o si amplía el tope.
/// </summary>
public sealed class AutoAsignacionServicio(IAppDbContext db, AsignacionesServicio asignaciones)
{
    /// <summary>Asigna en los turnos indicados (de cuadrantes en borrador). Con <paramref name="soloProfesionalId"/> solo considera a ese profesional.</summary>
    public async Task<int> AsignarTurnosAsync(IReadOnlyCollection<Guid> turnoIds, Guid? soloProfesionalId, CancellationToken ct)
    {
        if (turnoIds.Count == 0) return 0;
        var turnos = await (from t in db.Turnos.AsNoTracking()
                            where turnoIds.Contains(t.Id)
                            join c in db.Cuadrantes on t.CuadranteId equals c.Id
                            where c.Estado == EstadoCuadrante.Borrador
                            join s in db.Servicios on t.ServicioId equals s.Id
                            select new { Turno = t, s.CentroId }).ToListAsync(ct);

        var total = 0;
        foreach (var x in turnos)
            total += await AsignarTurnoAsync(x.Turno, x.CentroId, soloProfesionalId, ct);
        return total;
    }

    /// <summary>Al aprobar la disponibilidad de un profesional: lo asigna a los turnos existentes (en borrador) del periodo que le encajan.</summary>
    public async Task<int> AsignarProfesionalAsync(Guid profesionalId, DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var ids = await (from t in db.Turnos.AsNoTracking()
                         where t.Fecha >= desde && t.Fecha <= hasta
                         join c in db.Cuadrantes on t.CuadranteId equals c.Id
                         where c.Estado == EstadoCuadrante.Borrador
                         select t.Id).ToListAsync(ct);
        return await AsignarTurnosAsync(ids, profesionalId, ct);
    }

    private async Task<int> AsignarTurnoAsync(Turno turno, Guid centroId, Guid? soloProfesionalId, CancellationToken ct)
    {
        var bit = DiasSemana.Bit(turno.Fecha.DayOfWeek);
        var declaraciones = (await db.Disponibilidades.AsNoTracking()
                .Where(d => d.Estado == EstadoDisponibilidad.Aprobada && d.Tipo == TipoDisponibilidad.Disponible
                            && d.Desde <= turno.Fecha && d.Hasta >= turno.Fecha
                            && (d.TipoTurnoId == null || d.TipoTurnoId == turno.TipoTurnoId)
                            && (d.CentroId == null || d.CentroId == centroId)
                            && (soloProfesionalId == null || d.ProfesionalId == soloProfesionalId))
                .OrderBy(d => d.FechaDecision).ToListAsync(ct))
            .Where(d => (d.Dias & bit) != 0).ToList();
        if (declaraciones.Count == 0) return 0;

        var yaAsignados = (await db.Asignaciones.Where(a => a.TurnoId == turno.Id && a.Estado != EstadoAsignacion.Cancelada).Select(a => a.ProfesionalId).ToListAsync(ct)).ToHashSet();
        var ordenados = declaraciones.Select(d => d.ProfesionalId).Distinct().Where(p => !yaAsignados.Contains(p)).ToList();
        if (ordenados.Count == 0) return 0;

        var profesionales = (await db.Profesionales.Where(p => ordenados.Contains(p.Id) && p.Activo).ToListAsync(ct))
            .Where(p => p.ServiciosAutorizados.Any(s => s.ServicioId == turno.ServicioId)).ToDictionary(p => p.Id);
        var necesidades = await db.Necesidades.AsNoTracking().Where(n => n.TurnoId == turno.Id).ToListAsync(ct);
        var ocupadas = await db.Asignaciones.Where(a => a.TurnoId == turno.Id && a.Estado != EstadoAsignacion.Cancelada)
            .GroupBy(a => a.NecesidadId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.N, ct);

        var creadas = 0;
        foreach (var id in ordenados.Where(profesionales.ContainsKey))
        {
            var p = profesionales[id];
            var admitidas = necesidades.Where(n => n.CategoriaProfesionalId == null || n.CategoriaProfesionalId == p.CategoriaId)
                .OrderBy(n => n.CategoriaProfesionalId == null ? 1 : 0).ToList();
            // Primero una plaza libre de su categoría, luego una libre «de cualquiera»; si todo está lleno, se pasa del tope
            var necesidad = admitidas.FirstOrDefault(n => ocupadas.GetValueOrDefault(n.Id) < n.NumeroPlazas) ?? admitidas.FirstOrDefault();
            if (necesidad is null) continue;
            var exceso = ocupadas.GetValueOrDefault(necesidad.Id) >= necesidad.NumeroPlazas;
            try
            {
                await asignaciones.AsignarAsync(necesidad.Id, id, null, ct, permitirExceso: exceso);
                ocupadas[necesidad.Id] = ocupadas.GetValueOrDefault(necesidad.Id) + 1;
                creadas++;
            }
            catch (Exception e) when (e is ExcepcionReglas or ExcepcionConflicto or ExcepcionNoEncontrado)
            {
                // No se puede asignar a esta persona (regla bloqueante, solapamiento…): queda para la planificación manual.
            }
        }
        return creadas;
    }
}
