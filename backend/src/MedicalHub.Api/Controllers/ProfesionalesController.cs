using MedicalHub.Api.Seguridad;
using MedicalHub.Application.Planificacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalHub.Api.Controllers;

[ApiController]
[Route("api/profesionales")]
public sealed class ProfesionalesController(ProfesionalesServicio servicio) : ControllerBase
{
    /// <summary>Los responsables de centro solo ven profesionales autorizados en sus servicios y sin datos de contacto.</summary>
    [HttpGet]
    [Authorize(Politicas.LecturaSociedad)]
    public async Task<ActionResult<IReadOnlyList<ProfesionalDto>>> Listar([FromQuery] bool? activo, [FromQuery] Guid? categoriaId,
        [FromQuery] Guid? servicioId, [FromQuery] string? texto, CancellationToken ct)
        => Ok(await servicio.ListarAsync(new FiltroProfesionales(activo, categoriaId, servicioId, texto), ct));

    /// <summary>Ficha del propio profesional (usuario vinculado).</summary>
    [HttpGet("mi-perfil")]
    [Authorize(Politicas.UsuarioSociedad)]
    public async Task<ActionResult<ProfesionalDto>> MiPerfil(CancellationToken ct) => await servicio.MiPerfilAsync(ct);

    [HttpGet("{id:guid}")]
    [Authorize(Politicas.LecturaSociedad)]
    public async Task<ActionResult<ProfesionalDto>> Obtener(Guid id, CancellationToken ct) => await servicio.ObtenerAsync(id, ct);

    [HttpGet("{id:guid}/historial")]
    [Authorize(Politicas.AdministradorSociedad)]
    public async Task<ActionResult<IReadOnlyList<CambioHistorialDto>>> Historial(Guid id, CancellationToken ct)
        => Ok(await servicio.HistorialAsync(id, ct));

    [HttpPost]
    [Authorize(Politicas.AdministradorSociedad)]
    public async Task<ActionResult<ProfesionalDto>> Crear(SolicitudProfesional solicitud, CancellationToken ct)
    {
        var dto = await servicio.CrearAsync(solicitud, ct);
        return CreatedAtAction(nameof(Obtener), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Politicas.AdministradorSociedad)]
    public async Task<ActionResult<ProfesionalDto>> Actualizar(Guid id, SolicitudProfesional solicitud, CancellationToken ct)
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
}
