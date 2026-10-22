using MedicalHub.Application.Comun;

namespace MedicalHub.Application.Identidad;

public static class PoliticaContrasena
{
    private const int MaxLongitud = 128;

    public static void Validar(Validador v, string campo, string? contrasena, string? email, int longitudMinima)
    {
        if (string.IsNullOrEmpty(contrasena)) { v.Agregar(campo, "Es obligatoria."); return; }
        if (contrasena.Length < longitudMinima) v.Agregar(campo, $"Debe tener al menos {longitudMinima} caracteres.");
        if (contrasena.Length > MaxLongitud) v.Agregar(campo, $"No puede superar {MaxLongitud} caracteres.");
        if (!string.IsNullOrEmpty(email) && contrasena.Contains(email.Split('@')[0], StringComparison.OrdinalIgnoreCase)
            && email.Split('@')[0].Length >= 4)
            v.Agregar(campo, "No puede contener su nombre de usuario.");
        if (contrasena.Distinct().Count() < 5) v.Agregar(campo, "Es demasiado simple.");
    }
}
