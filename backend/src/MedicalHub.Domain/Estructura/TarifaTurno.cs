using MedicalHub.Domain.Comun;

namespace MedicalHub.Domain.Estructura;

/// <summary>Coste de un turno para un perfil profesional (p. ej. «Guardia 24 h» · «Médico anestesista» = 600 €).</summary>
public sealed class TarifaTurno : EntidadDeOrganizacion, IAuditable
{
    public const decimal ImporteMaximo = 1_000_000m;

    public Guid TipoTurnoId { get; private set; }
    public Guid CategoriaProfesionalId { get; private set; }
    /// <summary>Importe por turno, en euros.</summary>
    public decimal Importe { get; private set; }

    private TarifaTurno() { }

    public TarifaTurno(Guid organizacionId, Guid tipoTurnoId, Guid categoriaId, decimal importe) : base(organizacionId)
    {
        if (tipoTurnoId == Guid.Empty) throw new ExcepcionDominio("El tipo de turno es obligatorio.");
        if (categoriaId == Guid.Empty) throw new ExcepcionDominio("El perfil profesional es obligatorio.");
        TipoTurnoId = tipoTurnoId;
        CategoriaProfesionalId = categoriaId;
        CambiarImporte(importe);
    }

    public void CambiarImporte(decimal importe)
    {
        if (importe < 0 || importe > ImporteMaximo) throw new ExcepcionDominio($"El importe debe estar entre 0 y {ImporteMaximo:N0}.");
        Importe = decimal.Round(importe, 2);
    }
}
