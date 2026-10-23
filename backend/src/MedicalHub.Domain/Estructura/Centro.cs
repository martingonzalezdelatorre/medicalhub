using MedicalHub.Domain.Comun;
using MedicalHub.Domain.Organizaciones;

namespace MedicalHub.Domain.Estructura;

public enum TipoCentro { Hospital = 1, Clinica = 2, CentroAmbulatorio = 3, Otro = 4 }

/// <summary>Usuario (rol ResponsableCentro) con alcance sobre el centro. Tipo poseído.</summary>
public sealed class CentroResponsable
{
    private CentroResponsable() { }
    public CentroResponsable(Guid usuarioId) => UsuarioId = usuarioId;
    public Guid UsuarioId { get; private set; }
}

public sealed class Centro : EntidadDeOrganizacion, IAuditable
{
    private readonly List<CentroResponsable> _responsables = [];

    public string Nombre { get; private set; } = "";
    public TipoCentro Tipo { get; private set; }
    public string? Direccion { get; private set; }
    /// <summary>Zona horaria IANA del centro (los turnos se definen en hora local del centro).</summary>
    public string ZonaHoraria { get; private set; } = "";
    public bool Activo { get; private set; } = true;
    public DateTimeOffset FechaCreacion { get; private set; }

    /// <summary>Geovalla del centro (círculo) para validar la ubicación de los fichajes. Opcional.</summary>
    public double? Latitud { get; private set; }
    public double? Longitud { get; private set; }
    public int? RadioMetros { get; private set; }
    public bool TieneGeovalla => Latitud is not null && Longitud is not null && RadioMetros is not null;

    public IReadOnlyCollection<CentroResponsable> Responsables => _responsables;

    private Centro() { }

    public Centro(Guid organizacionId, string nombre, TipoCentro tipo, string? direccion, string zonaHoraria, DateTimeOffset ahora)
        : base(organizacionId)
    {
        Actualizar(nombre, tipo, direccion, zonaHoraria);
        FechaCreacion = ahora;
    }

    public void Actualizar(string nombre, TipoCentro tipo, string? direccion, string zonaHoraria)
    {
        if (!Enum.IsDefined(tipo)) throw new ExcepcionDominio("El tipo de centro no es válido.");
        Nombre = Texto(nombre, "El nombre", 200);
        Tipo = tipo;
        Direccion = TextoOpcional(direccion, "La dirección", 300);
        ZonaHoraria = Organizacion.ValidarZonaHoraria(zonaHoraria);
    }

    public void EstablecerResponsables(IEnumerable<Guid> usuarios)
    {
        var nuevos = usuarios.Distinct().ToList();
        _responsables.RemoveAll(r => !nuevos.Contains(r.UsuarioId));
        foreach (var id in nuevos.Where(n => _responsables.All(r => r.UsuarioId != n)))
            _responsables.Add(new CentroResponsable(id));
    }

    public void EstablecerGeovalla(double latitud, double longitud, int radioMetros)
    {
        if (double.IsNaN(latitud) || latitud is < -90 or > 90) throw new ExcepcionDominio("La latitud debe estar entre -90 y 90.");
        if (double.IsNaN(longitud) || longitud is < -180 or > 180) throw new ExcepcionDominio("La longitud debe estar entre -180 y 180.");
        if (radioMetros is < Horas.CalculadoraGeovalla.MinRadioMetros or > Horas.CalculadoraGeovalla.MaxRadioMetros)
            throw new ExcepcionDominio($"El radio debe estar entre {Horas.CalculadoraGeovalla.MinRadioMetros} y {Horas.CalculadoraGeovalla.MaxRadioMetros} metros.");
        Latitud = latitud;
        Longitud = longitud;
        RadioMetros = radioMetros;
    }

    public void QuitarGeovalla() => (Latitud, Longitud, RadioMetros) = (null, null, null);

    public void Activar() => Activo = true;
    public void Desactivar() => Activo = false;
}
