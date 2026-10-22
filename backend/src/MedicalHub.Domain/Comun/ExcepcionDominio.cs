namespace MedicalHub.Domain.Comun;

/// <summary>Se lanza cuando se viola una invariante del dominio.</summary>
public sealed class ExcepcionDominio(string mensaje) : Exception(mensaje);
