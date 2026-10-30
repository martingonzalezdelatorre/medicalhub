using MedicalHub.Api.Seguridad;
using MedicalHub.Application.Planificacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalHub.Api.Controllers;

[ApiController]
[Route("api/centros")]
public sealed class CentrosController(CentrosServicio servicio) : ControllerBase
{
    [HttpGet]
    [Authorize(Politicas.LecturaEstructura)]
    public async Task<ActionResult<IReadOnlyList<CentroDto>>> Listar([FromQuery] bool incluirInactivos, CancellationToken ct)
        => Ok(await servicio.ListarAsync(incluirInactivos, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Politicas.LecturaEstructura)]
    public async Task<ActionResult<CentroDto>> Obtener(Guid id, CancellationToken ct) => await servicio.ObtenerAsync(id, ct);

    [HttpPost]
    [Authorize(Politicas.AdministradorSociedad)]
    public async Task<ActionResult<CentroDto>> Crear(SolicitudCentro solicitud, CancellationToken ct)
    {
        var dto = await servicio.CrearAsync(solicitud, ct);
        return CreatedAtAction(nameof(Obtener), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Politicas.AdministradorSociedad)]
    public async Task<ActionResult<CentroDto>> Actualizar(Guid id, SolicitudCentro solicitud, CancellationToken ct)
        => await servicio.ActualizarAsync(id, solicitud, ct);

    [HttpPut("{id:guid}/responsables")]
    [Authorize(Politicas.AdministradorSociedad)]
    public async Task<ActionResult<CentroDto>> EstablecerResponsables(Guid id, SolicitudResponsables solicitud, CancellationToken ct)
        => await servicio.EstablecerResponsablesAsync(id, solicitud, ct);

    /// <summary>Geovalla del centro para validar la ubicación de los fichajes. Con todos los campos a null se quita.</summary>
    [HttpPut("{id:guid}/geovalla")]
    [Authorize(Politicas.AdministradorSociedad)]
    public async Task<ActionResult<MedicalHub.Application.Horas.GeovallaDto>> EstablecerGeovalla(Guid id, MedicalHub.Application.Horas.SolicitudGeovalla solicitud, CancellationToken ct)
        => await servicio.EstablecerGeovallaAsync(id, solicitud, ct);

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
}

/// <summary>Coste por turno de cada perfil. Dato económico: solo lo ve y lo gestiona el administrador de la sociedad.</summary>
[ApiController]
[Route("api/servicios/{servicioId:guid}/tarifas")]
[Authorize(Politicas.GestionRecursosHumanos)]
public sealed class TarifasController(TarifasServicio servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TarifaDto>>> Listar(Guid servicioId, CancellationToken ct)
        => Ok(await servicio.ListarAsync(servicioId, ct));

    [HttpPut]
    public async Task<ActionResult<IReadOnlyList<TarifaDto>>> Reemplazar(Guid servicioId, SolicitudTarifas solicitud, CancellationToken ct)
        => Ok(await servicio.ReemplazarAsync(servicioId, solicitud, ct));
}

[ApiController]
[Route("api/servicios")]
public sealed class ServiciosController(ServiciosServicio servicio) : ControllerBase
{
    [HttpGet]
    [Authorize(Politicas.LecturaEstructura)]
    public async Task<ActionResult<IReadOnlyList<ServicioDto>>> Listar([FromQuery] Guid? centroId, [FromQuery] bool incluirInactivos, CancellationToken ct)
        => Ok(await servicio.ListarAsync(centroId, incluirInactivos, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Politicas.LecturaEstructura)]
    public async Task<ActionResult<ServicioDto>> Obtener(Guid id, CancellationToken ct) => await servicio.ObtenerAsync(id, ct);

    [HttpPost]
    [Authorize(Politicas.AdministradorSociedad)]
    public async Task<ActionResult<ServicioDto>> Crear(SolicitudServicio solicitud, CancellationToken ct)
    {
        var dto = await servicio.CrearAsync(solicitud, ct);
        return CreatedAtAction(nameof(Obtener), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Politicas.AdministradorSociedad)]
    public async Task<ActionResult<ServicioDto>> Actualizar(Guid id, SolicitudServicio solicitud, CancellationToken ct)
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

    [HttpPost("{id:guid}/tipos-turno")]
    [Authorize(Politicas.GestionRecursosHumanos)]
    public async Task<ActionResult<TipoTurnoDto>> CrearTipoTurno(Guid id, SolicitudTipoTurno solicitud, CancellationToken ct)
        => Created($"{Request.Path}", await servicio.CrearTipoTurnoAsync(id, solicitud, ct));

    [HttpPut("{id:guid}/tipos-turno/{tipoId:guid}")]
    [Authorize(Politicas.GestionRecursosHumanos)]
    public async Task<ActionResult<TipoTurnoDto>> ActualizarTipoTurno(Guid id, Guid tipoId, SolicitudTipoTurno solicitud, CancellationToken ct)
        => await servicio.ActualizarTipoTurnoAsync(id, tipoId, solicitud, ct);

    [HttpPost("{id:guid}/tipos-turno/{tipoId:guid}/activar")]
    [Authorize(Politicas.GestionRecursosHumanos)]
    public async Task<IActionResult> ActivarTipoTurno(Guid id, Guid tipoId, CancellationToken ct)
    {
        await servicio.CambiarEstadoTipoTurnoAsync(id, tipoId, true, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/tipos-turno/{tipoId:guid}/desactivar")]
    [Authorize(Politicas.GestionRecursosHumanos)]
    public async Task<IActionResult> DesactivarTipoTurno(Guid id, Guid tipoId, CancellationToken ct)
    {
        await servicio.CambiarEstadoTipoTurnoAsync(id, tipoId, false, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/requisitos")]
    [Authorize(Politicas.AdministradorSociedad)]
    public async Task<ActionResult<IReadOnlyList<RequisitoDto>>> ReemplazarRequisitos(Guid id, SolicitudRequisitos solicitud, CancellationToken ct)
        => Ok(await servicio.ReemplazarRequisitosAsync(id, solicitud, ct));

    /// <summary>Profesionales que cumplen las condiciones de dominio para el servicio en la fecha indicada (por defecto, hoy).</summary>
    [HttpGet("{id:guid}/profesionales-elegibles")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<IReadOnlyList<ElegibilidadDto>>> Elegibles(Guid id, [FromQuery] DateOnly? fecha,
        [FromQuery] bool incluirNoElegibles, CancellationToken ct)
        => Ok(await servicio.EvaluarProfesionalesAsync(id, fecha ?? DateOnly.FromDateTime(DateTime.UtcNow), incluirNoElegibles, ct));
}
