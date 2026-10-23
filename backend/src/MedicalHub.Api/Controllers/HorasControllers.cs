using MedicalHub.Api.Seguridad;
using MedicalHub.Application.Horas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalHub.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(DashboardServicio servicio) : ControllerBase
{
    /// <summary>Indicadores según el rol del usuario (gestión y/o profesional).</summary>
    [HttpGet]
    [Authorize(Politicas.UsuarioSociedad)]
    public async Task<ActionResult<DashboardDto>> Obtener(CancellationToken ct) => await servicio.ObtenerAsync(ct);
}

[ApiController]
public sealed class HorasController(HorasServicio servicio) : ControllerBase
{
    /// <summary>Asignaciones del periodo con horas planificadas, registradas y ajustes (dentro del alcance del usuario).</summary>
    [HttpGet("api/horas")]
    [Authorize(Politicas.LecturaSociedad)]
    public async Task<ActionResult<IReadOnlyList<LineaHorasDto>>> Listar([FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, [FromQuery] Guid? profesionalId,
        [FromQuery] Guid? servicioId, [FromQuery] Guid? centroId, CancellationToken ct)
        => Ok(await servicio.ListarAsync(new FiltroHoras(desde, hasta, profesionalId, servicioId, centroId), ct));

    [HttpGet("api/mis-horas")]
    [Authorize(Politicas.UsuarioSociedad)]
    public async Task<ActionResult<IReadOnlyList<LineaHorasDto>>> Mias([FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, CancellationToken ct)
        => Ok(await servicio.MisHorasAsync(desde, hasta, ct));

    /// <summary>Registra (o corrige) las horas realmente trabajadas de una asignación ya iniciada.</summary>
    [HttpPut("api/asignaciones/{id:guid}/horas")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<LineaHorasDto>> Registrar(Guid id, SolicitudRegistroHoras solicitud, CancellationToken ct)
        => await servicio.RegistrarAsync(id, solicitud, ct);

    /// <summary>Añade un ajuste en minutos con motivo obligatorio. Los ajustes no se modifican ni se borran.</summary>
    [HttpPost("api/asignaciones/{id:guid}/horas/ajustes")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<LineaHorasDto>> Ajustar(Guid id, SolicitudAjusteHoras solicitud, CancellationToken ct)
        => Created($"/api/asignaciones/{id}/horas", await servicio.AjustarAsync(id, solicitud, ct));
}

[ApiController]
[Route("api/informes")]
public sealed class InformesController(InformesServicio servicio) : ControllerBase
{
    [HttpGet("horas")]
    [Authorize(Politicas.LecturaSociedad)]
    public async Task<ActionResult<InformeHorasDto>> Horas([FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, [FromQuery] Guid? profesionalId,
        [FromQuery] Guid? servicioId, [FromQuery] Guid? centroId, CancellationToken ct)
        => await servicio.HorasAsync(new FiltroHoras(desde, hasta, profesionalId, servicioId, centroId), ct);

    [HttpGet("horas.csv")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<IActionResult> HorasCsv([FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, [FromQuery] Guid? profesionalId,
        [FromQuery] Guid? servicioId, [FromQuery] Guid? centroId, CancellationToken ct)
    {
        var a = await servicio.HorasCsvAsync(new FiltroHoras(desde, hasta, profesionalId, servicioId, centroId), ct);
        return File(a.Contenido, a.TipoContenido, a.NombreArchivo);
    }

    [HttpGet("cobertura")]
    [Authorize(Politicas.LecturaSociedad)]
    public async Task<ActionResult<InformeCoberturaDto>> Cobertura([FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, [FromQuery] Guid? servicioId,
        [FromQuery] Guid? centroId, CancellationToken ct)
        => await servicio.CoberturaAsync(desde, hasta, servicioId, centroId, ct);

    [HttpGet("cobertura.csv")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<IActionResult> CoberturaCsv([FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, [FromQuery] Guid? servicioId, [FromQuery] Guid? centroId, CancellationToken ct)
    {
        var a = await servicio.CoberturaCsvAsync(desde, hasta, servicioId, centroId, ct);
        return File(a.Contenido, a.TipoContenido, a.NombreArchivo);
    }
}
