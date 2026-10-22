using MedicalHub.Application.Comun;
using MedicalHub.Domain.Identidad;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Identidad;

/// <summary>
/// Crea el primer administrador de plataforma a partir de configuración externa (variables de entorno)
/// si todavía no existe ninguno. No hay credenciales predeterminadas.
/// </summary>
public sealed class InicializadorPlataforma(IAppDbContext db, IHasherContrasena hasher, TimeProvider reloj, OpcionesSeguridad opciones)
{
    public async Task<bool> AsegurarAdministradorAsync(string? email, string? nombre, string? contrasena, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(contrasena)) return false;
        if (await db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Roles.Any(r => r.Rol == Rol.AdministradorPlataforma), ct))
            return false;

        var v = new Validador();
        PoliticaContrasena.Validar(v, "Contrasena", contrasena, email, opciones.LongitudMinimaContrasena);
        v.LanzarSiHayErrores();

        db.Usuarios.Add(new Usuario(null, email, string.IsNullOrWhiteSpace(nombre) ? "Administrador de plataforma" : nombre,
            hasher.Hash(contrasena), [Rol.AdministradorPlataforma], reloj.GetUtcNow()));
        await db.SaveChangesAsync(ct);
        return true;
    }
}
