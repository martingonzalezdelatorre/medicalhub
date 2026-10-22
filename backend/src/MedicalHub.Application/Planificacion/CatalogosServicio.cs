using MedicalHub.Application.Comun;
using MedicalHub.Domain.Catalogos;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Planificacion;

/// <summary>Especialidades, categorías y competencias: mismo comportamiento, datos propios de cada sociedad.</summary>
public sealed class CatalogosServicio(IAppDbContext db, IContextoActual contexto, TimeProvider reloj)
{
    public async Task<IReadOnlyList<CatalogoDto>> ListarAsync<T>(bool incluirInactivas, CancellationToken ct)
        where T : EntidadCatalogo
    {
        var consulta = db.Set<T>().AsQueryable();
        if (!incluirInactivas) consulta = consulta.Where(x => x.Activa);
        var lista = await consulta.OrderBy(x => x.Nombre).ToListAsync(ct);
        return lista.Select(ADto).ToList();
    }

    public async Task<CatalogoDto> CrearAsync<T>(SolicitudCatalogo s, CancellationToken ct)
        where T : EntidadCatalogo, ICreableCatalogo<T>
    {
        var org = contexto.ExigirOrganizacion();
        new Validador().Requerido(nameof(s.Nombre), s.Nombre, 120).LanzarSiHayErrores();
        await ComprobarDuplicadoAsync<T>(s.Nombre, null, ct);

        var entidad = T.Crear(org, s.Nombre, s.Descripcion, reloj.GetUtcNow());
        db.Set<T>().Add(entidad);
        await db.SaveChangesAsync(ct);
        return ADto(entidad);
    }

    public async Task<CatalogoDto> ActualizarAsync<T>(Guid id, SolicitudCatalogo s, CancellationToken ct) where T : EntidadCatalogo
    {
        new Validador().Requerido(nameof(s.Nombre), s.Nombre, 120).LanzarSiHayErrores();
        var entidad = await BuscarAsync<T>(id, ct);
        await ComprobarDuplicadoAsync<T>(s.Nombre, id, ct);
        entidad.Actualizar(s.Nombre, s.Descripcion);
        await db.SaveChangesAsync(ct);
        return ADto(entidad);
    }

    public async Task CambiarEstadoAsync<T>(Guid id, bool activa, CancellationToken ct) where T : EntidadCatalogo
    {
        var entidad = await BuscarAsync<T>(id, ct);
        if (activa) entidad.Activar(); else entidad.Desactivar();
        await db.SaveChangesAsync(ct);
    }

    private async Task<T> BuscarAsync<T>(Guid id, CancellationToken ct) where T : EntidadCatalogo
        => await db.Set<T>().FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new ExcepcionNoEncontrado("Elemento no encontrado.");

    private async Task ComprobarDuplicadoAsync<T>(string nombre, Guid? excepto, CancellationToken ct) where T : EntidadCatalogo
    {
        var n = nombre.Trim();
        if (await db.Set<T>().AnyAsync(x => x.Nombre == n && x.Id != excepto, ct))
            throw new ExcepcionConflicto("Ya existe un elemento con ese nombre.");
    }

    private static CatalogoDto ADto(EntidadCatalogo c) => new(c.Id, c.Nombre, c.Descripcion, c.Activa);
}
