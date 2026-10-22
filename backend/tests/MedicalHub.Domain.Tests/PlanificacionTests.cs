using MedicalHub.Domain.Comun;
using MedicalHub.Domain.Planificacion;

namespace MedicalHub.Domain.Tests;

public class CalculadoraHorarioTests
{
    private static (DateTimeOffset i, DateTimeOffset f) R(string zona, string fecha, string ini, string fin)
        => CalculadoraHorario.Resolver(zona, DateOnly.Parse(fecha), TimeOnly.Parse(ini), TimeOnly.Parse(fin));

    private static DateTimeOffset U(string iso) => DateTimeOffset.Parse(iso).ToUniversalTime();

    [Fact]
    public void Un_turno_de_verano_se_convierte_a_utc_con_el_desfase_de_la_zona()
    {
        var (i, f) = R("Europe/Madrid", "2026-07-10", "08:00", "15:00");
        Assert.Equal(U("2026-07-10T06:00:00Z"), i);
        Assert.Equal(U("2026-07-10T13:00:00Z"), f);
    }

    [Fact]
    public void Un_turno_de_invierno_usa_el_desfase_de_invierno()
    {
        var (i, _) = R("Europe/Madrid", "2026-01-15", "08:00", "15:00");
        Assert.Equal(U("2026-01-15T07:00:00Z"), i);
    }

    [Fact]
    public void Un_turno_nocturno_termina_al_dia_siguiente()
    {
        var (i, f) = R("Europe/Madrid", "2026-07-10", "22:00", "08:00");
        Assert.Equal(U("2026-07-10T20:00:00Z"), i);
        Assert.Equal(U("2026-07-11T06:00:00Z"), f);
        Assert.Equal(10, (f - i).TotalHours);
    }

    [Fact]
    public void La_noche_del_retroceso_de_hora_dura_11_horas()
    {
        // 25/10/2026: a las 03:00 los relojes vuelven a las 02:00 (fin del horario de verano)
        var (i, f) = R("Europe/Madrid", "2026-10-24", "22:00", "08:00");
        Assert.Equal(U("2026-10-24T20:00:00Z"), i);
        Assert.Equal(U("2026-10-25T07:00:00Z"), f);
        Assert.Equal(11, (f - i).TotalHours);
    }

    [Fact]
    public void La_noche_del_adelanto_de_hora_dura_9_horas()
    {
        // 29/03/2026: a las 02:00 los relojes saltan a las 03:00
        var (i, f) = R("Europe/Madrid", "2026-03-28", "22:00", "08:00");
        Assert.Equal(U("2026-03-28T21:00:00Z"), i);
        Assert.Equal(U("2026-03-29T06:00:00Z"), f);
        Assert.Equal(9, (f - i).TotalHours);
    }

    [Theory]
    [InlineData("2026-10-24", 25)] // guardia de 24 h «locales» que incluye el retroceso: 25 h reales
    [InlineData("2026-03-28", 23)] // ...y el adelanto: 23 h reales
    [InlineData("2026-07-01", 24)]
    public void La_guardia_de_24_horas_locales_se_ajusta_al_cambio_de_hora(string fecha, double horas)
    {
        var (i, f) = R("Europe/Madrid", fecha, "08:00", "08:00");
        Assert.Equal(horas, (f - i).TotalHours);
    }

    [Fact]
    public void Una_hora_que_no_existe_por_el_adelanto_se_desplaza_una_hora()
    {
        var (i, _) = R("Europe/Madrid", "2026-03-29", "02:30", "10:00");
        Assert.Equal(U("2026-03-29T01:30:00Z"), i); // 03:30 CEST
    }

    [Fact]
    public void Una_hora_repetida_por_el_retroceso_toma_la_primera_ocurrencia()
    {
        var (i, _) = R("Europe/Madrid", "2026-10-25", "02:30", "10:00");
        Assert.Equal(U("2026-10-25T00:30:00Z"), i); // 02:30 CEST (primera vez)
    }

    [Fact]
    public void Canarias_tiene_su_propio_desfase()
    {
        var (i, _) = R("Atlantic/Canary", "2026-07-10", "08:00", "15:00");
        Assert.Equal(U("2026-07-10T07:00:00Z"), i); // UTC+1 en verano
    }

    [Fact]
    public void Se_recupera_la_hora_local_a_partir_del_instante()
    {
        var local = CalculadoraHorario.ALocal("Europe/Madrid", U("2026-10-25T07:00:00Z"));
        Assert.Equal(new DateTime(2026, 10, 25, 8, 0, 0), local);
    }

    [Theory]
    [InlineData("08:00", "15:00", "15:00", "22:00", false)] // contiguos: no se solapan
    [InlineData("08:00", "15:00", "14:59", "22:00", true)]
    public void Los_intervalos_contiguos_no_se_solapan(string ia, string fa, string ib, string fb, bool esperado)
    {
        DateTimeOffset T(string h) => DateTimeOffset.Parse($"2026-07-10T{h}:00Z");
        Assert.Equal(esperado, CalculadoraHorario.Solapan(T(ia), T(fa), T(ib), T(fb)));
    }
}

public class CuadranteTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid Org = Guid.NewGuid();
    private static Cuadrante Nuevo(int dias = 7) => new(Org, Guid.NewGuid(), "Semana", new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 2).AddDays(dias - 1), Ahora);

    [Fact]
    public void El_periodo_no_puede_ser_inverso_ni_superar_93_dias()
    {
        Assert.Throws<ExcepcionDominio>(() => new Cuadrante(Org, Guid.NewGuid(), "X", new DateOnly(2026, 11, 5), new DateOnly(2026, 11, 4), Ahora));
        Assert.Throws<ExcepcionDominio>(() => Nuevo(94));
        Assert.Equal(93, Nuevo(93).Dias);
    }

    [Fact]
    public void El_ciclo_de_vida_borrador_publicado_reabierto_publicado_incrementa_la_version()
    {
        var c = Nuevo();
        Assert.True(c.EsEditable);
        Assert.Equal(0, c.Version);

        c.Publicar(Guid.NewGuid(), Ahora);
        Assert.False(c.EsEditable);
        Assert.Equal(1, c.Version);
        Assert.Throws<ExcepcionDominio>(() => c.Publicar(Guid.NewGuid(), Ahora));

        c.Reabrir();
        Assert.True(c.EsEditable);
        c.Publicar(Guid.NewGuid(), Ahora);
        Assert.Equal(2, c.Version);
    }

    [Fact]
    public void Solo_se_archiva_un_cuadrante_publicado()
    {
        var c = Nuevo();
        Assert.Throws<ExcepcionDominio>(() => c.Archivar());
        c.Publicar(Guid.NewGuid(), Ahora);
        c.Archivar();
        Assert.Equal(EstadoCuadrante.Archivado, c.Estado);
        Assert.Throws<ExcepcionDominio>(() => c.Reabrir());
    }

    [Fact]
    public void Contiene_incluye_los_dos_extremos()
    {
        var c = Nuevo();
        Assert.True(c.Contiene(new DateOnly(2026, 11, 2)));
        Assert.True(c.Contiene(new DateOnly(2026, 11, 8)));
        Assert.False(c.Contiene(new DateOnly(2026, 11, 9)));
    }

    [Fact]
    public void La_asignacion_copia_el_horario_del_turno_y_se_puede_cancelar_una_vez()
    {
        var turno = new Turno(Org, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 11, 2),
            Ahora, Ahora.AddHours(7));
        var a = new Asignacion(Org, Guid.NewGuid(), turno, Guid.NewGuid(), Guid.NewGuid(), Ahora);
        Assert.True(a.EstaActiva);
        Assert.Equal(turno.InicioUtc, a.InicioUtc);
        Assert.Equal(turno.FinUtc, a.FinUtc);

        a.Cancelar(Ahora.AddHours(1));
        a.Cancelar(Ahora.AddHours(2));
        Assert.False(a.EstaActiva);
        Assert.Equal(Ahora.AddHours(1), a.FechaCancelacion);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Una_necesidad_exige_entre_1_y_50_plazas(int plazas)
        => Assert.Throws<ExcepcionDominio>(() => new NecesidadCobertura(Org, Guid.NewGuid(), plazas, null));

    [Fact]
    public void Un_turno_no_puede_terminar_antes_de_empezar()
        => Assert.Throws<ExcepcionDominio>(() => new Turno(Org, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 11, 2), Ahora, Ahora));
}
