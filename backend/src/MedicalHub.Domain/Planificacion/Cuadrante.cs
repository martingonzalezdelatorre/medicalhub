using MedicalHub.Domain.Comun;

namespace MedicalHub.Domain.Planificacion;

public enum EstadoCuadrante { Borrador = 1, Publicado = 2, Archivado = 3 }

/// <summary>Planificación de un servicio para un periodo. Solo se edita en borrador; al publicarse se guarda una versión.</summary>
public sealed class Cuadrante : EntidadDeOrganizacion, IAuditable
{
    public const int MaxDiasPeriodo = 93;

    public Guid ServicioId { get; private set; }
    public string Nombre { get; private set; } = "";
    public DateOnly Desde { get; private set; }
    public DateOnly Hasta { get; private set; }
    public EstadoCuadrante Estado { get; private set; } = EstadoCuadrante.Borrador;
    /// <summary>Número de la última versión publicada (0 = nunca publicado).</summary>
    public int Version { get; private set; }
    public DateTimeOffset? FechaPublicacion { get; private set; }
    public Guid? PublicadoPorUsuarioId { get; private set; }
    public DateTimeOffset FechaCreacion { get; private set; }

    private Cuadrante() { }

    public Cuadrante(Guid organizacionId, Guid servicioId, string nombre, DateOnly desde, DateOnly hasta, DateTimeOffset ahora)
        : base(organizacionId)
    {
        if (servicioId == Guid.Empty) throw new ExcepcionDominio("El servicio es obligatorio.");
        if (hasta < desde) throw new ExcepcionDominio("La fecha final no puede ser anterior a la inicial.");
        if (hasta.DayNumber - desde.DayNumber + 1 > MaxDiasPeriodo)
            throw new ExcepcionDominio($"Un cuadrante no puede abarcar más de {MaxDiasPeriodo} días.");
        ServicioId = servicioId;
        Nombre = Texto(nombre, "El nombre", 150);
        Desde = desde;
        Hasta = hasta;
        FechaCreacion = ahora;
    }

    public bool EsEditable => Estado == EstadoCuadrante.Borrador;
    public bool Contiene(DateOnly fecha) => fecha >= Desde && fecha <= Hasta;
    public int Dias => Hasta.DayNumber - Desde.DayNumber + 1;

    public void Renombrar(string nombre) => Nombre = Texto(nombre, "El nombre", 150);

    public void Publicar(Guid usuarioId, DateTimeOffset ahora)
    {
        if (Estado != EstadoCuadrante.Borrador) throw new ExcepcionDominio("Solo se puede publicar un cuadrante en borrador.");
        Version++;
        Estado = EstadoCuadrante.Publicado;
        FechaPublicacion = ahora;
        PublicadoPorUsuarioId = usuarioId;
    }

    public void Reabrir()
    {
        if (Estado != EstadoCuadrante.Publicado) throw new ExcepcionDominio("Solo se puede reabrir un cuadrante publicado.");
        Estado = EstadoCuadrante.Borrador;
    }

    public void Archivar()
    {
        if (Estado != EstadoCuadrante.Publicado) throw new ExcepcionDominio("Solo se puede archivar un cuadrante publicado.");
        Estado = EstadoCuadrante.Archivado;
    }
}

/// <summary>Instancia de un tipo de turno en un día concreto de un cuadrante. Puede existir sin ningún profesional asignado.</summary>
public sealed class Turno : EntidadDeOrganizacion, IAuditable
{
    public Guid CuadranteId { get; private set; }
    public Guid ServicioId { get; private set; }
    public Guid TipoTurnoId { get; private set; }
    /// <summary>Día local (en la zona del centro) en que empieza el turno.</summary>
    public DateOnly Fecha { get; private set; }
    public DateTimeOffset InicioUtc { get; private set; }
    public DateTimeOffset FinUtc { get; private set; }

    public double DuracionHoras => (FinUtc - InicioUtc).TotalHours;

    private Turno() { }

    public Turno(Guid organizacionId, Guid cuadranteId, Guid servicioId, Guid tipoTurnoId, DateOnly fecha,
        DateTimeOffset inicioUtc, DateTimeOffset finUtc) : base(organizacionId)
    {
        if (finUtc <= inicioUtc) throw new ExcepcionDominio("El fin del turno debe ser posterior al inicio.");
        CuadranteId = cuadranteId;
        ServicioId = servicioId;
        TipoTurnoId = tipoTurnoId;
        Fecha = fecha;
        InicioUtc = inicioUtc;
        FinUtc = finUtc;
    }
}

/// <summary>Cuántos profesionales (y de qué categoría) necesita un turno. Un turno puede tener varias necesidades.</summary>
public sealed class NecesidadCobertura : EntidadDeOrganizacion, IAuditable
{
    public Guid TurnoId { get; private set; }
    public int NumeroPlazas { get; private set; }
    /// <summary>null = cualquier categoría admitida por el servicio.</summary>
    public Guid? CategoriaProfesionalId { get; private set; }

    private NecesidadCobertura() { }

    public NecesidadCobertura(Guid organizacionId, Guid turnoId, int plazas, Guid? categoriaId) : base(organizacionId)
    {
        TurnoId = turnoId;
        CambiarPlazas(plazas);
        CategoriaProfesionalId = categoriaId;
    }

    public void CambiarPlazas(int plazas)
    {
        if (plazas is < 1 or > 50) throw new ExcepcionDominio("El número de plazas debe estar entre 1 y 50.");
        NumeroPlazas = plazas;
    }
}

public enum EstadoAsignacion { Confirmada = 1, PendienteAprobacion = 2, Cancelada = 3 }

/// <summary>Un profesional concreto ocupando una plaza de una necesidad. Se conservan las canceladas como historial.</summary>
public sealed class Asignacion : EntidadDeOrganizacion, IAuditable
{
    public Guid NecesidadId { get; private set; }
    public Guid TurnoId { get; private set; }
    public Guid ProfesionalId { get; private set; }
    public EstadoAsignacion Estado { get; private set; } = EstadoAsignacion.Confirmada;
    /// <summary>Copia del horario del turno: permite detectar solapamientos sin cargar el turno.</summary>
    public DateTimeOffset InicioUtc { get; private set; }
    public DateTimeOffset FinUtc { get; private set; }
    public DateTimeOffset FechaAsignacion { get; private set; }
    public Guid? AsignadoPorUsuarioId { get; private set; }
    public DateTimeOffset? FechaCancelacion { get; private set; }

    public bool EstaActiva => Estado != EstadoAsignacion.Cancelada;

    private Asignacion() { }

    public Asignacion(Guid organizacionId, Guid necesidadId, Turno turno, Guid profesionalId, Guid? usuarioId, DateTimeOffset ahora)
        : base(organizacionId)
    {
        NecesidadId = necesidadId;
        TurnoId = turno.Id;
        ProfesionalId = profesionalId;
        InicioUtc = turno.InicioUtc;
        FinUtc = turno.FinUtc;
        AsignadoPorUsuarioId = usuarioId;
        FechaAsignacion = ahora;
    }

    public void Cancelar(DateTimeOffset ahora)
    {
        if (Estado == EstadoAsignacion.Cancelada) return;
        Estado = EstadoAsignacion.Cancelada;
        FechaCancelacion = ahora;
    }
}

/// <summary>Instantánea inmutable de un cuadrante en el momento de publicarlo.</summary>
public sealed class VersionCuadrante : EntidadDeOrganizacion
{
    public Guid CuadranteId { get; private set; }
    public int Numero { get; private set; }
    public DateTimeOffset FechaPublicacion { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public int TotalTurnos { get; private set; }
    public int PlazasTotales { get; private set; }
    public int PlazasCubiertas { get; private set; }
    public string ContenidoJson { get; private set; } = "{}";

    private VersionCuadrante() { }

    public VersionCuadrante(Guid organizacionId, Guid cuadranteId, int numero, DateTimeOffset fecha, Guid? usuarioId,
        int turnos, int plazas, int cubiertas, string contenidoJson) : base(organizacionId)
    {
        CuadranteId = cuadranteId;
        Numero = numero;
        FechaPublicacion = fecha;
        UsuarioId = usuarioId;
        TotalTurnos = turnos;
        PlazasTotales = plazas;
        PlazasCubiertas = cubiertas;
        ContenidoJson = contenidoJson;
    }
}

public sealed class LineaPlantilla
{
    private LineaPlantilla() { }

    public LineaPlantilla(int diaOffset, Guid tipoTurnoId, int plazas, Guid? categoriaId)
    {
        if (diaOffset is < 0 or >= Cuadrante.MaxDiasPeriodo) throw new ExcepcionDominio("Día de plantilla no válido.");
        if (plazas is < 1 or > 50) throw new ExcepcionDominio("El número de plazas debe estar entre 1 y 50.");
        DiaOffset = diaOffset;
        TipoTurnoId = tipoTurnoId;
        NumeroPlazas = plazas;
        CategoriaProfesionalId = categoriaId;
    }

    public int DiaOffset { get; private set; }
    public Guid TipoTurnoId { get; private set; }
    public int NumeroPlazas { get; private set; }
    public Guid? CategoriaProfesionalId { get; private set; }
}

/// <summary>Estructura repetible de turnos y necesidades (sin profesionales) para crear cuadrantes nuevos.</summary>
public sealed class PlantillaCuadrante : EntidadDeOrganizacion, IAuditable
{
    private readonly List<LineaPlantilla> _lineas = [];

    public Guid ServicioId { get; private set; }
    public string Nombre { get; private set; } = "";
    public int DiasPeriodo { get; private set; }
    public DateTimeOffset FechaCreacion { get; private set; }
    public IReadOnlyCollection<LineaPlantilla> Lineas => _lineas;

    private PlantillaCuadrante() { }

    public PlantillaCuadrante(Guid organizacionId, Guid servicioId, string nombre, int diasPeriodo, IEnumerable<LineaPlantilla> lineas,
        DateTimeOffset ahora) : base(organizacionId)
    {
        if (diasPeriodo is < 1 or > Cuadrante.MaxDiasPeriodo) throw new ExcepcionDominio("Duración de plantilla no válida.");
        ServicioId = servicioId;
        Nombre = Texto(nombre, "El nombre", 150);
        DiasPeriodo = diasPeriodo;
        FechaCreacion = ahora;
        _lineas.AddRange(lineas);
        if (_lineas.Count == 0) throw new ExcepcionDominio("La plantilla no tiene turnos.");
    }
}
