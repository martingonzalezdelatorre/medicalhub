using MedicalHub.Application.Comun;
using MedicalHub.Domain.Identidad;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Identidad;

/// <summary>Gestión de usuarios de la sociedad del solicitante. Las consultas quedan acotadas por el filtro global de organización.</summary>
public sealed class UsuariosServicio(
    IAppDbContext db,
    IHasherContrasena hasher,
    IContextoActual contexto,
    TimeProvider reloj,
    OpcionesSeguridad opciones)
{
    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(CancellationToken ct)
    {
        ExigirSociedad();
        var usuarios = await db.Usuarios.OrderBy(u => u.NombreCompleto).ToListAsync(ct);
        return usuarios.Select(ADto).ToList();
    }

    public async Task<UsuarioDto> ObtenerAsync(Guid id, CancellationToken ct)
        => ADto(await BuscarAsync(id, ct));

    public async Task<UsuarioDto> CrearAsync(SolicitudCrearUsuario s, CancellationToken ct)
    {
        var organizacionId = ExigirSociedad();
        var v = new Validador().Requerido(nameof(s.Email), s.Email, 254).Requerido(nameof(s.NombreCompleto), s.NombreCompleto);
        ValidarRoles(v, s.Roles);
        PoliticaContrasena.Validar(v, nameof(s.Contrasena), s.Contrasena, s.Email, opciones.LongitudMinimaContrasena);
        v.LanzarSiHayErrores();

        var email = Usuario.NormalizarEmail(s.Email);
        if (await db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Email == email, ct))
            throw new ExcepcionConflicto("No se puede crear un usuario con ese correo electrónico.");

        var usuario = new Usuario(organizacionId, email, s.NombreCompleto, hasher.Hash(s.Contrasena), s.Roles, reloj.GetUtcNow());
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(ct);
        return ADto(usuario);
    }

    public async Task<UsuarioDto> ActualizarAsync(Guid id, SolicitudActualizarUsuario s, CancellationToken ct)
    {
        var usuario = await BuscarAsync(id, ct);
        var v = new Validador().Requerido(nameof(s.NombreCompleto), s.NombreCompleto);
        ValidarRoles(v, s.Roles);
        v.LanzarSiHayErrores();

        if (id == contexto.UsuarioId && !s.Roles.Contains(Rol.AdministradorSociedad))
            throw new ExcepcionConflicto("No puede retirarse a sí mismo el rol de administrador de la sociedad.");

        usuario.EstablecerRoles(s.Roles);
        usuario.CambiarNombre(s.NombreCompleto);
        await db.SaveChangesAsync(ct);
        return ADto(usuario);
    }

    public async Task CambiarEstadoAsync(Guid id, bool activo, CancellationToken ct)
    {
        var usuario = await BuscarAsync(id, ct);
        if (activo) usuario.Activar();
        else
        {
            if (id == contexto.UsuarioId) throw new ExcepcionConflicto("No puede desactivar su propia cuenta.");
            usuario.Desactivar();
            var ahora = reloj.GetUtcNow();
            foreach (var t in await db.TokensRefresco.Where(t => t.UsuarioId == id && t.RevocadoEn == null).ToListAsync(ct))
                t.Revocar(ahora);
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task ReiniciarMfaAsync(Guid id, CancellationToken ct)
    {
        var usuario = await BuscarAsync(id, ct);
        usuario.ReiniciarMfa();
        await db.SaveChangesAsync(ct);
    }

    private async Task<Usuario> BuscarAsync(Guid id, CancellationToken ct)
    {
        ExigirSociedad();
        // El filtro global por organización hace que un id de otra sociedad devuelva "no encontrado".
        return await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new ExcepcionNoEncontrado("Usuario no encontrado.");
    }

    private Guid ExigirSociedad()
        => contexto.OrganizacionId ?? throw new ExcepcionProhibido("Esta operación requiere pertenecer a una sociedad.");

    private static void ValidarRoles(Validador v, IReadOnlyList<Rol>? roles)
    {
        if (roles is null || roles.Count == 0) { v.Agregar("Roles", "Debe indicar al menos un rol."); return; }
        v.Cuando(roles.Any(r => !Enum.IsDefined(r)), "Roles", "Hay un rol no válido.");
        v.Cuando(roles.Contains(Rol.AdministradorPlataforma), "Roles", "No se puede asignar el rol de administrador de plataforma.");
    }

    private static UsuarioDto ADto(Usuario u)
        => new(u.Id, u.Email, u.NombreCompleto, u.ListaRoles.ToList(), u.Activo, u.MfaActivado);
}
