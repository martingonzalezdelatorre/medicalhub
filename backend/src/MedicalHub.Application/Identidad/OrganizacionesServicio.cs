using MedicalHub.Application.Comun;
using MedicalHub.Domain.Identidad;
using MedicalHub.Domain.Organizaciones;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Identidad;

/// <summary>Gestión de sociedades por el administrador de plataforma. No da acceso a datos operativos de las sociedades.</summary>
public sealed class OrganizacionesServicio(
    IAppDbContext db,
    IHasherContrasena hasher,
    TimeProvider reloj,
    OpcionesSeguridad opciones)
{
    public async Task<IReadOnlyList<OrganizacionDto>> ListarAsync(CancellationToken ct)
        => (await db.Organizaciones.OrderBy(o => o.Nombre).ToListAsync(ct)).Select(ADto).ToList();

    public async Task<OrganizacionDto> CrearAsync(SolicitudCrearOrganizacion s, CancellationToken ct)
    {
        var v = new Validador()
            .Requerido(nameof(s.Nombre), s.Nombre)
            .Requerido(nameof(s.ZonaHoraria), s.ZonaHoraria, 64)
            .Requerido(nameof(s.EmailAdministrador), s.EmailAdministrador, 254)
            .Requerido(nameof(s.NombreAdministrador), s.NombreAdministrador);
        PoliticaContrasena.Validar(v, nameof(s.ContrasenaAdministrador), s.ContrasenaAdministrador,
            s.EmailAdministrador, opciones.LongitudMinimaContrasena);
        v.LanzarSiHayErrores();

        var ahora = reloj.GetUtcNow();
        var email = Usuario.NormalizarEmail(s.EmailAdministrador);
        if (await db.Organizaciones.AnyAsync(o => o.Nombre == s.Nombre.Trim(), ct))
            throw new ExcepcionConflicto("Ya existe una organización con ese nombre.");
        if (await db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Email == email, ct))
            throw new ExcepcionConflicto("No se puede crear un usuario con ese correo electrónico.");

        var org = new Organizacion(s.Nombre, s.ZonaHoraria, ahora, s.MfaObligatorio);
        var admin = new Usuario(org.Id, email, s.NombreAdministrador, hasher.Hash(s.ContrasenaAdministrador),
            [Rol.AdministradorSociedad], ahora);
        db.Organizaciones.Add(org);
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync(ct);
        return ADto(org);
    }

    private static OrganizacionDto ADto(Organizacion o) => new(o.Id, o.Nombre, o.ZonaHoraria, o.Activa, o.MfaObligatorio);
}
