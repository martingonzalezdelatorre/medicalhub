using MedicalHub.Domain.Identidad;

namespace MedicalHub.Application.Identidad;

public sealed record SolicitudLogin(string Email, string Contrasena, string? CodigoMfa);
public sealed record SolicitudRefresco(string TokenRefresco);
public sealed record SolicitudCambioContrasena(string ContrasenaActual, string ContrasenaNueva);
public sealed record SolicitudActivarMfa(string Codigo);

public sealed record PerfilUsuario(
    Guid Id, Guid? OrganizacionId, string? Organizacion, string Email, string NombreCompleto,
    IReadOnlyList<Rol> Roles, bool MfaActivado, bool MfaPendiente, string? ZonaHoraria);

public sealed record RespuestaSesion(
    string TokenAcceso, DateTimeOffset AccesoExpira,
    string TokenRefresco, DateTimeOffset RefrescoExpira,
    bool MfaPendiente, PerfilUsuario Perfil);

public sealed record ConfiguracionMfa(string Secreto, string UriOtpAuth);

public sealed record SolicitudCrearUsuario(string Email, string NombreCompleto, string Contrasena, IReadOnlyList<Rol> Roles);
public sealed record SolicitudActualizarUsuario(string NombreCompleto, IReadOnlyList<Rol> Roles);

public sealed record UsuarioDto(
    Guid Id, string Email, string NombreCompleto, IReadOnlyList<Rol> Roles, bool Activo, bool MfaActivado);

public sealed record SolicitudCrearOrganizacion(
    string Nombre, string ZonaHoraria, bool MfaObligatorio,
    string EmailAdministrador, string NombreAdministrador, string ContrasenaAdministrador);

public sealed record OrganizacionDto(Guid Id, string Nombre, string ZonaHoraria, bool Activa, bool MfaObligatorio);
