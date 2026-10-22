namespace MedicalHub.Domain.Comun;

public abstract class Entidad
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();

    /// <summary>Token de concurrencia optimista (rowversion).</summary>
    public byte[] VersionFila { get; protected set; } = [];
}
