using MedicalHub.Domain.Catalogos;
using MedicalHub.Domain.Estructura;
using MedicalHub.Domain.Identidad;
using MedicalHub.Domain.Profesionales;
using MedicalHub.Domain.Organizaciones;
using MedicalHub.Domain.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Comun;

/// <summary>Acceso a datos. Lo implementa el DbContext de Infrastructure (decisión: Application referencia EF Core).</summary>
public interface IAppDbContext
{
    DbSet<Organizacion> Organizaciones { get; }
    DbSet<Usuario> Usuarios { get; }
    DbSet<TokenRefresco> TokensRefresco { get; }
    DbSet<EventoSeguridad> EventosSeguridad { get; }
    DbSet<RegistroAuditoria> RegistrosAuditoria { get; }
    DbSet<Especialidad> Especialidades { get; }
    DbSet<CategoriaProfesional> CategoriasProfesionales { get; }
    DbSet<Competencia> Competencias { get; }
    DbSet<Centro> Centros { get; }
    DbSet<Servicio> Servicios { get; }
    DbSet<TipoTurno> TiposTurno { get; }
    DbSet<RequisitoCobertura> RequisitosCobertura { get; }
    DbSet<Profesional> Profesionales { get; }
    DbSet<T> Set<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>Usuario y organización de la petición actual, tomados SIEMPRE del token validado, nunca del cliente.</summary>
public interface IContextoActual
{
    bool EstaAutenticado { get; }
    Guid? UsuarioId { get; }
    Guid? OrganizacionId { get; }
    IReadOnlySet<Rol> Roles { get; }
    bool MfaPendiente { get; }
    string? Ip { get; }
    string? IdCorrelacion { get; }
    bool TieneRol(Rol rol) => Roles.Contains(rol);
}

public interface IHasherContrasena
{
    string Hash(string contrasena);
    bool Verificar(string hash, string contrasena);
}

public sealed record TokenAcceso(string Token, DateTimeOffset Expira);

public interface IEmisorTokens
{
    TokenAcceso CrearTokenAcceso(Usuario usuario, bool mfaPendiente, DateTimeOffset ahora);
    /// <summary>Genera un token de refresco opaco y su hash (solo el hash se persiste).</summary>
    (string Token, string Hash) GenerarTokenRefresco();
    string HashearTokenRefresco(string token);
}

public interface IServicioTotp
{
    string GenerarSecreto();
    string ConstruirUri(string emisor, string cuenta, string secreto);
    /// <summary>Valida el código admitiendo ±1 paso y rechazando pasos ya usados. Devuelve el paso aceptado.</summary>
    bool Validar(string secreto, string codigo, DateTimeOffset ahora, long ultimoPasoUsado, out long paso);
}

public interface IProtectorSecretos
{
    string Proteger(string texto);
    string Desproteger(string protegido);
}

public sealed class OpcionesSeguridad
{
    public const string Seccion = "Seguridad";

    public int MaxIntentosFallidos { get; set; } = 5;
    public int MinutosBloqueo { get; set; } = 15;
    public int MinutosTokenAcceso { get; set; } = 15;
    public int DiasTokenRefresco { get; set; } = 7;
    public int LongitudMinimaContrasena { get; set; } = 12;
    public int LimiteLoginPorMinuto { get; set; } = 10;
    public string Emisor { get; set; } = "MedicalHub";
    public string Audiencia { get; set; } = "MedicalHub.Clientes";
    /// <summary>Clave HMAC (≥ 32 bytes) en variable de entorno Seguridad__ClaveJwt. En Desarrollo, si falta, se genera una efímera.</summary>
    public string? ClaveJwt { get; set; }
}
