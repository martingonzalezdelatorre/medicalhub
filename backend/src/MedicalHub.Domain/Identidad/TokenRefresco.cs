using MedicalHub.Domain.Comun;

namespace MedicalHub.Domain.Identidad;

/// <summary>
/// Token de refresco rotatorio. Solo se guarda el hash. Los tokens de una misma sesión comparten
/// FamiliaId: si se reutiliza uno ya revocado, se revoca toda la familia.
/// </summary>
public sealed class TokenRefresco : Entidad
{
    public Guid UsuarioId { get; private set; }
    public Guid FamiliaId { get; private set; }
    public string HashToken { get; private set; }
    public DateTimeOffset FechaCreacion { get; private set; }
    public DateTimeOffset Expira { get; private set; }
    public DateTimeOffset? RevocadoEn { get; private set; }

    private TokenRefresco() { HashToken = ""; }

    public TokenRefresco(Guid usuarioId, Guid familiaId, string hashToken, DateTimeOffset ahora, TimeSpan duracion)
    {
        UsuarioId = usuarioId;
        FamiliaId = familiaId;
        HashToken = hashToken;
        FechaCreacion = ahora;
        Expira = ahora + duracion;
    }

    public bool EstaRevocado => RevocadoEn is not null;
    public bool HaExpirado(DateTimeOffset ahora) => Expira <= ahora;

    public void Revocar(DateTimeOffset ahora) => RevocadoEn ??= ahora;
}
