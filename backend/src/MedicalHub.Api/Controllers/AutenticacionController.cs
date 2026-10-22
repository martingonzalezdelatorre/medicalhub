using MedicalHub.Api.Seguridad;
using MedicalHub.Application.Identidad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MedicalHub.Api.Controllers;

[ApiController]
[Route("api/autenticacion")]
public sealed class AutenticacionController(AutenticacionServicio servicio) : ControllerBase
{
    /// <summary>Inicia sesión. Si la cuenta tiene MFA y falta el código, responde 401 con codigo = "mfa_requerido".</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(Politicas.LimiteLogin)]
    public async Task<ActionResult<RespuestaSesion>> Login(SolicitudLogin solicitud, CancellationToken ct)
        => await servicio.LoginAsync(solicitud, ct);

    [HttpPost("refrescar")]
    [AllowAnonymous]
    [EnableRateLimiting(Politicas.LimiteLogin)]
    public async Task<ActionResult<RespuestaSesion>> Refrescar(SolicitudRefresco solicitud, CancellationToken ct)
        => await servicio.RefrescarAsync(solicitud, ct);

    [HttpPost("cerrar-sesion")]
    [Authorize(Politicas.SesionIncluidaMfaPendiente)]
    public async Task<IActionResult> CerrarSesion(SolicitudRefresco solicitud, CancellationToken ct)
    {
        await servicio.CerrarSesionAsync(solicitud, ct);
        return NoContent();
    }

    [HttpGet("perfil")]
    [Authorize(Politicas.SesionIncluidaMfaPendiente)]
    public async Task<ActionResult<PerfilUsuario>> Perfil(CancellationToken ct)
        => await servicio.ObtenerPerfilAsync(ct);

    [HttpPost("cambiar-contrasena")]
    [Authorize(Politicas.SesionIncluidaMfaPendiente)]
    public async Task<IActionResult> CambiarContrasena(SolicitudCambioContrasena solicitud, CancellationToken ct)
    {
        await servicio.CambiarContrasenaAsync(solicitud, ct);
        return NoContent();
    }

    /// <summary>Genera un secreto TOTP para enlazar la app autenticadora. No queda activo hasta confirmarlo.</summary>
    [HttpPost("mfa/configurar")]
    [Authorize(Politicas.SesionIncluidaMfaPendiente)]
    public async Task<ActionResult<ConfiguracionMfa>> ConfigurarMfa(CancellationToken ct)
        => await servicio.ConfigurarMfaAsync(ct);

    /// <summary>Confirma el código de la app autenticadora y devuelve una sesión completa.</summary>
    [HttpPost("mfa/activar")]
    [Authorize(Politicas.SesionIncluidaMfaPendiente)]
    public async Task<ActionResult<RespuestaSesion>> ActivarMfa(SolicitudActivarMfa solicitud, CancellationToken ct)
        => await servicio.ActivarMfaAsync(solicitud, ct);
}
