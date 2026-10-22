using MedicalHub.Application.Comun;
using MedicalHub.Domain.Estructura;
using MedicalHub.Domain.Identidad;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Planificacion;

public sealed class CentrosServicio(IAppDbContext db, IContextoActual contexto, TimeProvider reloj)
{
    /// <summary>Centros que el usuario puede ver: todos, o solo los suyos si es responsable de centro.</summary>
    public static IQueryable<Centro> Visibles(IAppDbContext db, IContextoActual c)
    {
        var uid = c.UsuarioId;
        return c.EsAlcanceRestringido()
            ? db.Centros.Where(x => x.Responsables.Any(r => r.UsuarioId == uid))
            : db.Centros;
    }

    public async Task<IReadOnlyList<CentroDto>> ListarAsync(bool incluirInactivos, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var q = Visibles(db, contexto);
        if (!incluirInactivos) q = q.Where(c => c.Activo);
        return (await q.OrderBy(c => c.Nombre).ToListAsync(ct)).Select(ADto).ToList();
    }

    public async Task<CentroDto> ObtenerAsync(Guid id, CancellationToken ct) => ADto(await BuscarAsync(id, ct));

    public async Task<CentroDto> CrearAsync(SolicitudCentro s, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        new Validador().Requerido(nameof(s.Nombre), s.Nombre).Requerido(nameof(s.ZonaHoraria), s.ZonaHoraria, 64).LanzarSiHayErrores();
        await ComprobarDuplicadoAsync(s.Nombre, null, ct);

        var centro = new Centro(org, s.Nombre, s.Tipo, s.Direccion, s.ZonaHoraria, reloj.GetUtcNow());
        db.Centros.Add(centro);
        await db.SaveChangesAsync(ct);
        return ADto(centro);
    }

    public async Task<CentroDto> ActualizarAsync(Guid id, SolicitudCentro s, CancellationToken ct)
    {
        new Validador().Requerido(nameof(s.Nombre), s.Nombre).Requerido(nameof(s.ZonaHoraria), s.ZonaHoraria, 64).LanzarSiHayErrores();
        var centro = await BuscarAsync(id, ct);
        await ComprobarDuplicadoAsync(s.Nombre, id, ct);
        centro.Actualizar(s.Nombre, s.Tipo, s.Direccion, s.ZonaHoraria);
        await db.SaveChangesAsync(ct);
        return ADto(centro);
    }

    public async Task CambiarEstadoAsync(Guid id, bool activo, CancellationToken ct)
    {
        var centro = await BuscarAsync(id, ct);
        if (activo) centro.Activar(); else centro.Desactivar();
        await db.SaveChangesAsync(ct);
    }

    public async Task<CentroDto> EstablecerResponsablesAsync(Guid id, SolicitudResponsables s, CancellationToken ct)
    {
        var centro = await BuscarAsync(id, ct);
        var ids = (s.UsuariosIds ?? []).Distinct().ToList();

        // Los usuarios se buscan con el filtro de organización: un id de otra sociedad cuenta como inexistente.
        var validos = await db.Usuarios
            .Where(u => ids.Contains(u.Id) && u.Activo && u.Roles.Any(r => r.Rol == Rol.ResponsableCentro))
            .CountAsync(ct);
        if (validos != ids.Count)
        {
            var v = new Validador();
            v.Agregar(nameof(s.UsuariosIds), "Todos los usuarios deben existir, estar activos y tener el rol de responsable de centro.");
            v.LanzarSiHayErrores();
        }

        centro.EstablecerResponsables(ids);
        await db.SaveChangesAsync(ct);
        return ADto(centro);
    }

    private async Task<Centro> BuscarAsync(Guid id, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        return await Visibles(db, contexto).FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new ExcepcionNoEncontrado("Centro no encontrado.");
    }

    private async Task ComprobarDuplicadoAsync(string nombre, Guid? excepto, CancellationToken ct)
    {
        var n = nombre.Trim();
        if (await db.Centros.AnyAsync(c => c.Nombre == n && c.Id != excepto, ct))
            throw new ExcepcionConflicto("Ya existe un centro con ese nombre.");
    }

    private static CentroDto ADto(Centro c) => new(c.Id, c.Nombre, c.Tipo, c.Direccion, c.ZonaHoraria, c.Activo,
        c.Responsables.Select(r => r.UsuarioId).ToList());
}
