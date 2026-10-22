using MedicalHub.Domain.Comun;

namespace MedicalHub.Domain.Organizaciones;

/// <summary>Sociedad médica cliente del SaaS (inquilino).</summary>
public sealed class Organizacion : Entidad, IAuditable
{
    public string Nombre { get; private set; }
    public string ZonaHoraria { get; private set; }
    public bool Activa { get; private set; } = true;
    /// <summary>Si es true, administradores y coordinadores de la sociedad deben usar MFA.</summary>
    public bool MfaObligatorio { get; private set; } = true;
    public DateTimeOffset FechaCreacion { get; private set; }

    private Organizacion() { Nombre = ""; ZonaHoraria = ""; }

    public Organizacion(string nombre, string zonaHoraria, DateTimeOffset ahora, bool mfaObligatorio = true)
    {
        Nombre = ValidarNombre(nombre);
        ZonaHoraria = ValidarZonaHoraria(zonaHoraria);
        FechaCreacion = ahora;
        MfaObligatorio = mfaObligatorio;
    }

    public void Desactivar() => Activa = false;

    private static string ValidarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ExcepcionDominio("El nombre de la organización es obligatorio.");
        return nombre.Trim();
    }

    /// <summary>Valida un identificador de zona horaria IANA (p. ej. Europe/Madrid).</summary>
    public static string ValidarZonaHoraria(string zona)
    {
        if (string.IsNullOrWhiteSpace(zona) || !TimeZoneInfo.TryFindSystemTimeZoneById(zona.Trim(), out _))
            throw new ExcepcionDominio($"La zona horaria '{zona}' no es un identificador IANA válido.");
        return zona.Trim();
    }
}
