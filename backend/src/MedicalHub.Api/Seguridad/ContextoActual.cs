using System.Security.Claims;
using MedicalHub.Api.Middleware;
using MedicalHub.Application.Comun;
using MedicalHub.Domain.Identidad;

namespace MedicalHub.Api.Seguridad;

/// <summary>Identidad de la petición. Solo lee claims de un token ya validado (firma, emisor, audiencia y caducidad).</summary>
public sealed class ContextoActual(IHttpContextAccessor http) : IContextoActual
{
    private ClaimsPrincipal? Usuario => http.HttpContext?.User;

    public bool EstaAutenticado => Usuario?.Identity?.IsAuthenticated == true;
    public Guid? UsuarioId => Guid.TryParse(Usuario?.FindFirstValue("sub"), out var id) ? id : null;
    public Guid? OrganizacionId => Guid.TryParse(Usuario?.FindFirstValue("org"), out var id) ? id : null;
    public bool MfaPendiente => Usuario?.HasClaim("mfa_pendiente", "true") == true;
    public string? Ip => http.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? IdCorrelacion => http.HttpContext?.Items[IdCorrelacionMiddleware.ClaveItem] as string;

    public IReadOnlySet<Rol> Roles => Usuario?.FindAll("role")
        .Select(c => Enum.TryParse<Rol>(c.Value, out var r) ? (Rol?)r : null)
        .Where(r => r is not null).Select(r => r!.Value).ToHashSet() ?? [];
}

public static class Politicas
{
    /// <summary>Permite sesiones con MFA pendiente (alta de MFA, perfil, cierre de sesión).</summary>
    public const string SesionIncluidaMfaPendiente = "SesionIncluidaMfaPendiente";
    public const string LimiteLogin = "login";

    // Cada política de rol incluye la exigencia de sesión completa (sin MFA pendiente).
    public const string AdministradorPlataforma = nameof(AdministradorPlataforma);
    public const string AdministradorSociedad = nameof(AdministradorSociedad);
    /// <summary>Administrador, coordinador o responsable de centro (lectura de la planificación y fichas).</summary>
    public const string LecturaSociedad = nameof(LecturaSociedad);
    /// <summary>Administrador de sociedad o coordinador médico (elaboración de cuadrantes y búsqueda de candidatos).</summary>
    public const string GestionPlanificacion = nameof(GestionPlanificacion);
    /// <summary>Administrador de sociedad o RRHH y Administración (perfiles, tipos de turno y tarifas).</summary>
    public const string GestionRecursosHumanos = nameof(GestionRecursosHumanos);
    /// <summary>Lectura de centros y servicios: los roles de lectura más RRHH y Administración.</summary>
    public const string LecturaEstructura = nameof(LecturaEstructura);
    /// <summary>Cualquier usuario de una sociedad con sesión completa.</summary>
    public const string UsuarioSociedad = nameof(UsuarioSociedad);
}
