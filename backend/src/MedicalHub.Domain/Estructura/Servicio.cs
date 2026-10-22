using System.Text.Json;
using MedicalHub.Domain.Comun;

namespace MedicalHub.Domain.Estructura;

public sealed class ServicioCategoria
{
    private ServicioCategoria() { }
    public ServicioCategoria(Guid categoriaId) => CategoriaProfesionalId = categoriaId;
    public Guid CategoriaProfesionalId { get; private set; }
}

public sealed class ServicioCompetencia
{
    private ServicioCompetencia() { }
    public ServicioCompetencia(Guid competenciaId) => CompetenciaId = competenciaId;
    public Guid CompetenciaId { get; private set; }
}

/// <summary>Servicio asistencial de un centro (quirófano, urgencias, UCI…). Cada servicio define sus propios tipos de turno.</summary>
public sealed class Servicio : EntidadDeOrganizacion, IAuditable
{
    private readonly List<ServicioCategoria> _categorias = [];
    private readonly List<ServicioCompetencia> _competencias = [];

    public Guid CentroId { get; private set; }
    public string Nombre { get; private set; } = "";
    public string? Descripcion { get; private set; }
    /// <summary>Usuario coordinador responsable del servicio (opcional).</summary>
    public Guid? CoordinadorUsuarioId { get; private set; }
    /// <summary>Parámetros de reglas específicas del servicio (JSON); los interpreta el motor de reglas (E4).</summary>
    public string? ConfiguracionReglas { get; private set; }
    public bool Activo { get; private set; } = true;
    public DateTimeOffset FechaCreacion { get; private set; }

    /// <summary>Categorías profesionales admitidas. Vacío = cualquiera.</summary>
    public IReadOnlyCollection<ServicioCategoria> CategoriasAdmitidas => _categorias;
    public IReadOnlyCollection<ServicioCompetencia> CompetenciasRequeridas => _competencias;

    private Servicio() { }

    public Servicio(Guid organizacionId, Guid centroId, string nombre, string? descripcion, DateTimeOffset ahora)
        : base(organizacionId)
    {
        if (centroId == Guid.Empty) throw new ExcepcionDominio("El centro es obligatorio.");
        CentroId = centroId;
        Actualizar(nombre, descripcion);
        FechaCreacion = ahora;
    }

    public void Actualizar(string nombre, string? descripcion)
    {
        Nombre = Texto(nombre, "El nombre", 200);
        Descripcion = TextoOpcional(descripcion, "La descripción", 1000);
    }

    public void EstablecerCoordinador(Guid? usuarioId) => CoordinadorUsuarioId = usuarioId;

    public void EstablecerConfiguracionReglas(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) { ConfiguracionReglas = null; return; }
        if (json.Length > 4000) throw new ExcepcionDominio("La configuración de reglas es demasiado extensa.");
        try { JsonDocument.Parse(json).Dispose(); }
        catch (JsonException) { throw new ExcepcionDominio("La configuración de reglas debe ser un JSON válido."); }
        ConfiguracionReglas = json.Trim();
    }

    public void EstablecerCategoriasAdmitidas(IEnumerable<Guid> ids)
    {
        var nuevos = ids.Distinct().ToList();
        _categorias.RemoveAll(c => !nuevos.Contains(c.CategoriaProfesionalId));
        foreach (var id in nuevos.Where(n => _categorias.All(c => c.CategoriaProfesionalId != n)))
            _categorias.Add(new ServicioCategoria(id));
    }

    public void EstablecerCompetenciasRequeridas(IEnumerable<Guid> ids)
    {
        var nuevos = ids.Distinct().ToList();
        _competencias.RemoveAll(c => !nuevos.Contains(c.CompetenciaId));
        foreach (var id in nuevos.Where(n => _competencias.All(c => c.CompetenciaId != n)))
            _competencias.Add(new ServicioCompetencia(id));
    }

    public void Activar() => Activo = true;
    public void Desactivar() => Activo = false;
}

/// <summary>Franja horaria tipo de un servicio (p. ej. «Mañana 08:00–15:00», «Guardia 24 h»). Horas en hora local del centro.</summary>
public sealed class TipoTurno : EntidadDeOrganizacion, IAuditable
{
    public Guid ServicioId { get; private set; }
    public string Nombre { get; private set; } = "";
    public TimeOnly HoraInicio { get; private set; }
    public TimeOnly HoraFin { get; private set; }
    public bool EsGuardia { get; private set; }
    public bool Activo { get; private set; } = true;

    /// <summary>Fin igual o anterior al inicio ⇒ termina al día siguiente (inicio = fin equivale a 24 h).</summary>
    public bool CruzaMedianoche => HoraFin <= HoraInicio;

    public TimeSpan Duracion => CruzaMedianoche
        ? TimeSpan.FromHours(24) - (HoraInicio - HoraFin)
        : HoraFin - HoraInicio;

    private TipoTurno() { }

    public TipoTurno(Guid organizacionId, Guid servicioId, string nombre, TimeOnly inicio, TimeOnly fin, bool esGuardia)
        : base(organizacionId)
    {
        ServicioId = servicioId;
        Actualizar(nombre, inicio, fin, esGuardia);
    }

    public void Actualizar(string nombre, TimeOnly inicio, TimeOnly fin, bool esGuardia)
    {
        Nombre = Texto(nombre, "El nombre", 100);
        HoraInicio = inicio;
        HoraFin = fin;
        EsGuardia = esGuardia;
    }

    public void Activar() => Activo = true;
    public void Desactivar() => Activo = false;
}

/// <summary>Cuántos profesionales (y de qué categoría) exige un tipo de turno. Es la «necesidad de cobertura» base.</summary>
public sealed class RequisitoCobertura : EntidadDeOrganizacion, IAuditable
{
    public Guid ServicioId { get; private set; }
    public Guid TipoTurnoId { get; private set; }
    public int NumeroProfesionales { get; private set; }
    /// <summary>null = cualquier categoría admitida por el servicio.</summary>
    public Guid? CategoriaProfesionalId { get; private set; }

    private RequisitoCobertura() { }

    public RequisitoCobertura(Guid organizacionId, Guid servicioId, Guid tipoTurnoId, int numero, Guid? categoriaId)
        : base(organizacionId)
    {
        if (numero is < 1 or > 50) throw new ExcepcionDominio("El número de profesionales debe estar entre 1 y 50.");
        ServicioId = servicioId;
        TipoTurnoId = tipoTurnoId;
        NumeroProfesionales = numero;
        CategoriaProfesionalId = categoriaId;
    }

    public void CambiarNumero(int numero)
    {
        if (numero is < 1 or > 50) throw new ExcepcionDominio("El número de profesionales debe estar entre 1 y 50.");
        NumeroProfesionales = numero;
    }
}
