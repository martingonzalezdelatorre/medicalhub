using MedicalHub.Domain.Comun;

namespace MedicalHub.Domain.Notificaciones;

public enum TipoNotificacion
{
    AusenciaSolicitada = 1,
    AusenciaAprobada = 2,
    AusenciaRechazada = 3,
    VacanteAbierta = 4,
    OfertaRecibida = 5,
    OfertaAceptada = 6,
    OfertaRechazada = 7,
    SustitucionAprobada = 8,
    SustitucionDenegada = 9,
    OfertaRetirada = 10
}

/// <summary>
/// Aviso dentro de la aplicación para un usuario. El texto describe el hecho (qué, cuándo, dónde) sin datos de salud;
/// no guarda el motivo clínico de ninguna ausencia. Solo la lee su destinatario.
/// </summary>
public sealed class Notificacion : EntidadDeOrganizacion
{
    public const int MaxTitulo = 150;
    public const int MaxMensaje = 500;

    public Guid UsuarioId { get; private set; }
    public TipoNotificacion Tipo { get; private set; }
    public string Titulo { get; private set; } = "";
    public string Mensaje { get; private set; } = "";
    /// <summary>Entidad a la que se refiere (ausencia, vacante u oferta), para enlazar desde la interfaz.</summary>
    public Guid? EntidadId { get; private set; }
    public DateTimeOffset Fecha { get; private set; }
    public DateTimeOffset? FechaLectura { get; private set; }

    private Notificacion() { }

    public Notificacion(Guid organizacionId, Guid usuarioId, TipoNotificacion tipo, string titulo, string texto, Guid? entidadId, DateTimeOffset ahora) : base(organizacionId)
    {
        if (!Enum.IsDefined(tipo)) throw new ExcepcionDominio("El tipo de notificación no es válido.");
        UsuarioId = usuarioId;
        Tipo = tipo;
        Titulo = Texto(titulo, "El título", MaxTitulo);
        Mensaje = Texto(texto, "El texto", MaxMensaje);
        EntidadId = entidadId;
        Fecha = ahora;
    }

    public bool Leida => FechaLectura is not null;

    public void MarcarLeida(DateTimeOffset ahora) => FechaLectura ??= ahora;
}
