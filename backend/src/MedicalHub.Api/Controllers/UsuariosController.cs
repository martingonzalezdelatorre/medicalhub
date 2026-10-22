using MedicalHub.Application.Identidad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalHub.Api.Controllers;

/// <summary>Usuarios de la sociedad del solicitante (la organización sale del token, nunca de la petición).</summary>
[ApiController]
[Route("api/usuarios")]
[Authorize(MedicalHub.Api.Seguridad.Politicas.AdministradorSociedad)]
public sealed class UsuariosController(UsuariosServicio servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UsuarioDto>>> Listar(CancellationToken ct)
        => Ok(await servicio.ListarAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UsuarioDto>> Obtener(Guid id, CancellationToken ct)
        => await servicio.ObtenerAsync(id, ct);

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Crear(SolicitudCrearUsuario solicitud, CancellationToken ct)
    {
        var usuario = await servicio.CrearAsync(solicitud, ct);
        return CreatedAtAction(nameof(Obtener), new { id = usuario.Id }, usuario);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UsuarioDto>> Actualizar(Guid id, SolicitudActualizarUsuario solicitud, CancellationToken ct)
        => await servicio.ActualizarAsync(id, solicitud, ct);

    [HttpPost("{id:guid}/desactivar")]
    public async Task<IActionResult> Desactivar(Guid id, CancellationToken ct)
    {
        await servicio.CambiarEstadoAsync(id, false, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/activar")]
    public async Task<IActionResult> Activar(Guid id, CancellationToken ct)
    {
        await servicio.CambiarEstadoAsync(id, true, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reiniciar-mfa")]
    public async Task<IActionResult> ReiniciarMfa(Guid id, CancellationToken ct)
    {
        await servicio.ReiniciarMfaAsync(id, ct);
        return NoContent();
    }
}
