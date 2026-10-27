using MedicalHub.Api.Seguridad;
using MedicalHub.Application.Comun;
using MedicalHub.Application.Identidad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MedicalHub.Api.Controllers;

[ApiController]
[Route("api/autenticacion")]
public sealed class AutenticacionController(AutenticacionServicio servicio, OpcionesSeguridad seguridad, IWebHostEnvironment entorno) : ControllerBase
{
    /// <summary>Secure salvo en desarrollo local con HTTP (los navegadores no guardan cookies Secure recibidas por HTTP salvo en localhost).</summary>
    private bool Segura => seguridad.CookieRefrescoSegura && (!entorno.IsDevelopment() || Request.IsHttps);

    /// <summary>En modo cookie el token de refresco va en una cookie HttpOnly y se retira del cuerpo de la respuesta.</summary>
    private ActionResult<RespuestaSesion> Responder(RespuestaSesion r)
    {
        if (!SesionCookie.EsModoCookie(Request)) return r;
        SesionCookie.Escribir(Response, r.TokenRefresco, r.RefrescoExpira, Segura);
        return r with { TokenRefresco = "" };
    }

    /// <summary>Inicia sesión. Si la cuenta tiene MFA y falta el código, responde 401 con codigo = "mfa_requerido".</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(Politicas.LimiteLogin)]
    public async Task<ActionResult<RespuestaSesion>> Login(SolicitudLogin solicitud, CancellationToken ct)
        => Responder(await servicio.LoginAsync(solicitud, ct));

    [HttpPost("refrescar")]
    [AllowAnonymous]
    [EnableRateLimiting(Politicas.LimiteLogin)]
    public async Task<ActionResult<RespuestaSesion>> Refrescar(SolicitudRefresco? solicitud, CancellationToken ct)
    {
        var cookie = SesionCookie.EsModoCookie(Request);
        var token = cookie ? SesionCookie.Leer(Request) : solicitud?.TokenRefresco;
        try
        {
            return Responder(await servicio.RefrescarAsync(new SolicitudRefresco(token), ct));
        }
        catch (ExcepcionNoAutenticado)
        {
            // Cookie caducada, revocada o reutilizada: se elimina para no reintentarla en cada arranque
            // El manejador de excepciones limpia las cabeceras de la respuesta: la cookie se borra justo al empezar a escribirla.
            if (cookie)
            {
                var segura = Segura;
                Response.OnStarting(() =>
                {
                    SesionCookie.Borrar(Response, segura);
                    return Task.CompletedTask;
                });
            }
            throw;
        }
    }

    [HttpPost("cerrar-sesion")]
    [Authorize(Politicas.SesionIncluidaMfaPendiente)]
    public async Task<IActionResult> CerrarSesion(SolicitudRefresco? solicitud, CancellationToken ct)
    {
        var cookie = SesionCookie.EsModoCookie(Request);
        await servicio.CerrarSesionAsync(new SolicitudRefresco(cookie ? SesionCookie.Leer(Request) : solicitud?.TokenRefresco), ct);
        if (cookie) SesionCookie.Borrar(Response, Segura);
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
        => Responder(await servicio.ActivarMfaAsync(solicitud, ct));
}
