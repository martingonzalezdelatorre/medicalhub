using MedicalHub.Domain.Comun;

namespace MedicalHub.Domain.Ausencias;

/// <summary>
/// Disponible = declara que puede trabajar (informativo); Bloqueo = no puede trabajar (se aplica si está aprobado);
/// Prefiere / Evita = preferencias: no son prohibiciones salvo que una regla de la sociedad lo establezca.
/// </summary>
public enum TipoDisponibilidad { Disponible = 1, Bloqueo = 2, Prefiere = 3, Evita = 4 }

/// <summary>Declarada por el profesional; solo la Aprobada por el coordinador se tiene en cuenta en las reglas.</summary>
public enum EstadoDisponibilidad { Declarada = 1, Aprobada = 2, Rechazada = 3 }

public static class DiasSemana
{
    public const int Todos = 127;

    /// <summary>Bit del día: lunes = 1 … domingo = 64.</summary>
    public static int Bit(DayOfWeek dia) => 1 << (((int)dia + 6) % 7);
}

public sealed class Disponibilidad : EntidadDeOrganizacion, IAuditable
{
    public Guid ProfesionalId { get; private set; }
    public TipoDisponibilidad Tipo { get; private set; }
    public DateOnly Desde { get; private set; }
    public DateOnly Hasta { get; private set; }
    /// <summary>Máscara de días de la semana (lunes = 1 … domingo = 64; 127 = todos).</summary>
    public int Dias { get; private set; } = DiasSemana.Todos;
    /// <summary>Si se indica, solo afecta a ese tipo de turno.</summary>
    public Guid? TipoTurnoId { get; private set; }
    public string? Comentario { get; private set; }
    public EstadoDisponibilidad Estado { get; private set; } = EstadoDisponibilidad.Declarada;
    public Guid DeclaradaPorUsuarioId { get; private set; }
    public DateTimeOffset FechaDeclaracion { get; private set; }
    public Guid? DecididaPorUsuarioId { get; private set; }
    public DateTimeOffset? FechaDecision { get; private set; }
    public string? MotivoDecision { get; private set; }

    private Disponibilidad() { }

    public Disponibilidad(Guid organizacionId, Guid profesionalId, TipoDisponibilidad tipo, DateOnly desde, DateOnly hasta, int dias,
        Guid? tipoTurnoId, string? comentario, Guid declaradaPor, DateTimeOffset ahora) : base(organizacionId)
    {
        if (!Enum.IsDefined(tipo)) throw new ExcepcionDominio("El tipo de disponibilidad no es válido.");
        if (hasta < desde) throw new ExcepcionDominio("La fecha final no puede ser anterior a la inicial.");
        if (hasta.DayNumber - desde.DayNumber + 1 > 366) throw new ExcepcionDominio("El periodo no puede superar 366 días.");
        if (dias is < 1 or > DiasSemana.Todos) throw new ExcepcionDominio("Indique al menos un día de la semana.");
        ProfesionalId = profesionalId;
        Tipo = tipo;
        Desde = desde;
        Hasta = hasta;
        Dias = dias;
        TipoTurnoId = tipoTurnoId;
        Comentario = TextoOpcional(comentario, "El comentario", 300);
        DeclaradaPorUsuarioId = declaradaPor;
        FechaDeclaracion = ahora;
    }

    /// <summary>¿Afecta a un turno que ocupa de <paramref name="inicio"/> a <paramref name="fin"/> (días locales) y empieza ese día de la semana?</summary>
    public bool Afecta(DateOnly inicio, DateOnly fin, Guid? tipoTurnoId)
        => Desde <= fin && inicio <= Hasta
           && (Dias & DiasSemana.Bit(inicio.DayOfWeek)) != 0
           && (TipoTurnoId is null || TipoTurnoId == tipoTurnoId);

    public void Aprobar(Guid usuarioId, DateTimeOffset ahora, string? motivo) => Decidir(EstadoDisponibilidad.Aprobada, usuarioId, ahora, motivo);

    public void Rechazar(Guid usuarioId, DateTimeOffset ahora, string? motivo) => Decidir(EstadoDisponibilidad.Rechazada, usuarioId, ahora, motivo);

    private void Decidir(EstadoDisponibilidad estado, Guid usuarioId, DateTimeOffset ahora, string? motivo)
    {
        if (Estado != EstadoDisponibilidad.Declarada) throw new ExcepcionDominio("La declaración ya fue resuelta.");
        Estado = estado;
        DecididaPorUsuarioId = usuarioId;
        FechaDecision = ahora;
        MotivoDecision = TextoOpcional(motivo, "El motivo", 500);
    }
}
