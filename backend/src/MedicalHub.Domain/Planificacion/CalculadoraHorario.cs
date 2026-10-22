namespace MedicalHub.Domain.Planificacion;

/// <summary>
/// Convierte la hora local de un centro en instantes UTC. Los turnos se definen en hora local del centro,
/// pero se comparan y solapan en UTC. El fin se calcula sobre el reloj local (no sumando una duración fija),
/// de modo que una noche con cambio de hora dura realmente 9 u 11 horas.
/// </summary>
public static class CalculadoraHorario
{
    public static (DateTimeOffset InicioUtc, DateTimeOffset FinUtc) Resolver(string zonaHoraria, DateOnly fecha, TimeOnly inicio, TimeOnly fin)
    {
        var zona = TimeZoneInfo.FindSystemTimeZoneById(zonaHoraria);
        var inicioUtc = ALocalUtc(zona, fecha.ToDateTime(inicio));
        var fechaFin = fin <= inicio ? fecha.AddDays(1) : fecha; // termina al día siguiente
        var finUtc = ALocalUtc(zona, fechaFin.ToDateTime(fin));
        return (inicioUtc, finUtc);
    }

    /// <summary>Hora local legible de un instante (para mostrar en la zona del centro).</summary>
    public static DateTime ALocal(string zonaHoraria, DateTimeOffset instanteUtc)
        => TimeZoneInfo.ConvertTime(instanteUtc, TimeZoneInfo.FindSystemTimeZoneById(zonaHoraria)).DateTime;

    private static DateTimeOffset ALocalUtc(TimeZoneInfo zona, DateTime local)
    {
        // Hora inexistente (salto de primavera): se adelanta una hora.
        if (zona.IsInvalidTime(local)) local = local.AddHours(1);
        // Hora repetida (retroceso de otoño): se toma la primera ocurrencia (horario de verano).
        var desfase = zona.IsAmbiguousTime(local) ? zona.GetAmbiguousTimeOffsets(local).Max() : zona.GetUtcOffset(local);
        return new DateTimeOffset(local, desfase).ToUniversalTime();
    }

    public static bool Solapan(DateTimeOffset inicioA, DateTimeOffset finA, DateTimeOffset inicioB, DateTimeOffset finB)
        => inicioA < finB && inicioB < finA;
}
