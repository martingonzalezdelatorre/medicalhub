using MedicalHub.Application.Comun;
using MedicalHub.Application.Reglas;
using MedicalHub.Domain.Comun;
using MedicalHub.Domain.Identidad;
using MedicalHub.Domain.Planificacion;
using MedicalHub.Domain.Reglas;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Planificacion;

/// <summary>
/// Asignación de profesionales a las plazas de una necesidad de cobertura, sometida al motor de reglas.
/// Las operaciones críticas se serializan con bloqueos por necesidad y por profesional dentro de una transacción,
/// de modo que dos peticiones simultáneas no pueden llenar una plaza dos veces ni solapar a un profesional.
/// </summary>
public sealed class AsignacionesServicio(IAppDbContext db, IContextoActual contexto, TimeProvider reloj, EvaluacionServicio evaluacion)
{
    public async Task<ResultadoAsignacionDto> AsignarAsync(Guid necesidadId, Guid profesionalId, SolicitudExcepcion? excepcion, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        var uid = contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");

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

            // Condiciones de estado (409): ya asignado y plazas libres
            var activas = db.Asignaciones.Where(a => a.NecesidadId == necesidadId && a.Estado != EstadoAsignacion.Cancelada);
            if (await activas.AnyAsync(a => a.ProfesionalId == profesionalId, token))
                throw new ExcepcionConflicto("El profesional ya está asignado a esta necesidad.");
            if (await activas.CountAsync(token) >= necesidad.NumeroPlazas)
                throw new ExcepcionConflicto("No quedan plazas libres en esta necesidad.");

            // Reglas de planificación
            var datos = await evaluacion.CargarDatosAsync(token);
            var ligero = await evaluacion.LigeroAsync(turno, datos, token);
            var otras = (await evaluacion.AsignacionesAsync([profesionalId], turno.InicioUtc - datos.Horizonte, turno.FinUtc + datos.Horizonte, datos, token))
                .GetValueOrDefault(profesionalId) ?? [];
            var violaciones = MotorReglas.EvaluarAsignacion(new ContextoEvaluacion
            {
                Profesional = profesional, Servicio = servicio, Necesidad = necesidad, Turno = ligero, Otras = otras,
                Competencias = datos.Competencias, Servicios = datos.Servicios
            }, MotorReglas.Efectivas(datos.Reglas, turno.ServicioId));

            var bloqueantes = violaciones.Where(v => v.Bloquea).ToList();
            var advertencias = violaciones.Where(v => !v.Bloquea).ToList();
            ExigirAutorizacion(bloqueantes, excepcion);

            var asignacion = new Asignacion(org, necesidadId, turno, profesionalId, uid, reloj.GetUtcNow());
            db.Asignaciones.Add(asignacion);
            foreach (var v in bloqueantes)
                db.Excepciones.Add(new ExcepcionRegla(org, asignacion.Id, turno.Id, profesionalId, v.Clave, v.Codigo, v.ReglaId, v.Mensaje,
                    excepcion!.Justificacion, uid, reloj.GetUtcNow()));
            await db.SaveChangesAsync(token);

            return new ResultadoAsignacionDto(asignacion.Id, profesionalId, profesional.NombreCompleto, asignacion.Estado,
                advertencias.Select(ViolacionDto.De).ToList(), bloqueantes.Select(ViolacionDto.De).ToList());
        }, ct);
    }

    /// <summary>Decide si se puede asignar con las infracciones bloqueantes dadas: sin excepción, con excepción o nunca.</summary>
    private void ExigirAutorizacion(IReadOnlyList<Violacion> bloqueantes, SolicitudExcepcion? excepcion)
    {
        if (bloqueantes.Count == 0) return;
        var dtos = bloqueantes.Select(ViolacionDto.De).ToList();
        var detalle = string.Join(" ", bloqueantes.Select(b => b.Mensaje));

        if (bloqueantes.Any(b => !b.PermiteExcepcion))
            throw new ExcepcionReglas("No se puede asignar: " + detalle, dtos, "regla_incumplida");
        if (excepcion is null)
            throw new ExcepcionReglas("No se puede asignar sin una excepción justificada: " + detalle, dtos, "requiere_excepcion");
        if (bloqueantes.Any(b => b.RequiereAdministrador) && !contexto.TieneRol(Rol.AdministradorSociedad))
            throw new ExcepcionReglas("Esta excepción solo puede autorizarla el administrador de la sociedad.",
                bloqueantes.Where(b => b.RequiereAdministrador).Select(ViolacionDto.De).ToList(), "requiere_administrador", prohibida: true);
    }

    /// <summary>
    /// Autoriza, sobre una asignación ya hecha, las infracciones bloqueantes que admiten excepción (p. ej. cuando la sociedad
    /// endurece una regla después). Queda registrada con su justificación y quién la autorizó.
    /// </summary>
    public async Task<IReadOnlyList<ViolacionDto>> AutorizarExcepcionAsync(Guid asignacionId, SolicitudExcepcion excepcion, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        var uid = contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");

        return await db.EnTransaccionAsync(async token =>
        {
            var visibles = AlcancePlanificacion.Servicios(db, contexto).Select(s => s.Id);
            var asignacion = await db.Asignaciones.FirstOrDefaultAsync(a => a.Id == asignacionId && a.Estado != EstadoAsignacion.Cancelada, token)
                ?? throw new ExcepcionNoEncontrado("Asignación no encontrada.");
            foreach (var recurso in new[] { $"necesidad:{asignacion.NecesidadId}", $"profesional:{asignacion.ProfesionalId}" }.Order(StringComparer.Ordinal))
                await db.BloquearAsync(recurso, token);

            var turno = await db.Turnos.FirstOrDefaultAsync(t => t.Id == asignacion.TurnoId && visibles.Contains(t.ServicioId), token)
                ?? throw new ExcepcionNoEncontrado("Asignación no encontrada.");
            if (!(await db.Cuadrantes.FirstAsync(c => c.Id == turno.CuadranteId, token)).EsEditable) throw TurnosServicio.CuadrantePublicado();

            var necesidad = await db.Necesidades.FirstAsync(n => n.Id == asignacion.NecesidadId, token);
            var profesional = await db.Profesionales.FirstAsync(p => p.Id == asignacion.ProfesionalId, token);
            var servicio = await db.Servicios.FirstAsync(s => s.Id == turno.ServicioId, token);
            var datos = await evaluacion.CargarDatosAsync(token);
            var ligero = await evaluacion.LigeroAsync(turno, datos, token);
            var entorno = (await evaluacion.AsignacionesAsync([profesional.Id], turno.InicioUtc - datos.Horizonte, turno.FinUtc + datos.Horizonte, datos, token))
                .GetValueOrDefault(profesional.Id) ?? [];

            var existentes = (await db.Excepciones.Where(e => e.AsignacionId == asignacionId).Select(e => e.ClaveRegla).ToListAsync(token)).ToHashSet();
            var pendientes = MotorReglas.EvaluarAsignacion(new ContextoEvaluacion
            {
                Profesional = profesional, Servicio = servicio, Necesidad = necesidad, Turno = ligero,
                Otras = entorno.Where(o => CuadrantesServicio.EsAnterior(o, asignacion)).ToList(),
                Competencias = datos.Competencias, Servicios = datos.Servicios
            }, MotorReglas.Efectivas(datos.Reglas, turno.ServicioId)).Where(v => v.Bloquea && !existentes.Contains(v.Clave)).ToList();

            if (pendientes.Count == 0) throw new ExcepcionConflicto("La asignación no tiene infracciones bloqueantes pendientes de excepción.");
            ExigirAutorizacion(pendientes, excepcion);

            foreach (var v in pendientes)
                db.Excepciones.Add(new ExcepcionRegla(org, asignacion.Id, turno.Id, profesional.Id, v.Clave, v.Codigo, v.ReglaId, v.Mensaje,
                    excepcion.Justificacion, uid, reloj.GetUtcNow()));
            await db.SaveChangesAsync(token);
            return (IReadOnlyList<ViolacionDto>)pendientes.Select(ViolacionDto.De).ToList();
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

    /// <summary>Profesionales activos con las infracciones que tendría cada uno si se le asignara esta necesidad.</summary>
    public async Task<IReadOnlyList<CandidatoDto>> CandidatosAsync(Guid necesidadId, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var visibles = AlcancePlanificacion.Servicios(db, contexto).Select(s => s.Id);
        var necesidad = await db.Necesidades.FirstOrDefaultAsync(n => n.Id == necesidadId, ct)
            ?? throw new ExcepcionNoEncontrado("Necesidad de cobertura no encontrada.");
        var turno = await db.Turnos.FirstOrDefaultAsync(t => t.Id == necesidad.TurnoId && visibles.Contains(t.ServicioId), ct)
            ?? throw new ExcepcionNoEncontrado("Necesidad de cobertura no encontrada.");
        var servicio = await db.Servicios.FirstAsync(s => s.Id == turno.ServicioId, ct);

        var datos = await evaluacion.CargarDatosAsync(ct);
        var ligero = await evaluacion.LigeroAsync(turno, datos, ct);
        var reglas = MotorReglas.Efectivas(datos.Reglas, turno.ServicioId);
        var profesionales = await db.Profesionales.Where(p => p.Activo).OrderBy(p => p.Apellidos).ThenBy(p => p.Nombre).Take(500).ToListAsync(ct);
        var asignaciones = await evaluacion.AsignacionesAsync(profesionales.Select(p => p.Id), turno.InicioUtc - datos.Horizonte, turno.FinUtc + datos.Horizonte, datos, ct);
        var yaEnNecesidad = (await db.Asignaciones.Where(a => a.NecesidadId == necesidadId && a.Estado != EstadoAsignacion.Cancelada)
            .Select(a => a.ProfesionalId).ToListAsync(ct)).ToHashSet();
        var esAdmin = contexto.TieneRol(Rol.AdministradorSociedad);

        var lista = profesionales.Select(p =>
        {
            var v = MotorReglas.EvaluarAsignacion(new ContextoEvaluacion
            {
                Profesional = p, Servicio = servicio, Necesidad = necesidad, Turno = ligero,
                Otras = asignaciones.GetValueOrDefault(p.Id) ?? [], Competencias = datos.Competencias, Servicios = datos.Servicios
            }, reglas);
            var bloqueantes = v.Where(x => x.Bloquea).ToList();
            var motivos = v.Select(x => x.Mensaje).ToList();
            if (yaEnNecesidad.Contains(p.Id)) motivos.Insert(0, "Ya está asignado a esta necesidad.");
            var requiereExcepcion = bloqueantes.Count > 0 && bloqueantes.All(b => b.PermiteExcepcion);
            return new CandidatoDto(p.Id, p.NombreCompleto, bloqueantes.Count == 0 && !yaEnNecesidad.Contains(p.Id),
                v.All(x => x.Codigo != "Solape"), motivos, v.Select(ViolacionDto.De).ToList(), requiereExcepcion && !yaEnNecesidad.Contains(p.Id),
                requiereExcepcion && bloqueantes.All(b => !b.RequiereAdministrador || esAdmin));
        }).ToList();

        return lista.OrderByDescending(c => c.Elegible).ThenByDescending(c => c.RequiereExcepcion && c.PuedeAutorizar)
            .ThenByDescending(c => c.Libre).ThenBy(c => c.NombreCompleto).ToList();
    }
}
