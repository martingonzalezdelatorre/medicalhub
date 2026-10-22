using MedicalHub.Application.Comun;
using MedicalHub.Domain.Comun;
using MedicalHub.Domain.Planificacion;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Planificacion;

public sealed class TurnosServicio(IAppDbContext db, IContextoActual contexto)
{
    public static ExcepcionConflicto CuadrantePublicado()
        => new("El cuadrante no está en borrador. Reábralo para modificarlo.");

    public async Task<Guid> CrearAsync(Guid cuadranteId, SolicitudTurno s, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        var cuadrante = await CuadranteEditableAsync(cuadranteId, ct);

        if (!cuadrante.Contiene(s.Fecha))
            throw new ExcepcionValidacion(new Dictionary<string, string[]> { [nameof(s.Fecha)] = ["La fecha está fuera del periodo del cuadrante."] });

        var tipo = await db.TiposTurno.FirstOrDefaultAsync(t => t.Id == s.TipoTurnoId && t.ServicioId == cuadrante.ServicioId && t.Activo, ct)
            ?? throw new ExcepcionValidacion(new Dictionary<string, string[]> { [nameof(s.TipoTurnoId)] = ["El tipo de turno no existe, está inactivo o no pertenece al servicio."] });

        if (await db.Turnos.AnyAsync(t => t.CuadranteId == cuadranteId && t.TipoTurnoId == tipo.Id && t.Fecha == s.Fecha, ct))
            throw new ExcepcionConflicto("Ya existe un turno de ese tipo en esa fecha.");

        var lineas = s.Necesidades is { Count: > 0 } ? s.Necesidades
            : (await db.RequisitosCobertura.Where(r => r.TipoTurnoId == tipo.Id).ToListAsync(ct))
                .Select(r => new LineaNecesidad(r.NumeroProfesionales, r.CategoriaProfesionalId)).ToList();
        await ValidarLineasAsync(lineas, ct);

        var turno = NuevoTurno(org, cuadrante, tipo, s.Fecha, await ZonaDelServicioAsync(cuadrante.ServicioId, ct));
        db.Turnos.Add(turno);
        foreach (var l in lineas) db.Necesidades.Add(new NecesidadCobertura(org, turno.Id, l.NumeroPlazas, l.CategoriaProfesionalId));
        await db.SaveChangesAsync(ct);
        return turno.Id;
    }

    /// <summary>Elimina un turno (con sus necesidades y asignaciones) de un cuadrante en borrador.</summary>
    public async Task EliminarAsync(Guid turnoId, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var turno = await db.Turnos.FirstOrDefaultAsync(t => t.Id == turnoId, ct) ?? throw new ExcepcionNoEncontrado("Turno no encontrado.");
        await CuadranteEditableAsync(turno.CuadranteId, ct);

        db.Asignaciones.RemoveRange(await db.Asignaciones.Where(a => a.TurnoId == turnoId).ToListAsync(ct));
        db.Necesidades.RemoveRange(await db.Necesidades.Where(n => n.TurnoId == turnoId).ToListAsync(ct));
        db.Turnos.Remove(turno);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Sustituye las necesidades del turno. No se puede reducir una necesidad por debajo de sus asignaciones ni quitarla si tiene profesionales.</summary>
    public async Task ActualizarNecesidadesAsync(Guid turnoId, SolicitudNecesidades s, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        var turno = await db.Turnos.FirstOrDefaultAsync(t => t.Id == turnoId, ct) ?? throw new ExcepcionNoEncontrado("Turno no encontrado.");
        await CuadranteEditableAsync(turno.CuadranteId, ct);
        await ValidarLineasAsync(s.Necesidades ?? [], ct);

        var existentes = await db.Necesidades.Where(n => n.TurnoId == turnoId).ToListAsync(ct);
        var activas = await db.Asignaciones.Where(a => a.TurnoId == turnoId && a.Estado != EstadoAsignacion.Cancelada)
            .GroupBy(a => a.NecesidadId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, ct);

        foreach (var n in existentes)
        {
            var linea = s.Necesidades!.FirstOrDefault(l => l.CategoriaProfesionalId == n.CategoriaProfesionalId);
            var ocupadas = activas.GetValueOrDefault(n.Id);
            if (linea is null)
            {
                if (ocupadas > 0) throw new ExcepcionConflicto("No se puede quitar una necesidad que ya tiene profesionales asignados.");
                db.Necesidades.Remove(n);
            }
            else
            {
                if (linea.NumeroPlazas < ocupadas) throw new ExcepcionConflicto($"Hay {ocupadas} profesionales asignados: no se pueden reducir las plazas por debajo.");
                n.CambiarPlazas(linea.NumeroPlazas);
            }
        }
        foreach (var l in s.Necesidades!.Where(l => existentes.All(n => n.CategoriaProfesionalId != l.CategoriaProfesionalId)))
            db.Necesidades.Add(new NecesidadCobertura(org, turnoId, l.NumeroPlazas, l.CategoriaProfesionalId));
        await db.SaveChangesAsync(ct);
    }

    // ── Compartidos con CuadrantesServicio ──

    public static Turno NuevoTurno(Guid org, Cuadrante cuadrante, Domain.Estructura.TipoTurno tipo, DateOnly fecha, string zona)
    {
        var (inicio, fin) = CalculadoraHorario.Resolver(zona, fecha, tipo.HoraInicio, tipo.HoraFin);
        return new Turno(org, cuadrante.Id, cuadrante.ServicioId, tipo.Id, fecha, inicio, fin);
    }

    public async Task<string> ZonaDelServicioAsync(Guid servicioId, CancellationToken ct)
    {
        var centroId = await db.Servicios.Where(s => s.Id == servicioId).Select(s => s.CentroId).FirstAsync(ct);
        return await db.Centros.Where(c => c.Id == centroId).Select(c => c.ZonaHoraria).FirstAsync(ct);
    }

    public async Task<Cuadrante> CuadranteEditableAsync(Guid cuadranteId, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var visibles = AlcancePlanificacion.Servicios(db, contexto).Select(s => s.Id);
        var cuadrante = await db.Cuadrantes.FirstOrDefaultAsync(c => c.Id == cuadranteId && visibles.Contains(c.ServicioId), ct)
            ?? throw new ExcepcionNoEncontrado("Cuadrante no encontrado.");
        if (!cuadrante.EsEditable) throw CuadrantePublicado();
        return cuadrante;
    }

    private async Task ValidarLineasAsync(IReadOnlyList<LineaNecesidad> lineas, CancellationToken ct)
    {
        var v = new Validador();
        v.Cuando(lineas.GroupBy(l => l.CategoriaProfesionalId).Any(g => g.Count() > 1), "Necesidades", "Hay necesidades duplicadas para la misma categoría.");
        v.Cuando(lineas.Any(l => l.NumeroPlazas is < 1 or > 50), "Necesidades", "El número de plazas debe estar entre 1 y 50.");
        await db.CategoriasProfesionales.ExigirExistentesAsync(lineas.Where(l => l.CategoriaProfesionalId != null).Select(l => l.CategoriaProfesionalId!.Value), "Necesidades", v, ct);
        v.LanzarSiHayErrores();
    }
}
