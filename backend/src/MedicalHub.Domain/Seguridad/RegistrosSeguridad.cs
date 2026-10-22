using MedicalHub.Domain.Comun;

namespace MedicalHub.Domain.Seguridad;

public enum TipoEventoSeguridad
{
    InicioSesionCorrecto = 1,
    InicioSesionFallido = 2,
    CuentaBloqueada = 3,
    MfaRequerido = 4,
    MfaActivado = 5,
    MfaFallido = 6,
    TokenRefrescoReutilizado = 7,
    CierreSesion = 8,
    ContrasenaCambiada = 9,
    AccesoDenegado = 10,
    ExportacionDatos = 11
}

/// <summary>Evento de seguridad (autenticación y accesos). No lleva datos personales más allá del correo intentado.</summary>
public sealed class EventoSeguridad : Entidad
{
    public Guid? OrganizacionId { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public TipoEventoSeguridad Tipo { get; private set; }
    public string? Email { get; private set; }
    public string? Ip { get; private set; }
    public string? Detalle { get; private set; }
    public DateTimeOffset Fecha { get; private set; }

    private EventoSeguridad() { }

    public EventoSeguridad(TipoEventoSeguridad tipo, DateTimeOffset fecha, Guid? organizacionId = null,
        Guid? usuarioId = null, string? email = null, string? ip = null, string? detalle = null)
    {
        Tipo = tipo;
        Fecha = fecha;
        OrganizacionId = organizacionId;
        UsuarioId = usuarioId;
        Email = email?.Length > 254 ? email[..254] : email;
        Ip = ip;
        Detalle = detalle?.Length > 500 ? detalle[..500] : detalle;
    }
}

/// <summary>Registro de auditoría de cambios operativos.</summary>
public sealed class RegistroAuditoria : Entidad, IPerteneceAOrganizacion
{
    public Guid? OrganizacionId { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public string Entidad { get; private set; } = "";
    public string EntidadId { get; private set; } = "";
    public string Accion { get; private set; } = "";
    public string? Cambios { get; private set; }
    public string? IdCorrelacion { get; private set; }
    public DateTimeOffset Fecha { get; private set; }

    private RegistroAuditoria() { }

    public RegistroAuditoria(Guid? organizacionId, Guid? usuarioId, string entidad, string entidadId,
        string accion, string? cambios, string? idCorrelacion, DateTimeOffset fecha)
    {
        OrganizacionId = organizacionId;
        UsuarioId = usuarioId;
        Entidad = entidad;
        EntidadId = entidadId;
        Accion = accion;
        Cambios = cambios;
        IdCorrelacion = idCorrelacion;
        Fecha = fecha;
    }
}
