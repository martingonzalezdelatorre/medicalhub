namespace MedicalHub.Application.Comun;

public sealed class ExcepcionNoEncontrado(string mensaje) : Exception(mensaje);

public sealed class ExcepcionValidacion(IReadOnlyDictionary<string, string[]> errores)
    : Exception("Se han producido errores de validación.")
{
    public IReadOnlyDictionary<string, string[]> Errores { get; } = errores;
}

public sealed class ExcepcionConflicto(string mensaje) : Exception(mensaje);
