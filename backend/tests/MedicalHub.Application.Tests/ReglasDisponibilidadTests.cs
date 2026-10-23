using MedicalHub.Application.Reglas;
using MedicalHub.Domain.Ausencias;
using MedicalHub.Domain.Estructura;
using MedicalHub.Domain.Profesionales;
using MedicalHub.Domain.Reglas;

namespace MedicalHub.Application.Tests;

public class ReglasDisponibilidadTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid Org = Guid.NewGuid();
    private static readonly Guid Categoria = Guid.NewGuid();
    private static readonly Guid Noche = Guid.NewGuid();

    private static (Profesional, Servicio) Base()
    {
        var servicio = new Servicio(Org, Guid.NewGuid(), "Quirófano", null, Ahora);
        var prof = new Profesional(Org, "Ana", "García", Categoria, Ahora);
        prof.EstablecerServiciosAutorizados([servicio.Id]);
        return (prof, servicio);
    }

    /// <summary>Turno que empieza el día indicado y termina `diasQueOcupa - 1` días después.</summary>
    private static TurnoLigero Turno(Guid servicio, string fecha, int diasQueOcupa = 1, Guid? tipo = null)
    {
        var f = DateOnly.Parse(fecha);
        var inicio = new DateTimeOffset(f.ToDateTime(new TimeOnly(22, 0)), TimeSpan.Zero);
        return new TurnoLigero(Guid.NewGuid(), servicio, f, inicio, inicio.AddHours(10), false, "Noche", f.AddDays(diasQueOcupa - 1), tipo);
    }

    private static List<Violacion> Evaluar(Profesional p, Servicio s, TurnoLigero t, IEnumerable<AusenciaLigera>? ausencias = null,
        IEnumerable<DisponibilidadLigera>? disponibilidades = null, params ReglaEfectiva[] extra)
        => MotorReglas.EvaluarAsignacion(new ContextoEvaluacion
        {
            Profesional = p, Servicio = s, Turno = t, Otras = [], Competencias = new Dictionary<Guid, string>(), Servicios = new Dictionary<Guid, string>(),
            Ausencias = ausencias?.ToList() ?? [], Disponibilidades = disponibilidades?.ToList() ?? []
        }, MotorReglas.Efectivas([], s.Id).Concat(extra).ToList());

    private static AusenciaLigera Aus(string desde, string hasta, TipoAusencia tipo = TipoAusencia.Vacaciones) => new(tipo, DateOnly.Parse(desde), DateOnly.Parse(hasta));

    private static DisponibilidadLigera Disp(TipoDisponibilidad tipo, string desde, string hasta, int dias = DiasSemana.Todos, Guid? tipoTurno = null)
        => new(tipo, DateOnly.Parse(desde), DateOnly.Parse(hasta), dias, tipoTurno, null);

    [Fact]
    public void Una_ausencia_aprobada_que_cubre_el_turno_es_una_infraccion_bloqueante_con_excepcion_posible_por_defecto()
    {
        var (p, s) = Base();
        var v = Assert.Single(Evaluar(p, s, Turno(s.Id, "2026-11-04"), [Aus("2026-11-03", "2026-11-05")]), x => x.Codigo == "AusenciaAprobada");
        Assert.True(v.Bloquea);
        Assert.True(v.PermiteExcepcion);
        Assert.Contains("vacaciones", v.Mensaje);
        Assert.Contains("03/11/2026", v.Mensaje);
    }

    [Theory]
    [InlineData("2026-11-02", 1, false)]   // el día anterior
    [InlineData("2026-11-02", 2, true)]    // una noche que empieza el 02 y acaba el 03 toca la ausencia
    [InlineData("2026-11-05", 1, false)]   // el día siguiente
    [InlineData("2026-11-03", 1, true)]
    public void El_turno_se_ve_afectado_si_ocupa_cualquier_dia_de_la_ausencia(string inicio, int dias, bool afectado)
    {
        var (p, s) = Base();
        var v = Evaluar(p, s, Turno(s.Id, inicio, dias), [Aus("2026-11-03", "2026-11-04")]);
        Assert.Equal(afectado, v.Any(x => x.Codigo == "AusenciaAprobada"));
    }

    [Fact]
    public void Solo_el_bloqueo_aprobado_infringe_y_la_preferencia_es_una_advertencia()
    {
        var (p, s) = Base();
        var turno = Turno(s.Id, "2026-11-04");

        var bloqueo = Evaluar(p, s, turno, null, [Disp(TipoDisponibilidad.Bloqueo, "2026-11-04", "2026-11-04")]).Single(x => x.Codigo == "DisponibilidadBloqueada");
        Assert.True(bloqueo.Bloquea);

        var evita = Evaluar(p, s, turno, null, [Disp(TipoDisponibilidad.Evita, "2026-11-04", "2026-11-04")]).Single(x => x.Codigo == "PreferenciaEvitar");
        Assert.False(evita.Bloquea);

        // Ni «disponible» ni «prefiere» son infracciones
        Assert.Empty(Evaluar(p, s, turno, null, [Disp(TipoDisponibilidad.Disponible, "2026-11-04", "2026-11-04"), Disp(TipoDisponibilidad.Prefiere, "2026-11-04", "2026-11-04")]));
    }

    [Fact]
    public void La_sociedad_puede_hacer_obligatoria_una_preferencia_o_ignorarla()
    {
        var (p, s) = Base();
        var turno = Turno(s.Id, "2026-11-04");
        var evita = Disp(TipoDisponibilidad.Evita, "2026-11-04", "2026-11-04");

        var dura = new ReglaPlanificacion(Org, TipoRegla.PreferenciaEvitar, "Respetar", null, null, SeveridadRegla.Bloqueante, true, false, null, null, Ahora);
        var v = MotorReglas.EvaluarAsignacion(Contexto(p, s, turno, evita), MotorReglas.Efectivas([dura], s.Id)).Single(x => x.Codigo == "PreferenciaEvitar");
        Assert.True(v.Bloquea);

        dura.Desactivar();
        Assert.DoesNotContain(MotorReglas.EvaluarAsignacion(Contexto(p, s, turno, evita), MotorReglas.Efectivas([dura], s.Id)), x => x.Codigo == "PreferenciaEvitar");
    }

    private static ContextoEvaluacion Contexto(Profesional p, Servicio s, TurnoLigero t, DisponibilidadLigera d) => new()
    {
        Profesional = p, Servicio = s, Turno = t, Otras = [], Competencias = new Dictionary<Guid, string>(), Servicios = new Dictionary<Guid, string>(), Disponibilidades = [d]
    };

    [Fact]
    public void El_bloqueo_por_dia_de_la_semana_y_por_tipo_de_turno_solo_afecta_a_lo_indicado()
    {
        var (p, s) = Base();
        var lunes = Turno(s.Id, "2026-11-02", tipo: Noche);       // lunes
        var martes = Turno(s.Id, "2026-11-03", tipo: Noche);
        var lunesOtroTipo = Turno(s.Id, "2026-11-02", tipo: Guid.NewGuid());
        var bloqueo = Disp(TipoDisponibilidad.Bloqueo, "2026-11-01", "2026-11-08", DiasSemana.Bit(DayOfWeek.Monday), Noche);

        Assert.Contains(Evaluar(p, s, lunes, null, [bloqueo]), x => x.Codigo == "DisponibilidadBloqueada");
        Assert.DoesNotContain(Evaluar(p, s, martes, null, [bloqueo]), x => x.Codigo == "DisponibilidadBloqueada");
        Assert.DoesNotContain(Evaluar(p, s, lunesOtroTipo, null, [bloqueo]), x => x.Codigo == "DisponibilidadBloqueada");
    }

    [Fact]
    public void Las_notas_positivas_recogen_preferencias_y_disponibilidad_declarada()
    {
        var (p, s) = Base();
        var turno = Turno(s.Id, "2026-11-04");
        var ctx = new ContextoEvaluacion
        {
            Profesional = p, Servicio = s, Turno = turno, Otras = [], Competencias = new Dictionary<Guid, string>(), Servicios = new Dictionary<Guid, string>(),
            Disponibilidades = [Disp(TipoDisponibilidad.Prefiere, "2026-11-04", "2026-11-04"), Disp(TipoDisponibilidad.Disponible, "2026-11-01", "2026-11-30"), Disp(TipoDisponibilidad.Bloqueo, "2026-11-04", "2026-11-04")]
        };
        var notas = NotasEvaluacion.Positivas(ctx);
        Assert.Equal(2, notas.Count);
        Assert.Contains("Prefiere este turno.", notas);
    }

    [Fact]
    public void El_fin_local_de_un_turno_a_medianoche_no_ocupa_el_dia_siguiente()
    {
        // 00:00 del día 5 en Madrid = 23:00Z del día 4 (horario de invierno): el turno termina justo al empezar el 5
        var fin = new DateTimeOffset(2026, 11, 4, 23, 0, 0, TimeSpan.Zero);
        Assert.Equal(new DateOnly(2026, 11, 4), EvaluacionServicio.FechaFinLocal("Europe/Madrid", new DateOnly(2026, 11, 4), fin));
        Assert.Equal(new DateOnly(2026, 11, 5), EvaluacionServicio.FechaFinLocal("Europe/Madrid", new DateOnly(2026, 11, 4), fin.AddHours(1)));
    }
}
