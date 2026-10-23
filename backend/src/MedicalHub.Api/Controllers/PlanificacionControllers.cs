using MedicalHub.Api.Seguridad;
using MedicalHub.Application.Planificacion;
using MedicalHub.Domain.Planificacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalHub.Api.Controllers;

[ApiController]
[Route("api/cuadrantes")]
public sealed class CuadrantesController(CuadrantesServicio servicio, TurnosServicio turnos) : ControllerBase
{
    [HttpGet]
    [Authorize(Politicas.LecturaSociedad)]
    public async Task<ActionResult<IReadOnlyList<CuadranteDto>>> Listar([FromQuery] Guid? servicioId, [FromQuery] EstadoCuadrante? estado, CancellationToken ct)
        => Ok(await servicio.ListarAsync(servicioId, estado, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Politicas.LecturaSociedad)]
    public async Task<ActionResult<CuadranteDto>> Obtener(Guid id, CancellationToken ct) => await servicio.ObtenerAsync(id, ct);

    [HttpPost]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<CuadranteDto>> Crear(SolicitudCuadrante solicitud, CancellationToken ct)
    {
        var dto = await servicio.CrearAsync(solicitud, ct);
        return CreatedAtAction(nameof(Obtener), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}/nombre")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<IActionResult> Renombrar(Guid id, SolicitudRenombrar solicitud, CancellationToken ct)
    {
        await servicio.RenombrarAsync(id, solicitud, ct);
        return NoContent();
    }

    /// <summary>Elimina un borrador que nunca se publicó.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        await servicio.EliminarBorradorAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/validacion")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<ValidacionDto>> Validar(Guid id, CancellationToken ct) => await servicio.ValidarAsync(id, ct);

    [HttpPost("{id:guid}/publicar")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<CuadranteDto>> Publicar(Guid id, CancellationToken ct) => await servicio.PublicarAsync(id, ct);

    [HttpPost("{id:guid}/reabrir")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<CuadranteDto>> Reabrir(Guid id, CancellationToken ct) => await servicio.ReabrirAsync(id, ct);

    [HttpPost("{id:guid}/archivar")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<CuadranteDto>> Archivar(Guid id, CancellationToken ct) => await servicio.ArchivarAsync(id, ct);

    [HttpGet("{id:guid}/versiones")]
    [Authorize(Politicas.LecturaSociedad)]
    public async Task<ActionResult<IReadOnlyList<VersionResumenDto>>> Versiones(Guid id, CancellationToken ct) => Ok(await servicio.VersionesAsync(id, ct));

    [HttpGet("{id:guid}/versiones/{numero:int}")]
    [Authorize(Politicas.LecturaSociedad)]
    public async Task<ActionResult<VersionDetalleDto>> Version(Guid id, int numero, CancellationToken ct) => await servicio.VersionAsync(id, numero, ct);

    [HttpPost("{id:guid}/turnos")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<IdTurno>> CrearTurno(Guid id, SolicitudTurno solicitud, CancellationToken ct)
        => Created($"/api/turnos", new IdTurno(await turnos.CrearAsync(id, solicitud, ct)));

    [HttpPost("{id:guid}/plantilla")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<PlantillaDto>> GuardarComoPlantilla(Guid id, SolicitudPlantilla solicitud, CancellationToken ct)
        => Created("/api/plantillas-cuadrante", await servicio.GuardarComoPlantillaAsync(id, solicitud, ct));

    [HttpPost("desde-plantilla")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<CuadranteDto>> DesdePlantilla(SolicitudDesdePlantilla solicitud, CancellationToken ct)
    {
        var dto = await servicio.CrearDesdePlantillaAsync(solicitud, ct);
        return CreatedAtAction(nameof(Obtener), new { id = dto.Id }, dto);
    }

    /// <summary>Exporta el cuadrante a CSV (la exportación queda registrada en los eventos de seguridad).</summary>
    [HttpGet("{id:guid}/exportar.csv")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<IActionResult> ExportarCsv(Guid id, CancellationToken ct)
    {
        var archivo = await servicio.ExportarCsvAsync(id, ct);
        return File(archivo.Contenido, archivo.TipoContenido, archivo.NombreArchivo);
    }

    public sealed record IdTurno(Guid Id);
}

[ApiController]
[Route("api/plantillas-cuadrante")]
public sealed class PlantillasCuadranteController(CuadrantesServicio servicio) : ControllerBase
{
    [HttpGet]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<IReadOnlyList<PlantillaDto>>> Listar([FromQuery] Guid? servicioId, CancellationToken ct)
        => Ok(await servicio.PlantillasAsync(servicioId, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        await servicio.EliminarPlantillaAsync(id, ct);
        return NoContent();
    }
}

[ApiController]
public sealed class TurnosController(CalendarioServicio calendario, TurnosServicio turnos, AsignacionesServicio asignaciones) : ControllerBase
{
    /// <summary>Turnos del rango (máx. 100 días) para las vistas de calendario, con filtros opcionales.</summary>
    [HttpGet("api/turnos")]
    [Authorize(Politicas.LecturaSociedad)]
    public async Task<ActionResult<IReadOnlyList<TurnoDto>>> Calendario([FromQuery] DateOnly desde, [FromQuery] DateOnly hasta,
        [FromQuery] Guid? centroId, [FromQuery] Guid? servicioId, [FromQuery] Guid? profesionalId, [FromQuery] Guid? categoriaId,
        [FromQuery] Guid? cuadranteId, [FromQuery] bool soloSinCubrir, CancellationToken ct)
        => Ok(await calendario.ConsultarAsync(new FiltroCalendario(desde, hasta, centroId, servicioId, profesionalId, categoriaId, cuadranteId, soloSinCubrir), ct));

    /// <summary>Planificación del propio profesional (solo cuadrantes publicados).</summary>
    [HttpGet("api/mi-planificacion")]
    [Authorize(Politicas.UsuarioSociedad)]
    public async Task<ActionResult<IReadOnlyList<TurnoDto>>> MiPlanificacion([FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, CancellationToken ct)
        => Ok(await calendario.MiPlanificacionAsync(desde, hasta, ct));

    [HttpDelete("api/turnos/{id:guid}")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        await turnos.EliminarAsync(id, ct);
        return NoContent();
    }

    [HttpPut("api/turnos/{id:guid}/necesidades")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<IActionResult> ActualizarNecesidades(Guid id, SolicitudNecesidades solicitud, CancellationToken ct)
    {
        await turnos.ActualizarNecesidadesAsync(id, solicitud, ct);
        return NoContent();
    }

    [HttpGet("api/necesidades/{id:guid}/candidatos")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<IReadOnlyList<CandidatoDto>>> Candidatos(Guid id, CancellationToken ct) => Ok(await asignaciones.CandidatosAsync(id, ct));

    [HttpPost("api/necesidades/{id:guid}/asignaciones")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<MedicalHub.Application.Reglas.ResultadoAsignacionDto>> Asignar(Guid id, SolicitudAsignacion solicitud, CancellationToken ct)
        => Created($"/api/asignaciones", await asignaciones.AsignarAsync(id, solicitud.ProfesionalId, solicitud.Excepcion, ct));

    /// <summary>Autoriza con justificación las infracciones bloqueantes (que admiten excepción) de una asignación ya hecha.</summary>
    [HttpPost("api/asignaciones/{id:guid}/excepciones")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<IReadOnlyList<MedicalHub.Application.Reglas.ViolacionDto>>> AutorizarExcepcion(Guid id, MedicalHub.Application.Reglas.SolicitudExcepcion solicitud, CancellationToken ct)
        => Ok(await asignaciones.AutorizarExcepcionAsync(id, solicitud, ct));

    [HttpDelete("api/asignaciones/{id:guid}")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<IActionResult> Quitar(Guid id, CancellationToken ct)
    {
        await asignaciones.QuitarAsync(id, ct);
        return NoContent();
    }
}
