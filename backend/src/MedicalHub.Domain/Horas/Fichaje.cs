using MedicalHub.Domain.Comun;

namespace MedicalHub.Domain.Horas;

public enum EstadoFichaje { EnCurso = 1, EnPausa = 2, Cerrado = 3, Descartado = 4 }

/// <summary>Posición GPS enviada por el dispositivo en el momento del fichaje.</summary>
public sealed record Ubicacion
{
    public const double MaxPrecisionMetros = 100_000;

    public double Latitud { get; }
    public double Longitud { get; }
    public double? PrecisionMetros { get; }

    public Ubicacion(double latitud, double longitud, double? precisionMetros)
    {
        if (double.IsNaN(latitud) || latitud is < -90 or > 90) throw new ExcepcionDominio("La latitud debe estar entre -90 y 90.");
        if (double.IsNaN(longitud) || longitud is < -180 or > 180) throw new ExcepcionDominio("La longitud debe estar entre -180 y 180.");
        if (precisionMetros is { } p && (double.IsNaN(p) || p < 0 || p > MaxPrecisionMetros)) throw new ExcepcionDominio("La precisión no es válida.");
        Latitud = latitud;
        Longitud = longitud;
        PrecisionMetros = precisionMetros;
    }
}

/// <summary>Geovalla circular de un centro y comprobación de si una posición cae dentro.</summary>
public static class CalculadoraGeovalla
{
    public const int MinRadioMetros = 20;
    public const int MaxRadioMetros = 5000;
    /// <summary>Margen máximo que se concede por imprecisión del GPS (se suma a la distancia permitida).</summary>
    public const double MaxMargenPrecisionMetros = 100;

    public static double DistanciaMetros(double lat1, double lon1, double lat2, double lon2)
    {
        const double radioTierra = 6_371_000;
        static double Rad(double g) => g * Math.PI / 180;
        var dLat = Rad(lat2 - lat1);
        var dLon = Rad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * radioTierra * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    /// <summary>true/false si el centro tiene geovalla y hay ubicación; null si no se puede evaluar.</summary>
    public static bool? EstaDentro(double? centroLat, double? centroLon, int? radioMetros, Ubicacion? u)
    {
        if (centroLat is null || centroLon is null || radioMetros is null || u is null) return null;
        var margen = Math.Min(u.PrecisionMetros ?? 0, MaxMargenPrecisionMetros);
        return DistanciaMetros(centroLat.Value, centroLon.Value, u.Latitud, u.Longitud) <= radioMetros.Value + margen;
    }
}

/// <summary>Intervalo de pausa dentro de un fichaje. Tipo poseído.</summary>
public sealed class PausaFichaje
{
    private PausaFichaje() { }
    public PausaFichaje(DateTimeOffset inicioUtc) => InicioUtc = inicioUtc;

    public DateTimeOffset InicioUtc { get; private set; }
    public DateTimeOffset? FinUtc { get; private set; }

    public void Cerrar(DateTimeOffset finUtc) => FinUtc = finUtc;
    public int Minutos(DateTimeOffset hasta) => (int)Math.Max(0, ((FinUtc ?? hasta) - InicioUtc).TotalMinutes);
}

/// <summary>
/// Jornada fichada por el propio profesional (entrada, pausas y salida) sobre una asignación. Al cerrarla se vuelca a
/// <see cref="RegistroHoras"/>. La ubicación solo se guarda en la entrada y la salida; si el centro tiene geovalla se anota si
/// quedó fuera, pero no se bloquea el fichaje (decide la coordinación al revisarlo).
/// </summary>
public sealed class Fichaje : EntidadDeOrganizacion, IAuditable
{
    private readonly List<PausaFichaje> _pausas = [];

    public Guid AsignacionId { get; private set; }
    public Guid ProfesionalId { get; private set; }
    public EstadoFichaje Estado { get; private set; } = EstadoFichaje.EnCurso;
    public DateTimeOffset InicioUtc { get; private set; }
    public DateTimeOffset? FinUtc { get; private set; }

    public double? EntradaLatitud { get; private set; }
    public double? EntradaLongitud { get; private set; }
    public double? EntradaPrecisionMetros { get; private set; }
    /// <summary>null = no evaluable (centro sin geovalla o ubicación no facilitada).</summary>
    public bool? EntradaFueraDeZona { get; private set; }

    public double? SalidaLatitud { get; private set; }
    public double? SalidaLongitud { get; private set; }
    public double? SalidaPrecisionMetros { get; private set; }
    public bool? SalidaFueraDeZona { get; private set; }

    public IReadOnlyCollection<PausaFichaje> Pausas => _pausas;
    public bool EstaAbierto => Estado is EstadoFichaje.EnCurso or EstadoFichaje.EnPausa;
    public bool FueraDeZona => EntradaFueraDeZona == true || SalidaFueraDeZona == true;

    private Fichaje() { }

    public Fichaje(Guid organizacionId, Guid asignacionId, Guid profesionalId, DateTimeOffset ahora, Ubicacion? ubicacion, bool? fueraDeZona)
        : base(organizacionId)
    {
        AsignacionId = asignacionId;
        ProfesionalId = profesionalId;
        InicioUtc = ahora.ToUniversalTime();
        (EntradaLatitud, EntradaLongitud, EntradaPrecisionMetros) = (ubicacion?.Latitud, ubicacion?.Longitud, ubicacion?.PrecisionMetros);
        EntradaFueraDeZona = ubicacion is null ? null : fueraDeZona;
    }

    public void Pausar(DateTimeOffset ahora)
    {
        if (Estado != EstadoFichaje.EnCurso) throw new ExcepcionDominio("Solo se puede pausar una jornada en curso.");
        if (ahora < InicioUtc) throw new ExcepcionDominio("La pausa no puede empezar antes de la entrada.");
        _pausas.Add(new PausaFichaje(ahora.ToUniversalTime()));
        Estado = EstadoFichaje.EnPausa;
    }

    public void Reanudar(DateTimeOffset ahora)
    {
        if (Estado != EstadoFichaje.EnPausa) throw new ExcepcionDominio("No hay ninguna pausa en curso.");
        AbiertaOrThrow().Cerrar(ahora.ToUniversalTime());
        Estado = EstadoFichaje.EnCurso;
    }

    /// <summary>Cierra la jornada (cerrando antes la pausa abierta, si la hay).</summary>
    public void Cerrar(DateTimeOffset ahora, Ubicacion? ubicacion, bool? fueraDeZona)
    {
        if (!EstaAbierto) throw new ExcepcionDominio("La jornada ya está cerrada.");
        var fin = ahora.ToUniversalTime();
        if (fin <= InicioUtc) throw new ExcepcionDominio("La salida debe ser posterior a la entrada.");
        if ((fin - InicioUtc).TotalHours > RegistroHoras.MaxHoras) throw new ExcepcionDominio($"Una jornada no puede superar {RegistroHoras.MaxHoras} horas.");
        if (Estado == EstadoFichaje.EnPausa) AbiertaOrThrow().Cerrar(fin);
        FinUtc = fin;
        (SalidaLatitud, SalidaLongitud, SalidaPrecisionMetros) = (ubicacion?.Latitud, ubicacion?.Longitud, ubicacion?.PrecisionMetros);
        SalidaFueraDeZona = ubicacion is null ? null : fueraDeZona;
        Estado = EstadoFichaje.Cerrado;
    }

    /// <summary>Jornada olvidada abierta más de 24 h: se descarta (no genera horas) para no bloquear nuevos fichajes.</summary>
    public void Descartar(DateTimeOffset ahora)
    {
        if (!EstaAbierto) throw new ExcepcionDominio("La jornada ya está cerrada.");
        FinUtc = ahora.ToUniversalTime();
        Estado = EstadoFichaje.Descartado;
    }

    public bool Caducado(DateTimeOffset ahora) => EstaAbierto && (ahora - InicioUtc).TotalHours > RegistroHoras.MaxHoras;

    public int MinutosPausa(DateTimeOffset ahora) => _pausas.Sum(p => p.Minutos(FinUtc ?? ahora));

    public int MinutosTrabajados(DateTimeOffset ahora)
        => Math.Max(0, (int)((FinUtc ?? ahora) - InicioUtc).TotalMinutes - MinutosPausa(ahora));

    /// <summary>Anonimiza las coordenadas (plazo de retención) conservando el resto del fichaje.</summary>
    public void BorrarUbicaciones()
    {
        (EntradaLatitud, EntradaLongitud, EntradaPrecisionMetros) = (null, null, null);
        (SalidaLatitud, SalidaLongitud, SalidaPrecisionMetros) = (null, null, null);
    }

    private PausaFichaje AbiertaOrThrow() => _pausas.LastOrDefault(p => p.FinUtc is null) ?? throw new ExcepcionDominio("No hay ninguna pausa en curso.");
}
