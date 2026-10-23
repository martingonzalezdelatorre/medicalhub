using MedicalHub.Domain.Comun;
using MedicalHub.Domain.Horas;

namespace MedicalHub.Domain.Tests;

public class HorasTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 23, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid Org = Guid.NewGuid();
    private const string Madrid = "Europe/Madrid";

    [Theory]
    [InlineData("2026-10-05T20:00:00Z", "2026-10-06T06:00:00Z", 480)]   // noche 22:00–08:00 local (horario de verano): 8 h nocturnas
    [InlineData("2026-10-06T06:00:00Z", "2026-10-06T13:00:00Z", 0)]     // mañana 08:00–15:00 local: ninguna
    [InlineData("2026-10-06T03:00:00Z", "2026-10-06T10:00:00Z", 60)]    // 05:00–12:00 local: solo de 05:00 a 06:00
    [InlineData("2026-10-06T19:00:00Z", "2026-10-06T21:00:00Z", 60)]    // 21:00–23:00 local: solo de 22:00 a 23:00
    public void Las_horas_nocturnas_se_miden_en_hora_local_del_centro(string inicio, string fin, int esperado)
        => Assert.Equal(esperado, CalculadoraHoras.MinutosNocturnos(Madrid, DateTimeOffset.Parse(inicio), DateTimeOffset.Parse(fin)));

    [Fact]
    public void Un_turno_nocturno_que_cruza_el_cambio_de_hora_de_otono_dura_11_horas_y_9_son_nocturnas()
    {
        // 24/10/2026 22:00 (CEST, 20:00Z) → 25/10/2026 08:00 (CET, 07:00Z): el 25 de octubre la hora se repite
        var inicio = new DateTimeOffset(2026, 10, 24, 20, 0, 0, TimeSpan.Zero);
        var fin = new DateTimeOffset(2026, 10, 25, 7, 0, 0, TimeSpan.Zero);
        Assert.Equal(660, CalculadoraHoras.MinutosTotales(inicio, fin));
        Assert.Equal(540, CalculadoraHoras.MinutosNocturnos(Madrid, inicio, fin));
    }

    [Fact]
    public void Un_turno_nocturno_en_el_cambio_de_hora_de_primavera_dura_9_horas_y_7_son_nocturnas()
    {
        // 27/03/2027 22:00 (CET, 21:00Z) → 28/03/2027 08:00 (CEST, 06:00Z)
        var inicio = new DateTimeOffset(2027, 3, 27, 21, 0, 0, TimeSpan.Zero);
        var fin = new DateTimeOffset(2027, 3, 28, 6, 0, 0, TimeSpan.Zero);
        Assert.Equal(540, CalculadoraHoras.MinutosTotales(inicio, fin));
        Assert.Equal(420, CalculadoraHoras.MinutosNocturnos(Madrid, inicio, fin));
    }

    [Fact]
    public void Un_intervalo_vacio_o_invertido_no_suma_nada()
    {
        var t = new DateTimeOffset(2026, 10, 5, 20, 0, 0, TimeSpan.Zero);
        Assert.Equal(0, CalculadoraHoras.MinutosTotales(t, t));
        Assert.Equal(0, CalculadoraHoras.MinutosNocturnos(Madrid, t, t.AddHours(-1)));
    }

    [Fact]
    public void El_registro_valida_el_intervalo_y_se_puede_corregir()
    {
        var inicio = Ahora.AddHours(-10);
        var r = new RegistroHoras(Org, Guid.NewGuid(), Guid.NewGuid(), inicio, Ahora, Guid.NewGuid(), Ahora);
        Assert.Equal(600, r.Minutos);
        r.Actualizar(inicio, Ahora.AddMinutes(-30), Guid.NewGuid(), Ahora);
        Assert.Equal(570, r.Minutos);
        Assert.Throws<ExcepcionDominio>(() => r.Actualizar(Ahora, Ahora, Guid.NewGuid(), Ahora));
        Assert.Throws<ExcepcionDominio>(() => r.Actualizar(Ahora.AddHours(-25), Ahora, Guid.NewGuid(), Ahora));
    }

    [Fact]
    public void El_ajuste_exige_minutos_dentro_de_rango_y_un_motivo_suficiente()
    {
        var reg = Guid.NewGuid();
        var u = Guid.NewGuid();
        var a = new AjusteHoras(Org, reg, -30, "Salió media hora antes por acuerdo", u, Ahora);
        Assert.Equal(-30, a.Minutos);
        Assert.Throws<ExcepcionDominio>(() => new AjusteHoras(Org, reg, 0, "Motivo suficiente", u, Ahora));
        Assert.Throws<ExcepcionDominio>(() => new AjusteHoras(Org, reg, AjusteHoras.MaxMinutos + 1, "Motivo suficiente", u, Ahora));
        Assert.Throws<ExcepcionDominio>(() => new AjusteHoras(Org, reg, 15, "corto", u, Ahora));
        Assert.Throws<ExcepcionDominio>(() => new AjusteHoras(Org, reg, 15, " ", u, Ahora));
    }
}
