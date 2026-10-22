namespace MedicalHub.Domain.Identidad;

public enum Rol
{
    AdministradorPlataforma = 1,
    AdministradorSociedad = 2,
    CoordinadorMedico = 3,
    ProfesionalSanitario = 4,
    ResponsableCentro = 5
}

public static class RolExtensiones
{
    /// <summary>Roles con capacidad de gestión para los que se exige MFA.</summary>
    public static bool ExigeMfa(this Rol rol)
        => rol is Rol.AdministradorPlataforma or Rol.AdministradorSociedad or Rol.CoordinadorMedico;
}
