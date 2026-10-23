using MedicalHub.Domain.Comun;

namespace MedicalHub.Domain.Ausencias;

public enum TipoAusencia { Vacaciones = 1, AsuntosPropios = 2, Permiso = 3, Formacion = 4, BajaTemporal = 5, Otro = 6 }

public enum EstadoAusencia { Solicitada = 1, Aprobada = 2, Rechazada = 3, Cancelada = 4 }

/// <summary>
/// Ausencia de un profesional (días completos, inclusive). Solo se guarda lo necesario para gestionarla:
/// el tipo y un comentario opcional. NO se recogen diagnósticos ni datos de salud.
/// </summary>
public sealed class Ausencia : EntidadDeOrganizacion, IAuditable
{
    public const int MaxDias = 366;
    public const int MaxComentario = 300;

    public Guid ProfesionalId { get; private set; }
    public TipoAusencia Tipo { get; private set; }
    public DateOnly Desde { get; private set; }
    public DateOnly Hasta { get; private set; }
    public string? Comentario { get; private set; }
    public EstadoAusencia Estado { get; private set; } = EstadoAusencia.Solicitada;
    public Guid SolicitadaPorUsuarioId { get; private set; }
    public DateTimeOffset FechaSolicitud { get; private set; }

    public int Dias => Hasta.DayNumber - Desde.DayNumber + 1;
    /// <summary>Solicitada o aprobada: ocupa esas fechas para el profesional.</summary>
    public bool EstaVigente => Estado is EstadoAusencia.Solicitada or EstadoAusencia.Aprobada;

    private Ausencia() { }

    public Ausencia(Guid organizacionId, Guid profesionalId, TipoAusencia tipo, DateOnly desde, DateOnly hasta, string? comentario,
        Guid solicitadaPor, DateTimeOffset ahora) : base(organizacionId)
    {
        if (!Enum.IsDefined(tipo)) throw new ExcepcionDominio("El tipo de ausencia no es válido.");
        if (hasta < desde) throw new ExcepcionDominio("La fecha final no puede ser anterior a la inicial.");
        if (hasta.DayNumber - desde.DayNumber + 1 > MaxDias) throw new ExcepcionDominio($"Una ausencia no puede superar {MaxDias} días.");
        ProfesionalId = profesionalId;
        Tipo = tipo;
        Desde = desde;
        Hasta = hasta;
        Comentario = TextoOpcional(comentario, "El comentario", MaxComentario);
        SolicitadaPorUsuarioId = solicitadaPor;
        FechaSolicitud = ahora;
    }

    public bool Solapa(DateOnly desde, DateOnly hasta) => Desde <= hasta && desde <= Hasta;

    public void Aprobar()
    {
        if (Estado != EstadoAusencia.Solicitada) throw new ExcepcionDominio("Solo se puede aprobar una solicitud pendiente.");
        Estado = EstadoAusencia.Aprobada;
    }

    public void Rechazar()
    {
        if (Estado != EstadoAusencia.Solicitada) throw new ExcepcionDominio("Solo se puede rechazar una solicitud pendiente.");
        Estado = EstadoAusencia.Rechazada;
    }

    public void Cancelar()
    {
        if (!EstaVigente) throw new ExcepcionDominio("La ausencia ya no está vigente.");
        Estado = EstadoAusencia.Cancelada;
    }
}

/// <summary>Historial de decisiones sobre una ausencia (quién, cuándo y por qué). Inmutable.</summary>
public sealed class DecisionAusencia : EntidadDeOrganizacion
{
    public Guid AusenciaId { get; private set; }
    public EstadoAusencia Decision { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTimeOffset Fecha { get; private set; }
    public string? Motivo { get; private set; }

    private DecisionAusencia() { }

    public DecisionAusencia(Guid organizacionId, Guid ausenciaId, EstadoAusencia decision, Guid usuarioId, DateTimeOffset fecha, string? motivo)
        : base(organizacionId)
    {
        AusenciaId = ausenciaId;
        Decision = decision;
        UsuarioId = usuarioId;
        Fecha = fecha;
        Motivo = TextoOpcional(motivo, "El motivo", 500);
    }
}
