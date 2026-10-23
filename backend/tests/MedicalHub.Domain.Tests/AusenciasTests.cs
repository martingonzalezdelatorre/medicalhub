using MedicalHub.Domain.Ausencias;
using MedicalHub.Domain.Comun;
using MedicalHub.Domain.Planificacion;

namespace MedicalHub.Domain.Tests;

public class AusenciaDominioTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid Org = Guid.NewGuid();

    private static Ausencia Nueva(string desde = "2026-12-21", string hasta = "2026-12-31")
        => new(Org, Guid.NewGuid(), TipoAusencia.Vacaciones, DateOnly.Parse(desde), DateOnly.Parse(hasta), null, Guid.NewGuid(), Ahora);

    [Fact]
    public void La_duracion_incluye_los_dos_extremos()
        => Assert.Equal(11, Nueva().Dias);

    [Fact]
    public void Las_fechas_no_pueden_ir_al_reves_ni_superar_un_ano()
    {
        Assert.Throws<ExcepcionDominio>(() => Nueva("2026-12-31", "2026-12-21"));
        Assert.Throws<ExcepcionDominio>(() => Nueva("2026-01-01", "2027-12-31"));
        Assert.Equal(366, Nueva("2026-01-01", "2027-01-01").Dias);
    }

    [Fact]
    public void El_comentario_se_limita_a_300_caracteres()
        => Assert.Throws<ExcepcionDominio>(() => new Ausencia(Org, Guid.NewGuid(), TipoAusencia.Otro, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 2),
            new string('x', 301), Guid.NewGuid(), Ahora));

    [Fact]
    public void Una_solicitud_se_aprueba_o_rechaza_una_sola_vez()
    {
        var a = Nueva();
        a.Aprobar();
        Assert.Equal(EstadoAusencia.Aprobada, a.Estado);
        Assert.Throws<ExcepcionDominio>(() => a.Aprobar());
        Assert.Throws<ExcepcionDominio>(() => a.Rechazar());

        var b = Nueva();
        b.Rechazar();
        Assert.False(b.EstaVigente);
        Assert.Throws<ExcepcionDominio>(() => b.Cancelar());
    }

    [Fact]
    public void Solo_las_vigentes_ocupan_fechas_y_se_pueden_cancelar()
    {
        var a = Nueva();
        Assert.True(a.EstaVigente);
        Assert.True(a.Solapa(new DateOnly(2026, 12, 31), new DateOnly(2027, 1, 5)));
        Assert.False(a.Solapa(new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 5)));
        a.Aprobar();
        a.Cancelar();
        Assert.False(a.EstaVigente);
    }
}

public class DisponibilidadDominioTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid Org = Guid.NewGuid();

    private static Disponibilidad Nueva(TipoDisponibilidad tipo = TipoDisponibilidad.Bloqueo, string desde = "2026-11-02", string hasta = "2026-11-08",
        int dias = DiasSemana.Todos, Guid? tipoTurno = null)
        => new(Org, Guid.NewGuid(), tipo, DateOnly.Parse(desde), DateOnly.Parse(hasta), dias, tipoTurno, null, Guid.NewGuid(), Ahora);

    [Theory]
    [InlineData(DayOfWeek.Monday, 1)]
    [InlineData(DayOfWeek.Tuesday, 2)]
    [InlineData(DayOfWeek.Saturday, 32)]
    [InlineData(DayOfWeek.Sunday, 64)]
    public void Cada_dia_de_la_semana_tiene_su_bit_empezando_en_lunes(DayOfWeek dia, int bit)
        => Assert.Equal(bit, DiasSemana.Bit(dia));

    [Fact]
    public void Se_exige_al_menos_un_dia_y_un_periodo_valido()
    {
        Assert.Throws<ExcepcionDominio>(() => Nueva(dias: 0));
        Assert.Throws<ExcepcionDominio>(() => Nueva(dias: 128));
        Assert.Throws<ExcepcionDominio>(() => Nueva(desde: "2026-11-08", hasta: "2026-11-02"));
    }

    [Fact]
    public void Afecta_al_turno_que_ocupa_algun_dia_del_periodo_y_cumple_dia_y_tipo()
    {
        var noche = Guid.NewGuid();
        var lunes = new DateOnly(2026, 11, 2);

        Assert.True(Nueva().Afecta(lunes, lunes, null));
        Assert.True(Nueva().Afecta(lunes.AddDays(-1), lunes, null));                    // empieza antes del periodo pero lo ocupa
        Assert.False(Nueva(desde: "2026-11-03").Afecta(lunes, lunes, null));
        Assert.True(Nueva(dias: DiasSemana.Bit(DayOfWeek.Monday)).Afecta(lunes, lunes, null));
        Assert.False(Nueva(dias: DiasSemana.Bit(DayOfWeek.Tuesday)).Afecta(lunes, lunes, null));
        Assert.True(Nueva(tipoTurno: noche).Afecta(lunes, lunes, noche));
        Assert.False(Nueva(tipoTurno: noche).Afecta(lunes, lunes, Guid.NewGuid()));
    }

    [Fact]
    public void La_declaracion_se_resuelve_una_sola_vez_y_registra_quien_decide()
    {
        var d = Nueva();
        var jefe = Guid.NewGuid();
        d.Aprobar(jefe, Ahora, "Ok");
        Assert.Equal(EstadoDisponibilidad.Aprobada, d.Estado);
        Assert.Equal(jefe, d.DecididaPorUsuarioId);
        Assert.Throws<ExcepcionDominio>(() => d.Rechazar(jefe, Ahora, null));
    }
}

public class VacanteDominioTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);

    private static Vacante Nueva() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Ausencia aprobada", Ahora);

    [Fact]
    public void Una_vacante_nace_abierta_y_se_cierra_una_sola_vez()
    {
        var v = Nueva();
        Assert.Equal(EstadoVacante.Abierta, v.Estado);
        var asignacion = Guid.NewGuid();
        v.Cubrir(asignacion, Ahora.AddHours(1));
        Assert.Equal(EstadoVacante.Cubierta, v.Estado);
        Assert.Equal(asignacion, v.AsignacionCubiertaId);
        Assert.Equal(Ahora.AddHours(1), v.FechaCierre);
        Assert.Throws<ExcepcionDominio>(() => v.Cubrir(Guid.NewGuid(), Ahora));
        Assert.Throws<ExcepcionDominio>(() => v.Cancelar(Ahora));
    }

    [Fact]
    public void El_motivo_es_obligatorio()
        => Assert.Throws<ExcepcionDominio>(() => new Vacante(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, " ", Ahora));
}
