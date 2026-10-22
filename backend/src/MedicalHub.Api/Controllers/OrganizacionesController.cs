using MedicalHub.Application.Identidad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalHub.Api.Controllers;

/// <summary>Alta y listado de sociedades. Exclusivo del administrador de plataforma; no expone datos operativos.</summary>
[ApiController]
[Route("api/organizaciones")]
[Authorize(MedicalHub.Api.Seguridad.Politicas.AdministradorPlataforma)]
public sealed class OrganizacionesController(OrganizacionesServicio servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrganizacionDto>>> Listar(CancellationToken ct)
        => Ok(await servicio.ListarAsync(ct));

    [HttpPost]
    public async Task<ActionResult<OrganizacionDto>> Crear(SolicitudCrearOrganizacion solicitud, CancellationToken ct)
    {
        var org = await servicio.CrearAsync(solicitud, ct);
        return CreatedAtAction(nameof(Listar), new { }, org);
    }
}
