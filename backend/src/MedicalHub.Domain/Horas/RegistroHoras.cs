using MedicalHub.Domain.Comun;

namespace MedicalHub.Domain.Horas;

/// <summary>
/// Horas realmente trabajadas en una asignación (frente a las planificadas, que salen del turno). Lo registra la coordinación
/// y puede corregirse; cada corrección posterior de minutos se hace con un <see cref="AjusteHoras"/> con motivo, que no se borra.
/// </summary>
public sealed class RegistroHoras : EntidadDeOrganizacion, IAuditable
{
    public const int MaxHoras = 24;

    public Guid AsignacionId { get; private set; }
    public Guid ProfesionalId { get; private set; }
    public DateTimeOffset InicioRealUtc { get; private set; }
    public DateTimeOffset FinRealUtc { get; private set; }
    public Guid RegistradoPorUsuarioId { get; private set; }
    public DateTimeOffset FechaRegistro { get; private set; }

    public int Minutos => (int)(FinRealUtc - InicioRealUtc).TotalMinutes;

    private RegistroHoras() { }

    public RegistroHoras(Guid organizacionId, Guid asignacionId, Guid profesionalId, DateTimeOffset inicioUtc, DateTimeOffset finUtc, Guid usuarioId,
        DateTimeOffset ahora) : base(organizacionId)
    {
        AsignacionId = asignacionId;
        ProfesionalId = profesionalId;
        Actualizar(inicioUtc, finUtc, usuarioId, ahora);
    }

    public void Actualizar(DateTimeOffset inicioUtc, DateTimeOffset finUtc, Guid usuarioId, DateTimeOffset ahora)
    {
        if (finUtc <= inicioUtc) throw new ExcepcionDominio("El fin debe ser posterior al inicio.");
        if ((finUtc - inicioUtc).TotalHours > MaxHoras) throw new ExcepcionDominio($"Un registro no puede superar {MaxHoras} horas.");
        InicioRealUtc = inicioUtc.ToUniversalTime();
        FinRealUtc = finUtc.ToUniversalTime();
        RegistradoPorUsuarioId = usuarioId;
        FechaRegistro = ahora;
    }
}

/// <summary>Corrección en minutos (positiva o negativa) de un registro de horas. Inmutable: queda el rastro de quién y por qué.</summary>
public sealed class AjusteHoras : EntidadDeOrganizacion, IAuditable
{
    public const int MaxMinutos = 720;
    public const int MinMotivo = 10;
    public const int MaxMotivo = 300;

    public Guid RegistroHorasId { get; private set; }
    public int Minutos { get; private set; }
    public string Motivo { get; private set; } = "";
    public Guid UsuarioId { get; private set; }
    public DateTimeOffset Fecha { get; private set; }

    private AjusteHoras() { }

    public AjusteHoras(Guid organizacionId, Guid registroHorasId, int minutos, string motivo, Guid usuarioId, DateTimeOffset ahora) : base(organizacionId)
    {
        if (minutos == 0 || Math.Abs(minutos) > MaxMinutos) throw new ExcepcionDominio($"El ajuste debe ser distinto de cero y de como máximo {MaxMinutos} minutos.");
        var m = Texto(motivo, "El motivo", MaxMotivo);
        if (m.Length < MinMotivo) throw new ExcepcionDominio($"El motivo debe tener al menos {MinMotivo} caracteres.");
        RegistroHorasId = registroHorasId;
        Minutos = minutos;
        Motivo = m;
        UsuarioId = usuarioId;
        Fecha = ahora;
    }
}

/// <summary>Cálculo de horas por franja. La franja nocturna es fija en este MVP (22:00–06:00, hora local del centro).</summary>
public static class CalculadoraHoras
{
    public static readonly TimeOnly InicioNocturno = new(22, 0);
    public static readonly TimeOnly FinNocturno = new(6, 0);

    public static int MinutosTotales(DateTimeOffset inicioUtc, DateTimeOffset finUtc) => finUtc > inicioUtc ? (int)(finUtc - inicioUtc).TotalMinutes : 0;

    /// <summary>Minutos del intervalo que caen entre las 22:00 y las 06:00 en la zona indicada (respeta los cambios de hora).</summary>
    public static int MinutosNocturnos(string zonaHoraria, DateTimeOffset inicioUtc, DateTimeOffset finUtc)
    {
        if (finUtc <= inicioUtc) return 0;
        var zona = TimeZoneInfo.FindSystemTimeZoneById(zonaHoraria);
        var total = 0;
        // Se recorre por minutos; el desfase con UTC se reevalúa cada media hora (los cambios de hora ocurren en esos límites).
        var desfase = TimeSpan.Zero;
        for (var t = inicioUtc.ToUniversalTime(); t < finUtc; t = t.AddMinutes(1))
        {
            if (t == inicioUtc.ToUniversalTime() || t.Minute % 30 == 0) desfase = zona.GetUtcOffset(t);
            var local = TimeOnly.FromDateTime(t.UtcDateTime + desfase);
            if (local >= InicioNocturno || local < FinNocturno) total++;
        }
        return total;
    }
}
