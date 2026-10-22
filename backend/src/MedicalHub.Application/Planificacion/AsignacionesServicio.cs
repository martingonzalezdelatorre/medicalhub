using MedicalHub.Application.Comun;
using MedicalHub.Domain.Comun;
using MedicalHub.Domain.Planificacion;
using MedicalHub.Domain.Profesionales;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Planificacion;

/// <summary>
/// Asignación de profesionales a las plazas de una necesidad de cobertura.
/// Las operaciones críticas se serializan con bloqueos por necesidad y por profesional dentro de una transacción,
/// de modo que dos peticiones simultáneas no pueden llenar una plaza dos veces ni solapar a un profesional.
/// </summary>
public sealed class AsignacionesServicio(IAppDbContext db, IContextoActual contexto, TimeProvider reloj)
{
    public async Task<AsignacionDto> AsignarAsync(Guid necesidadId, Guid profesionalId, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        var uid = contexto.UsuarioId;

        return await db.EnTransaccionAsync(async token =>
        {
            // Orden fijo de bloqueos para evitar interbloqueos entre peticiones cruzadas.
            foreach (var recurso in new[] { $"necesidad:{necesidadId}", $"profesional:{profesionalId}" }.Order(StringComparer.Ordinal))
                await db.BloquearAsync(recurso, token);

            var visibles = AlcancePlanificacion.Servicios(db, contexto).Select(s => s.Id);
            var necesidad = await db.Necesidades.FirstOrDefaultAsync(n => n.Id == necesidadId, token)
                ?? throw new ExcepcionNoEncontrado("Necesidad de cobertura no encontrada.");
            var turno = await db.Turnos.FirstOrDefaultAsync(t => t.Id == necesidad.TurnoId && visibles.Contains(t.ServicioId), token)
                ?? throw new ExcepcionNoEncontrado("Necesidad de cobertura no encontrada.");
            var cuadrante = await db.Cuadrantes.FirstAsync(c => c.Id == turno.CuadranteId, token);
            if (!cuadrante.EsEditable) throw TurnosServicio.CuadrantePublicado();

            var profesional = await db.Profesionales.FirstOrDefaultAsync(p => p.Id == profesionalId, token)
                ?? throw new ExcepcionNoEncontrado("Profesional no encontrado.");
            var servicio = await db.Servicios.FirstAsync(s => s.Id == turno.ServicioId, token);
            var competencias = await db.Competencias.ToDictionaryAsync(c => c.Id, c => c.Nombre, token);

            // 1. Elegibilidad de dominio (autorización, categoría, competencias vigentes, activos)
            var elegibilidad = EvaluadorElegibilidad.Evaluar(profesional, servicio, turno.Fecha, competencias);
            if (!elegibilidad.Elegible) throw new ExcepcionDominio("No se puede asignar: " + string.Join(" ", elegibilidad.Motivos));

            // 2. Categoría exigida por la necesidad
            if (necesidad.CategoriaProfesionalId is { } cat && profesional.CategoriaId != cat)
                throw new ExcepcionDominio("No se puede asignar: la categoría del profesional no coincide con la que exige la necesidad.");

            // 3. Ya asignado a esta necesidad / plazas libres
            var activas = db.Asignaciones.Where(a => a.NecesidadId == necesidadId && a.Estado != EstadoAsignacion.Cancelada);
            if (await activas.AnyAsync(a => a.ProfesionalId == profesionalId, token))
                throw new ExcepcionConflicto("El profesional ya está asignado a esta necesidad.");
            if (await activas.CountAsync(token) >= necesidad.NumeroPlazas)
                throw new ExcepcionConflicto("No quedan plazas libres en esta necesidad.");

            // 4. Solapamiento con cualquier otra asignación activa del profesional (cualquier servicio o centro)
            var choque = await db.Asignaciones
                .Where(a => a.ProfesionalId == profesionalId && a.Estado != EstadoAsignacion.Cancelada
                            && a.InicioUtc < turno.FinUtc && turno.InicioUtc < a.FinUtc)
                .Select(a => a.TurnoId).FirstOrDefaultAsync(token);
            if (choque != Guid.Empty)
                throw new ExcepcionDominio("No se puede asignar: el profesional ya tiene otro turno que se solapa con este horario.");

            var asignacion = new Asignacion(org, necesidadId, turno, profesionalId, uid, reloj.GetUtcNow());
            db.Asignaciones.Add(asignacion);
            await db.SaveChangesAsync(token);
            return new AsignacionDto(asignacion.Id, profesionalId, profesional.NombreCompleto, asignacion.Estado);
        }, ct);
    }

    public async Task QuitarAsync(Guid asignacionId, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        await db.EnTransaccionAsync(async token =>
        {
            var visibles = AlcancePlanificacion.Servicios(db, contexto).Select(s => s.Id);
            var asignacion = await db.Asignaciones.FirstOrDefaultAsync(a => a.Id == asignacionId, token)
                ?? throw new ExcepcionNoEncontrado("Asignación no encontrada.");
            await db.BloquearAsync($"necesidad:{asignacion.NecesidadId}", token);

            var turno = await db.Turnos.FirstOrDefaultAsync(t => t.Id == asignacion.TurnoId && visibles.Contains(t.ServicioId), token)
                ?? throw new ExcepcionNoEncontrado("Asignación no encontrada.");
            var cuadrante = await db.Cuadrantes.FirstAsync(c => c.Id == turno.CuadranteId, token);
            if (!cuadrante.EsEditable) throw TurnosServicio.CuadrantePublicado();

            asignacion.Cancelar(reloj.GetUtcNow());
            await db.SaveChangesAsync(token);
            return true;
        }, ct);
    }

    /// <summary>Profesionales activos con su elegibilidad y disponibilidad horaria para una necesidad concreta.</summary>
    public async Task<IReadOnlyList<CandidatoDto>> CandidatosAsync(Guid necesidadId, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var visibles = AlcancePlanificacion.Servicios(db, contexto).Select(s => s.Id);
        var necesidad = await db.Necesidades.FirstOrDefaultAsync(n => n.Id == necesidadId, ct)
            ?? throw new ExcepcionNoEncontrado("Necesidad de cobertura no encontrada.");
        var turno = await db.Turnos.FirstOrDefaultAsync(t => t.Id == necesidad.TurnoId && visibles.Contains(t.ServicioId), ct)
            ?? throw new ExcepcionNoEncontrado("Necesidad de cobertura no encontrada.");
        var servicio = await db.Servicios.FirstAsync(s => s.Id == turno.ServicioId, ct);
        var competencias = await db.Competencias.ToDictionaryAsync(c => c.Id, c => c.Nombre, ct);

        var profesionales = await db.Profesionales.Where(p => p.Activo).OrderBy(p => p.Apellidos).ThenBy(p => p.Nombre).Take(500).ToListAsync(ct);
        var ocupados = (await db.Asignaciones
            .Where(a => a.Estado != EstadoAsignacion.Cancelada && a.InicioUtc < turno.FinUtc && turno.InicioUtc < a.FinUtc)
            .Select(a => a.ProfesionalId).ToListAsync(ct)).ToHashSet();

        var lista = profesionales.Select(p =>
        {
            var r = EvaluadorElegibilidad.Evaluar(p, servicio, turno.Fecha, competencias);
            var motivos = r.Motivos.ToList();
            if (necesidad.CategoriaProfesionalId is { } cat && p.CategoriaId != cat)
                motivos.Add("Su categoría no es la que exige esta necesidad.");
            var libre = !ocupados.Contains(p.Id);
            if (!libre) motivos.Add("Ya tiene un turno que se solapa con este horario.");
            return new CandidatoDto(p.Id, p.NombreCompleto, motivos.Count == 0, libre, motivos);
        }).ToList();

        return lista.OrderByDescending(c => c.Elegible).ThenByDescending(c => c.Libre).ThenBy(c => c.NombreCompleto).ToList();
    }
}
