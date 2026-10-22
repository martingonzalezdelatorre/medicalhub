namespace MedicalHub.Domain.Comun;

/// <summary>Base de toda entidad operativa de una sociedad. Su organización no cambia nunca tras la creación.</summary>
public abstract class EntidadDeOrganizacion : Entidad, IPerteneceAOrganizacion
{
    public Guid? OrganizacionId { get; private set; }

    protected EntidadDeOrganizacion() { }

    protected EntidadDeOrganizacion(Guid organizacionId)
    {
        if (organizacionId == Guid.Empty) throw new ExcepcionDominio("La organización es obligatoria.");
        OrganizacionId = organizacionId;
    }

    protected static string Texto(string? valor, string campo, int max, bool obligatorio = true)
    {
        var v = valor?.Trim() ?? "";
        if (obligatorio && v.Length == 0) throw new ExcepcionDominio($"{campo} es obligatorio.");
        if (v.Length > max) throw new ExcepcionDominio($"{campo} no puede superar {max} caracteres.");
        return v;
    }

    protected static string? TextoOpcional(string? valor, string campo, int max)
    {
        var v = Texto(valor, campo, max, obligatorio: false);
        return v.Length == 0 ? null : v;
    }
}
