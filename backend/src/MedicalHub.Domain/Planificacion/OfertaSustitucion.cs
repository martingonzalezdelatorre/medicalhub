using MedicalHub.Domain.Comun;

namespace MedicalHub.Domain.Planificacion;

/// <summary>
/// Pendiente: enviada al profesional. Aceptada/Rechazada: respuesta del profesional. Aprobada: el coordinador la confirmó y se creó la asignación.
/// Denegada: el coordinador descartó una oferta aceptada. Retirada: la anuló el coordinador. Caducada: venció sin respuesta.
/// Cerrada: la vacante se cubrió por otra vía.
/// </summary>
public enum EstadoOferta { Pendiente = 1, Aceptada = 2, Rechazada = 3, Aprobada = 4, Denegada = 5, Retirada = 6, Caducada = 7, Cerrada = 8 }

/// <summary>
/// Oferta de una vacante a un profesional. El profesional solo acepta o rechaza; la asignación real se crea cuando el coordinador
/// aprueba (momento en el que se vuelven a evaluar las reglas y, si procede, se autoriza una excepción justificada).
/// </summary>
public sealed class OfertaSustitucion : EntidadDeOrganizacion, IAuditable
{
    public const int MaxMotivo = 300;

    public Guid VacanteId { get; private set; }
    public Guid ProfesionalId { get; private set; }
    public EstadoOferta Estado { get; private set; } = EstadoOferta.Pendiente;
    public Guid OfrecidaPorUsuarioId { get; private set; }
    public DateTimeOffset FechaOferta { get; private set; }
    /// <summary>Instante a partir del cual una oferta pendiente ya no puede aceptarse. Null = sin caducidad.</summary>
    public DateTimeOffset? Caduca { get; private set; }
    public DateTimeOffset? FechaRespuesta { get; private set; }
    public string? MotivoRespuesta { get; private set; }
    public DateTimeOffset? FechaDecision { get; private set; }
    public Guid? DecididaPorUsuarioId { get; private set; }
    public string? MotivoDecision { get; private set; }
    public Guid? AsignacionId { get; private set; }

    private OfertaSustitucion() { }

    public OfertaSustitucion(Guid organizacionId, Guid vacanteId, Guid profesionalId, Guid ofrecidaPor, DateTimeOffset ahora, DateTimeOffset? caduca) : base(organizacionId)
    {
        if (caduca is { } c && c <= ahora) throw new ExcepcionDominio("La caducidad debe ser posterior al momento actual.");
        VacanteId = vacanteId;
        ProfesionalId = profesionalId;
        OfrecidaPorUsuarioId = ofrecidaPor;
        FechaOferta = ahora;
        Caduca = caduca;
    }

    /// <summary>Abierta: aún puede cambiar de estado (pendiente o aceptada a la espera del coordinador).</summary>
    public bool EstaAbierta => Estado is EstadoOferta.Pendiente or EstadoOferta.Aceptada;

    public bool HaVencido(DateTimeOffset ahora) => Estado == EstadoOferta.Pendiente && Caduca is { } c && c <= ahora;

    /// <summary>Estado que debe mostrarse: una pendiente vencida cuenta como caducada aunque nadie la haya actualizado.</summary>
    public EstadoOferta EstadoEfectivo(DateTimeOffset ahora) => HaVencido(ahora) ? EstadoOferta.Caducada : Estado;

    public void Aceptar(DateTimeOffset ahora, string? motivo)
    {
        ExigirPendiente(ahora);
        Estado = EstadoOferta.Aceptada;
        Responder(ahora, motivo);
    }

    public void Rechazar(DateTimeOffset ahora, string? motivo)
    {
        ExigirPendiente(ahora);
        Estado = EstadoOferta.Rechazada;
        Responder(ahora, motivo);
    }

    public void Aprobar(Guid usuarioId, Guid asignacionId, DateTimeOffset ahora)
    {
        if (Estado != EstadoOferta.Aceptada) throw new ExcepcionDominio("Solo se puede aprobar una oferta aceptada por el profesional.");
        Estado = EstadoOferta.Aprobada;
        AsignacionId = asignacionId;
        Decidir(usuarioId, ahora, null);
    }

    public void Denegar(Guid usuarioId, DateTimeOffset ahora, string motivo)
    {
        if (Estado != EstadoOferta.Aceptada) throw new ExcepcionDominio("Solo se puede denegar una oferta aceptada por el profesional.");
        if (string.IsNullOrWhiteSpace(motivo)) throw new ExcepcionDominio("Indique el motivo.");
        Estado = EstadoOferta.Denegada;
        Decidir(usuarioId, ahora, motivo);
    }

    public void Retirar(Guid usuarioId, DateTimeOffset ahora, string? motivo)
    {
        if (!EstaAbierta) throw new ExcepcionDominio("La oferta ya no está abierta.");
        Estado = EstadoOferta.Retirada;
        Decidir(usuarioId, ahora, motivo);
    }

    public void Cerrar(DateTimeOffset ahora, string motivo)
    {
        if (!EstaAbierta) return;
        Estado = EstadoOferta.Cerrada;
        FechaDecision = ahora;
        MotivoDecision = TextoOpcional(motivo, "El motivo", MaxMotivo);
    }

    /// <summary>Marca como caducada una oferta pendiente vencida (se llama al tocarla, p. ej. al intentar aceptarla).</summary>
    public void Caducar(DateTimeOffset ahora)
    {
        if (!HaVencido(ahora)) return;
        Estado = EstadoOferta.Caducada;
    }

    private void ExigirPendiente(DateTimeOffset ahora)
    {
        if (HaVencido(ahora)) throw new ExcepcionDominio("La oferta ha caducado.");
        if (Estado != EstadoOferta.Pendiente) throw new ExcepcionDominio("La oferta ya ha sido respondida o cerrada.");
    }

    private void Responder(DateTimeOffset ahora, string? motivo)
    {
        FechaRespuesta = ahora;
        MotivoRespuesta = TextoOpcional(motivo, "El motivo", MaxMotivo);
    }

    private void Decidir(Guid usuarioId, DateTimeOffset ahora, string? motivo)
    {
        DecididaPorUsuarioId = usuarioId;
        FechaDecision = ahora;
        MotivoDecision = TextoOpcional(motivo, "El motivo", MaxMotivo);
    }
}
