using MedicalHub.Api.Seguridad;
using MedicalHub.Application.Planificacion;
using MedicalHub.Domain.Catalogos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalHub.Api.Controllers;

/// <summary>Comportamiento común de los catálogos de la sociedad. Lectura: cualquier usuario de la sociedad; escritura: su administrador.</summary>
[ApiController]
public abstract class CatalogoControllerBase<T>(CatalogosServicio servicio) : ControllerBase
    where T : EntidadCatalogo, ICreableCatalogo<T>
{
    [HttpGet]
    [Authorize(Politicas.UsuarioSociedad)]
    public async Task<ActionResult<IReadOnlyList<CatalogoDto>>> Listar([FromQuery] bool incluirInactivas, CancellationToken ct)
        => Ok(await servicio.ListarAsync<T>(incluirInactivas, ct));

    [HttpPost]
    [Authorize(Politicas.AdministradorSociedad)]
    public async Task<ActionResult<CatalogoDto>> Crear(SolicitudCatalogo solicitud, CancellationToken ct)
    {
        var dto = await servicio.CrearAsync<T>(solicitud, ct);
        return Created($"{Request.Path}/{dto.Id}", dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Politicas.AdministradorSociedad)]
    public async Task<ActionResult<CatalogoDto>> Actualizar(Guid id, SolicitudCatalogo solicitud, CancellationToken ct)
        => await servicio.ActualizarAsync<T>(id, solicitud, ct);

    [HttpPost("{id:guid}/activar")]
    [Authorize(Politicas.AdministradorSociedad)]
    public async Task<IActionResult> Activar(Guid id, CancellationToken ct)
    {
        await servicio.CambiarEstadoAsync<T>(id, true, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/desactivar")]
    [Authorize(Politicas.AdministradorSociedad)]
    public async Task<IActionResult> Desactivar(Guid id, CancellationToken ct)
    {
        await servicio.CambiarEstadoAsync<T>(id, false, ct);
        return NoContent();
    }
}

[Route("api/especialidades")]
public sealed class EspecialidadesController(CatalogosServicio s) : CatalogoControllerBase<Especialidad>(s);

[Route("api/categorias-profesionales")]
public sealed class CategoriasProfesionalesController(CatalogosServicio s) : CatalogoControllerBase<CategoriaProfesional>(s);

[Route("api/competencias")]
public sealed class CompetenciasController(CatalogosServicio s) : CatalogoControllerBase<Competencia>(s);
