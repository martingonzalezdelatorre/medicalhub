namespace MedicalHub.Application.Comun;

public sealed class ExcepcionNoEncontrado(string mensaje) : Exception(mensaje);

public sealed class ExcepcionValidacion(IReadOnlyDictionary<string, string[]> errores)
    : Exception("Se han producido errores de validación.")
{
    public IReadOnlyDictionary<string, string[]> Errores { get; } = errores;
}

public sealed class ExcepcionConflicto(string mensaje) : Exception(mensaje);

/// <summary>Credenciales o sesión no válidas (401). <c>Codigo</c> permite al cliente reaccionar (p. ej. mfa_requerido).</summary>
public sealed class ExcepcionNoAutenticado(string mensaje, string? codigo = null) : Exception(mensaje)
{
    public string? Codigo { get; } = codigo;
}

/// <summary>Usuario autenticado sin permiso para la operación (403).</summary>
public sealed class ExcepcionProhibido(string mensaje) : Exception(mensaje);

/// <summary>Acumula errores de validación por campo y los lanza juntos.</summary>
public sealed class Validador
{
    private readonly Dictionary<string, List<string>> _errores = [];

    public Validador Requerido(string campo, string? valor, int maxLongitud = 200)
    {
        if (string.IsNullOrWhiteSpace(valor)) Agregar(campo, "Es obligatorio.");
        else if (valor.Length > maxLongitud) Agregar(campo, $"No puede superar {maxLongitud} caracteres.");
        return this;
    }

    public Validador Cuando(bool condicion, string campo, string mensaje)
    {
        if (condicion) Agregar(campo, mensaje);
        return this;
    }

    public void Agregar(string campo, string mensaje)
    {
        if (!_errores.TryGetValue(campo, out var lista)) _errores[campo] = lista = [];
        lista.Add(mensaje);
    }

    public void LanzarSiHayErrores()
    {
        if (_errores.Count > 0)
            throw new ExcepcionValidacion(_errores.ToDictionary(k => k.Key, k => k.Value.ToArray()));
    }
}
