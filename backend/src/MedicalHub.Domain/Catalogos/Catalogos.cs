using MedicalHub.Domain.Comun;

namespace MedicalHub.Domain.Catalogos;

/// <summary>Elemento de catálogo propio de cada sociedad (las especialidades se añaden como datos, no como código).</summary>
public abstract class EntidadCatalogo : EntidadDeOrganizacion, IAuditable
{
    public string Nombre { get; private set; } = "";
    public string? Descripcion { get; private set; }
    public bool Activa { get; private set; } = true;
    public DateTimeOffset FechaCreacion { get; private set; }

    protected EntidadCatalogo() { }

    protected EntidadCatalogo(Guid organizacionId, string nombre, string? descripcion, DateTimeOffset ahora)
        : base(organizacionId)
    {
        Actualizar(nombre, descripcion);
        FechaCreacion = ahora;
    }

    public void Actualizar(string nombre, string? descripcion)
    {
        Nombre = Texto(nombre, "El nombre", 120);
        Descripcion = TextoOpcional(descripcion, "La descripción", 500);
    }

    public void Activar() => Activa = true;
    public void Desactivar() => Activa = false;
}

public interface ICreableCatalogo<TSelf> where TSelf : EntidadCatalogo
{
    static abstract TSelf Crear(Guid organizacionId, string nombre, string? descripcion, DateTimeOffset ahora);
}

public sealed class Especialidad : EntidadCatalogo, ICreableCatalogo<Especialidad>
{
    private Especialidad() { }
    private Especialidad(Guid org, string n, string? d, DateTimeOffset a) : base(org, n, d, a) { }
    public static Especialidad Crear(Guid org, string n, string? d, DateTimeOffset a) => new(org, n, d, a);
}

public sealed class CategoriaProfesional : EntidadCatalogo, ICreableCatalogo<CategoriaProfesional>
{
    private CategoriaProfesional() { }
    private CategoriaProfesional(Guid org, string n, string? d, DateTimeOffset a) : base(org, n, d, a) { }
    public static CategoriaProfesional Crear(Guid org, string n, string? d, DateTimeOffset a) => new(org, n, d, a);
}

/// <summary>Competencia o habilitación (p. ej. «Anestesia pediátrica», «Ecografía»).</summary>
public sealed class Competencia : EntidadCatalogo, ICreableCatalogo<Competencia>
{
    private Competencia() { }
    private Competencia(Guid org, string n, string? d, DateTimeOffset a) : base(org, n, d, a) { }
    public static Competencia Crear(Guid org, string n, string? d, DateTimeOffset a) => new(org, n, d, a);
}
