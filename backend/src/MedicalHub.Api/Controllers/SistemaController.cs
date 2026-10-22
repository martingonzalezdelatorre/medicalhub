using Microsoft.AspNetCore.Mvc;

namespace MedicalHub.Api.Controllers;

[ApiController]
[Route("api/sistema")]
public sealed class SistemaController : ControllerBase
{
    public sealed record InformacionSistema(string Nombre, string Version, string Entorno);

    /// <summary>Información pública mínima de la API.</summary>
    [HttpGet("info")]
    [ProducesResponseType<InformacionSistema>(StatusCodes.Status200OK)]
    public ActionResult<InformacionSistema> Info([FromServices] IWebHostEnvironment entorno)
        => new InformacionSistema("MedicalHub API",
            typeof(SistemaController).Assembly.GetName().Version?.ToString(3) ?? "0.0.0",
            entorno.EnvironmentName);
}
