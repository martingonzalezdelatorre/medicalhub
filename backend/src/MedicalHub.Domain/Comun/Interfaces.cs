namespace MedicalHub.Domain.Comun;

/// <summary>
/// Entidad ligada a una sociedad médica. Toda entidad que implemente esta interfaz
/// DEBE tener un filtro global de consulta por organización en la capa de persistencia
/// (hay una prueba automática que lo comprueba).
/// Es null solo para usuarios de plataforma.
/// </summary>
public interface IPerteneceAOrganizacion
{
    Guid? OrganizacionId { get; }
}

/// <summary>Entidad cuyos cambios se registran en la auditoría operativa.</summary>
public interface IAuditable;
