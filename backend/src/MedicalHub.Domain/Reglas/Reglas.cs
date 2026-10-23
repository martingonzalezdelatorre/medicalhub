using System.Text.Json;
using MedicalHub.Domain.Comun;

namespace MedicalHub.Domain.Reglas;

/// <summary>
/// Tipos de regla soportados por el motor. Los límites concretos (horas, descansos…) los define cada sociedad:
/// el sistema NO incluye valores legales por defecto.
/// </summary>
public enum TipoRegla
{
    // Elegibilidad (comportamiento por defecto: bloqueante con excepción posible)
    AutorizacionServicio = 1,
    CategoriaAdmitida = 2,
    CompetenciasRequeridas = 3,
    CategoriaNecesidad = 4,

    // Límites configurables por la sociedad
    DescansoMinimo = 10,
    MaxHorasPeriodo = 11,
    MaxGuardiasPeriodo = 12,
    MaxDiasConsecutivos = 13,
    IncompatibilidadServicios = 14,

    // Evaluadas al validar un cuadrante
    CoberturaMinima = 20,
    AsignacionPendiente = 21,

    // Datos del propio profesional (E5)
    AusenciaAprobada = 30,
    DisponibilidadBloqueada = 31,
    PreferenciaEvitar = 32
}

public enum SeveridadRegla { Advertencia = 1, Bloqueante = 2 }

/// <summary>Regla de planificación configurada por una sociedad (para toda la sociedad o para un servicio).</summary>
public sealed class ReglaPlanificacion : EntidadDeOrganizacion, IAuditable
{
    public TipoRegla Tipo { get; private set; }
    public string Nombre { get; private set; } = "";
    public string? Descripcion { get; private set; }
    /// <summary>null = se aplica a toda la sociedad.</summary>
    public Guid? ServicioId { get; private set; }
    public SeveridadRegla Severidad { get; private set; }
    public bool Activa { get; private set; } = true;
    /// <summary>Si es bloqueante, indica si puede saltarse con una excepción justificada.</summary>
    public bool PermiteExcepcion { get; private set; }
    /// <summary>La excepción solo la puede autorizar el administrador de la sociedad (no el coordinador).</summary>
    public bool ExcepcionRequiereAdministrador { get; private set; }
    public string? ParametrosJson { get; private set; }
    /// <summary>Base normativa, convenio o acuerdo que justifica la regla (lo completa la sociedad).</summary>
    public string? Fundamento { get; private set; }
    public DateTimeOffset? RevisadaEl { get; private set; }
    public Guid? RevisadaPorUsuarioId { get; private set; }
    public DateTimeOffset FechaCreacion { get; private set; }

    private ReglaPlanificacion() { }

    public ReglaPlanificacion(Guid organizacionId, TipoRegla tipo, string nombre, string? descripcion, Guid? servicioId,
        SeveridadRegla severidad, bool permiteExcepcion, bool requiereAdministrador, string? parametrosJson,
        string? fundamento, DateTimeOffset ahora) : base(organizacionId)
    {
        if (!Enum.IsDefined(tipo)) throw new ExcepcionDominio("El tipo de regla no es válido.");
        Tipo = tipo;
        FechaCreacion = ahora;
        Actualizar(nombre, descripcion, servicioId, severidad, permiteExcepcion, requiereAdministrador, parametrosJson, fundamento);
    }

    public void Actualizar(string nombre, string? descripcion, Guid? servicioId, SeveridadRegla severidad, bool permiteExcepcion,
        bool requiereAdministrador, string? parametrosJson, string? fundamento)
    {
        if (!Enum.IsDefined(severidad)) throw new ExcepcionDominio("La severidad no es válida.");
        Nombre = Texto(nombre, "El nombre", 150);
        Descripcion = TextoOpcional(descripcion, "La descripción", 500);
        Fundamento = TextoOpcional(fundamento, "El fundamento", 1000);
        ServicioId = servicioId;
        Severidad = severidad;
        // Una advertencia nunca bloquea, así que no tiene sentido exigir excepción.
        PermiteExcepcion = severidad == SeveridadRegla.Bloqueante && permiteExcepcion;
        ExcepcionRequiereAdministrador = PermiteExcepcion && requiereAdministrador;

        if (string.IsNullOrWhiteSpace(parametrosJson)) ParametrosJson = null;
        else
        {
            if (parametrosJson.Length > 2000) throw new ExcepcionDominio("Los parámetros de la regla son demasiado extensos.");
            try { JsonDocument.Parse(parametrosJson).Dispose(); }
            catch (JsonException) { throw new ExcepcionDominio("Los parámetros de la regla no son un JSON válido."); }
            ParametrosJson = parametrosJson.Trim();
        }
        // Cualquier cambio requiere una nueva revisión por la sociedad.
        RevisadaEl = null;
        RevisadaPorUsuarioId = null;
    }

    public void MarcarRevisada(Guid usuarioId, DateTimeOffset ahora)
    {
        RevisadaEl = ahora;
        RevisadaPorUsuarioId = usuarioId;
    }

    public void Activar() => Activa = true;
    public void Desactivar() => Activa = false;
}

/// <summary>
/// Excepción autorizada: una asignación que incumple una regla bloqueante, con su justificación y quién la autorizó.
/// Es inmutable; si la asignación se cancela, la excepción queda como historial.
/// </summary>
public sealed class ExcepcionRegla : EntidadDeOrganizacion, IAuditable
{
    public Guid AsignacionId { get; private set; }
    public Guid TurnoId { get; private set; }
    public Guid ProfesionalId { get; private set; }
    /// <summary>Identifica la regla incumplida: el id de la regla configurada o, si es la de por defecto, el nombre del tipo.</summary>
    public string ClaveRegla { get; private set; } = "";
    public string Codigo { get; private set; } = "";
    public Guid? ReglaId { get; private set; }
    /// <summary>Texto de la infracción en el momento de la excepción.</summary>
    public string Mensaje { get; private set; } = "";
    public string Justificacion { get; private set; } = "";
    public Guid AutorizadaPorUsuarioId { get; private set; }
    public DateTimeOffset Fecha { get; private set; }

    public const int MinLongitudJustificacion = 10;

    private ExcepcionRegla() { }

    public ExcepcionRegla(Guid organizacionId, Guid asignacionId, Guid turnoId, Guid profesionalId, string claveRegla, string codigo,
        Guid? reglaId, string mensaje, string justificacion, Guid autorizadaPor, DateTimeOffset ahora) : base(organizacionId)
    {
        if (string.IsNullOrWhiteSpace(justificacion) || justificacion.Trim().Length < MinLongitudJustificacion)
            throw new ExcepcionDominio($"La justificación de la excepción es obligatoria (mínimo {MinLongitudJustificacion} caracteres).");
        if (justificacion.Length > 1000) throw new ExcepcionDominio("La justificación no puede superar 1000 caracteres.");
        AsignacionId = asignacionId;
        TurnoId = turnoId;
        ProfesionalId = profesionalId;
        ClaveRegla = claveRegla;
        Codigo = codigo;
        ReglaId = reglaId;
        Mensaje = mensaje.Length > 500 ? mensaje[..500] : mensaje;
        Justificacion = justificacion.Trim();
        AutorizadaPorUsuarioId = autorizadaPor;
        Fecha = ahora;
    }
}
