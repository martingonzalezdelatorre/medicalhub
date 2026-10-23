using MedicalHub.Domain.Comun;
using MedicalHub.Domain.Estructura;
using MedicalHub.Domain.Horas;

namespace MedicalHub.Domain.Tests;

public class FichajeTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 23, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid Org = Guid.NewGuid();

    private static Fichaje Nuevo(Ubicacion? u = null, bool? fuera = null) => new(Org, Guid.NewGuid(), Guid.NewGuid(), T0, u, fuera);

    [Fact]
    public void La_jornada_descuenta_las_pausas_del_tiempo_trabajado()
    {
        var f = Nuevo();
        f.Pausar(T0.AddHours(3));
        Assert.Equal(EstadoFichaje.EnPausa, f.Estado);
        f.Reanudar(T0.AddHours(3).AddMinutes(30));
        f.Pausar(T0.AddHours(5));
        f.Cerrar(T0.AddHours(8), null, null);          // se cierra con una pausa abierta: se cierra con la salida

        Assert.Equal(EstadoFichaje.Cerrado, f.Estado);
        Assert.Equal(30 + 180, f.MinutosPausa(T0.AddHours(9)));
        Assert.Equal(480 - 210, f.MinutosTrabajados(T0.AddHours(9)));
        Assert.All(f.Pausas, p => Assert.NotNull(p.FinUtc));
    }

    [Fact]
    public void Las_transiciones_invalidas_se_rechazan()
    {
        var f = Nuevo();
        Assert.Throws<ExcepcionDominio>(() => f.Reanudar(T0.AddHours(1)));             // sin pausa
        f.Pausar(T0.AddHours(1));
        Assert.Throws<ExcepcionDominio>(() => f.Pausar(T0.AddHours(2)));               // ya en pausa
        f.Reanudar(T0.AddHours(2));
        Assert.Throws<ExcepcionDominio>(() => f.Cerrar(T0, null, null));               // salida no posterior a la entrada
        Assert.Throws<ExcepcionDominio>(() => f.Cerrar(T0.AddHours(25), null, null));  // más de 24 h
        f.Cerrar(T0.AddHours(4), null, null);
        Assert.Throws<ExcepcionDominio>(() => f.Cerrar(T0.AddHours(5), null, null));   // ya cerrada
        Assert.Throws<ExcepcionDominio>(() => f.Pausar(T0.AddHours(5)));
    }

    [Fact]
    public void Una_jornada_olvidada_caduca_y_se_descarta()
    {
        var f = Nuevo();
        Assert.False(f.Caducado(T0.AddHours(24)));
        Assert.True(f.Caducado(T0.AddHours(25)));
        f.Descartar(T0.AddHours(25));
        Assert.Equal(EstadoFichaje.Descartado, f.Estado);
        Assert.False(f.EstaAbierto);
        Assert.False(f.Caducado(T0.AddHours(30)));
    }

    [Fact]
    public void La_ubicacion_se_guarda_con_su_marca_de_zona_y_se_puede_anonimizar()
    {
        var f = Nuevo(new Ubicacion(40.4168, -3.7038, 12), fuera: true);
        Assert.Equal(40.4168, f.EntradaLatitud);
        Assert.True(f.EntradaFueraDeZona);
        f.Cerrar(T0.AddHours(2), new Ubicacion(40.4170, -3.7040, 8), false);
        Assert.False(f.SalidaFueraDeZona);
        Assert.True(f.FueraDeZona);

        f.BorrarUbicaciones();
        Assert.Null(f.EntradaLatitud);
        Assert.Null(f.SalidaLongitud);
        Assert.True(f.EntradaFueraDeZona);                  // la marca se conserva
    }

    [Fact]
    public void Sin_ubicacion_la_zona_no_es_evaluable()
    {
        var f = Nuevo(null, fuera: true);
        Assert.Null(f.EntradaFueraDeZona);
        Assert.False(f.FueraDeZona);
    }

    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    [InlineData(double.NaN, 0)]
    public void Las_coordenadas_fuera_de_rango_se_rechazan(double lat, double lon)
        => Assert.Throws<ExcepcionDominio>(() => new Ubicacion(lat, lon, null));

    [Fact]
    public void La_precision_negativa_se_rechaza()
        => Assert.Throws<ExcepcionDominio>(() => new Ubicacion(0, 0, -1));
}

public class GeovallaTests
{
    // Puerta del Sol (Madrid)
    private const double Lat = 40.41683, Lon = -3.70379;

    [Fact]
    public void La_distancia_haversine_es_razonable()
    {
        Assert.Equal(0, CalculadoraGeovalla.DistanciaMetros(Lat, Lon, Lat, Lon), 3);
        // 0,001° de latitud ≈ 111 m
        Assert.InRange(CalculadoraGeovalla.DistanciaMetros(Lat, Lon, Lat + 0.001, Lon), 110, 112);
        // Madrid–Barcelona ≈ 505 km
        Assert.InRange(CalculadoraGeovalla.DistanciaMetros(Lat, Lon, 41.3874, 2.1686) / 1000, 500, 510);
    }

    [Fact]
    public void Dentro_y_fuera_dependen_del_radio_y_la_precision_del_gps()
    {
        var cerca = new Ubicacion(Lat + 0.0005, Lon, null);          // ≈ 56 m
        var lejos = new Ubicacion(Lat + 0.002, Lon, null);           // ≈ 222 m
        Assert.True(CalculadoraGeovalla.EstaDentro(Lat, Lon, 100, cerca));
        Assert.False(CalculadoraGeovalla.EstaDentro(Lat, Lon, 100, lejos));
        // Una precisión de 150 m solo concede un margen de 100 m
        Assert.False(CalculadoraGeovalla.EstaDentro(Lat, Lon, 100, new Ubicacion(Lat + 0.002, Lon, 150)));
        Assert.True(CalculadoraGeovalla.EstaDentro(Lat, Lon, 150, new Ubicacion(Lat + 0.002, Lon, 150)));
    }

    [Fact]
    public void Sin_geovalla_o_sin_ubicacion_no_se_puede_evaluar()
    {
        Assert.Null(CalculadoraGeovalla.EstaDentro(null, null, null, new Ubicacion(Lat, Lon, null)));
        Assert.Null(CalculadoraGeovalla.EstaDentro(Lat, Lon, 100, null));
    }

    [Fact]
    public void El_centro_valida_su_geovalla()
    {
        var c = new Centro(Guid.NewGuid(), "Centro", TipoCentro.Hospital, null, "Europe/Madrid", DateTimeOffset.UtcNow);
        Assert.False(c.TieneGeovalla);
        c.EstablecerGeovalla(Lat, Lon, 150);
        Assert.True(c.TieneGeovalla);
        Assert.Throws<ExcepcionDominio>(() => c.EstablecerGeovalla(Lat, Lon, 5));
        Assert.Throws<ExcepcionDominio>(() => c.EstablecerGeovalla(Lat, Lon, 6000));
        Assert.Throws<ExcepcionDominio>(() => c.EstablecerGeovalla(100, Lon, 150));
        c.QuitarGeovalla();
        Assert.False(c.TieneGeovalla);
    }
}
