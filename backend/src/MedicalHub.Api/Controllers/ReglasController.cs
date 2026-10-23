using MedicalHub.Api.Seguridad;
using MedicalHub.Application.Reglas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalHub.Api.Controllers;

/// <summary>Reglas de planificación de la sociedad. Las configura y revisa su administrador; el coordinador puede consultarlas.</summary>
[ApiController]
[Route("api/reglas-planificacion")]
public sealed class ReglasController(ReglasServicio servicio) : ControllerBase
{
    /// <summary>Catálogo de tipos de regla con la descripción de sus parámetros.</summary>
    [HttpGet("tipos")]
    [Authorize(Politicas.GestionPlanificacion)]
    public ActionResult<IReadOnlyList<DefinicionTipoRegla>> Tipos() => Ok(servicio.Tipos());

    [HttpGet]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<IReadOnlyList<ReglaDto>>> Listar(CancellationToken ct) => Ok(await servicio.ListarAsync(ct));

    [HttpPost]
    [Authorize(Politicas.AdministradorSociedad)]
    public async Task<ActionResult<ReglaDto>> Crear(SolicitudRegla solicitud, CancellationToken ct)
        => Created("/api/reglas-planificacion", await servicio.CrearAsync(solicitud, ct));

    [HttpPut("{id:guid}")]
    [Authorize(Politicas.AdministradorSociedad)]
    public async Task<ActionResult<ReglaDto>> Actualizar(Guid id, SolicitudRegla solicitud, CancellationToken ct)
        => await servicio.ActualizarAsync(id, solicitud, ct);

    [HttpPost("{id:guid}/activar")]
    [Authorize(Politicas.AdministradorSociedad)]
    public async Task<IActionResult> Activar(Guid id, CancellationToken ct)
    {
        await servicio.CambiarEstadoAsync(id, true, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/desactivar")]
    [Authorize(Politicas.AdministradorSociedad)]
    public async Task<IActionResult> Desactivar(Guid id, CancellationToken ct)
    {
        await servicio.CambiarEstadoAsync(id, false, ct);
        return NoContent();
    }

    /// <summary>Registra que la sociedad ha revisado la regla.</summary>
    [HttpPost("{id:guid}/revisar")]
    [Authorize(Politicas.AdministradorSociedad)]
    public async Task<ActionResult<ReglaDto>> Revisar(Guid id, CancellationToken ct) => await servicio.RevisarAsync(id, ct);
}

[ApiController]
[Route("api/excepciones-reglas")]
public sealed class ExcepcionesReglasController(ExcepcionesServicio servicio) : ControllerBase
{
    [HttpGet]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<IReadOnlyList<ExcepcionDto>>> Listar([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta,
        [FromQuery] Guid? profesionalId, CancellationToken ct)
        => Ok(await servicio.ListarAsync(desde, hasta, profesionalId, ct));
}
