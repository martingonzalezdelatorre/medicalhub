using MedicalHub.Application.Reglas;
using MedicalHub.Domain.Estructura;
using MedicalHub.Domain.Planificacion;
using MedicalHub.Domain.Profesionales;
using MedicalHub.Domain.Reglas;

namespace MedicalHub.Application.Tests;

public class MotorReglasTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid Org = Guid.NewGuid();
    private static readonly DateTimeOffset Lunes = new(2026, 11, 2, 0, 0, 0, TimeSpan.Zero);

    private static readonly Guid Categoria = Guid.NewGuid();
    private static readonly Guid Competencia = Guid.NewGuid();

    private static (Profesional prof, Servicio serv) Base(bool autorizado = true, bool conCompetencia = true, bool exigeCompetencia = false)
    {
        var serv = new Servicio(Org, Guid.NewGuid(), "Quirófano", null, Ahora);
        if (exigeCompetencia) serv.EstablecerCompetenciasRequeridas([Competencia]);
        var prof = new Profesional(Org, "Ana", "García", Categoria, Ahora);
        if (autorizado) prof.EstablecerServiciosAutorizados([serv.Id]);
        if (conCompetencia) prof.EstablecerCompetencias([(Competencia, null)]);
        return (prof, serv);
    }

    /// <summary>Turno que empieza `dia` días después del lunes a la hora indicada y dura `horas`.</summary>
    private static TurnoLigero T(Guid servicio, int dia, double hora, double horas, bool guardia = false, string? etiqueta = null)
    {
        var inicio = Lunes.AddDays(dia).AddHours(hora);
        return new TurnoLigero(Guid.NewGuid(), servicio, DateOnly.FromDateTime(inicio.UtcDateTime), inicio, inicio.AddHours(horas), guardia,
            etiqueta ?? $"Turno día {dia}");
    }

    private static AsignacionLigera A(TurnoLigero t) => new(Guid.NewGuid(), EstadoAsignacion.Confirmada, t, Ahora);

    private static List<Violacion> Evaluar(Profesional p, Servicio s, TurnoLigero turno, IEnumerable<TurnoLigero> otras, params ReglaEfectiva[] extra)
    {
        var reglas = MotorReglas.Efectivas([], s.Id).Concat(extra).ToList();
        return MotorReglas.EvaluarAsignacion(new ContextoEvaluacion
        {
            Profesional = p, Servicio = s, Turno = turno, Otras = otras.Select(A).ToList(),
            Competencias = new Dictionary<Guid, string> { [Competencia] = "Vía aérea" }, Servicios = new Dictionary<Guid, string> { [s.Id] = s.Nombre }
        }, reglas);
    }

    private static ReglaEfectiva R(TipoRegla tipo, string json, SeveridadRegla sev = SeveridadRegla.Bloqueante, bool excepcion = true, bool admin = false)
        => new(Guid.NewGuid(), tipo, $"Regla {tipo}", sev, excepcion, admin, json);

    // ── Reglas base y comportamiento por defecto ──

    [Fact]
    public void Un_profesional_que_cumple_todo_no_tiene_infracciones()
    {
        var (p, s) = Base(exigeCompetencia: true);
        Assert.Empty(Evaluar(p, s, T(s.Id, 0, 8, 7), []));
    }

    [Fact]
    public void Por_defecto_las_reglas_de_elegibilidad_son_bloqueantes_y_admiten_excepcion()
    {
        var (p, s) = Base(autorizado: false, conCompetencia: false, exigeCompetencia: true);
        var v = Evaluar(p, s, T(s.Id, 0, 8, 7), []);

        Assert.Contains(v, x => x.Codigo == "AutorizacionServicio");
        Assert.Contains(v, x => x.Codigo == "CompetenciasRequeridas" && x.Mensaje.Contains("Vía aérea"));
        Assert.All(v, x => { Assert.True(x.Bloquea); Assert.True(x.PermiteExcepcion); Assert.False(x.RequiereAdministrador); });
    }

    [Fact]
    public void La_sociedad_puede_rebajar_una_regla_base_a_advertencia_o_desactivarla()
    {
        var (p, s) = Base(autorizado: false);
        var aut = new ReglaPlanificacion(Org, TipoRegla.AutorizacionServicio, "Autorización", null, null, SeveridadRegla.Advertencia, false, false, null, null, Ahora);
        var v = MotorReglas.EvaluarAsignacion(new ContextoEvaluacion
        {
            Profesional = p, Servicio = s, Turno = T(s.Id, 0, 8, 7), Otras = [], Competencias = new Dictionary<Guid, string>(), Servicios = new Dictionary<Guid, string>()
        }, MotorReglas.Efectivas([aut], s.Id));
        Assert.Single(v, x => x.Codigo == "AutorizacionServicio" && !x.Bloquea);

        aut.Desactivar(); // desactivar una regla base la anula: no vuelve el comportamiento por defecto
        Assert.DoesNotContain(MotorReglas.Efectivas([aut], s.Id), r => r.Tipo == TipoRegla.AutorizacionServicio);
    }

    [Fact]
    public void Una_regla_de_un_servicio_no_se_aplica_a_otro()
    {
        var servicioA = Guid.NewGuid();
        var regla = new ReglaPlanificacion(Org, TipoRegla.DescansoMinimo, "Descanso", null, servicioA, SeveridadRegla.Bloqueante, true, false, """{"horas":12}""", null, Ahora);
        Assert.Contains(MotorReglas.Efectivas([regla], servicioA), r => r.Tipo == TipoRegla.DescansoMinimo);
        Assert.DoesNotContain(MotorReglas.Efectivas([regla], Guid.NewGuid()), r => r.Tipo == TipoRegla.DescansoMinimo);
    }

    [Fact]
    public void Los_limites_no_existen_hasta_que_la_sociedad_los_configura()
    {
        var tipos = MotorReglas.Efectivas([], Guid.NewGuid()).Select(r => r.Tipo).ToList();
        Assert.DoesNotContain(TipoRegla.DescansoMinimo, tipos);
        Assert.DoesNotContain(TipoRegla.MaxHorasPeriodo, tipos);
        Assert.DoesNotContain(TipoRegla.MaxGuardiasPeriodo, tipos);
        Assert.DoesNotContain(TipoRegla.MaxDiasConsecutivos, tipos);
        Assert.DoesNotContain(TipoRegla.IncompatibilidadServicios, tipos);
    }

    // ── Reglas intrínsecas ──

    [Fact]
    public void El_solapamiento_y_la_inactividad_nunca_admiten_excepcion()
    {
        var (p, s) = Base();
        var turno = T(s.Id, 0, 8, 7);
        var solapa = Evaluar(p, s, turno, [T(s.Id, 0, 14, 4)]);
        Assert.Contains(solapa, x => x.Codigo == "Solape" && x.Bloquea && !x.PermiteExcepcion);

        p.Desactivar();
        Assert.Contains(Evaluar(p, s, turno, []), x => x.Codigo == "ProfesionalInactivo" && !x.PermiteExcepcion);
    }

    [Fact]
    public void Turnos_contiguos_no_se_solapan()
    {
        var (p, s) = Base();
        Assert.DoesNotContain(Evaluar(p, s, T(s.Id, 0, 15, 7), [T(s.Id, 0, 8, 7)]), x => x.Codigo == "Solape");
    }

    // ── Descanso mínimo ──

    [Theory]
    [InlineData(10, true)]   // 10 h de hueco con 12 exigidas
    [InlineData(12, false)]  // exactamente el mínimo
    [InlineData(20, false)]
    public void El_descanso_minimo_se_mide_entre_el_fin_de_un_turno_y_el_inicio_del_siguiente(double hueco, bool infraccion)
    {
        var (p, s) = Base();
        var anterior = T(s.Id, 0, 8, 7);                       // 08:00–15:00
        var nuevo = T(s.Id, 0, 15 + hueco, 6);                  // empieza `hueco` horas después
        var v = Evaluar(p, s, nuevo, [anterior], R(TipoRegla.DescansoMinimo, """{"horas":12}"""));
        Assert.Equal(infraccion, v.Any(x => x.Codigo == "DescansoMinimo"));
    }

    [Fact]
    public void El_descanso_tambien_se_comprueba_hacia_adelante_y_el_mensaje_explica_el_motivo()
    {
        var (p, s) = Base();
        var siguiente = T(s.Id, 1, 6, 8, etiqueta: "Mañana del 03/11");
        var v = Evaluar(p, s, T(s.Id, 0, 22, 8), [siguiente], R(TipoRegla.DescansoMinimo, """{"horas":12}"""));
        var d = Assert.Single(v, x => x.Codigo == "DescansoMinimo");
        Assert.Contains("Mañana del 03/11", d.Mensaje);
        Assert.Contains("mínimo 12 h", d.Mensaje);
    }

    // ── Máximo de horas / guardias / días consecutivos ──

    [Fact]
    public void El_maximo_de_horas_se_evalua_en_cualquier_ventana_de_dias_consecutivos()
    {
        var (p, s) = Base();
        var regla = R(TipoRegla.MaxHorasPeriodo, """{"ventanaDias":7,"maxHoras":40}""");
        var otras = Enumerable.Range(0, 5).Select(d => T(s.Id, d, 8, 8)).ToList(); // 5 × 8 h = 40 h (lunes a viernes)

        Assert.DoesNotContain(Evaluar(p, s, T(s.Id, 5, 8, 0.0001), [], regla), x => x.Codigo == "MaxHorasPeriodo");
        // 40 h es el máximo: no hay infracción con el quinto; con un sexto turno sí
        Assert.DoesNotContain(Evaluar(p, s, otras[4], otras.Take(4), regla), x => x.Codigo == "MaxHorasPeriodo");
        var sexto = Evaluar(p, s, T(s.Id, 5, 8, 8), otras, regla);
        var infraccion = Assert.Single(sexto, x => x.Codigo == "MaxHorasPeriodo");
        Assert.Contains("48 h", infraccion.Mensaje);
        Assert.Contains("máximo: 40 h", infraccion.Mensaje);
    }

    [Fact]
    public void Fuera_de_la_ventana_no_cuenta()
    {
        var (p, s) = Base();
        var regla = R(TipoRegla.MaxHorasPeriodo, """{"ventanaDias":3,"maxHoras":16}""");
        // Días 0,1 con 8 h y día 5: ningún intervalo de 3 días reúne más de 16 h
        Assert.Empty(Evaluar(p, s, T(s.Id, 5, 8, 8), [T(s.Id, 0, 8, 8), T(s.Id, 1, 8, 8)], regla).Where(x => x.Codigo == "MaxHorasPeriodo"));
        Assert.Single(Evaluar(p, s, T(s.Id, 2, 8, 8), [T(s.Id, 0, 8, 8), T(s.Id, 1, 8, 8)], regla).Where(x => x.Codigo == "MaxHorasPeriodo"));
    }

    [Fact]
    public void El_maximo_de_guardias_solo_cuenta_las_guardias()
    {
        var (p, s) = Base();
        var regla = R(TipoRegla.MaxGuardiasPeriodo, """{"ventanaDias":7,"maxGuardias":2}""");
        var otras = new[] { T(s.Id, 0, 8, 24, guardia: true), T(s.Id, 2, 8, 24, guardia: true), T(s.Id, 4, 8, 7, guardia: false) };
        Assert.DoesNotContain(Evaluar(p, s, T(s.Id, 5, 8, 7, guardia: false), otras, regla), x => x.Codigo == "MaxGuardiasPeriodo");
        Assert.Contains(Evaluar(p, s, T(s.Id, 6, 8, 24, guardia: true), otras, regla), x => x.Codigo == "MaxGuardiasPeriodo");
    }

    [Fact]
    public void Los_dias_consecutivos_se_cuentan_alrededor_del_turno_evaluado()
    {
        var (p, s) = Base();
        var regla = R(TipoRegla.MaxDiasConsecutivos, """{"maxDias":3}""");
        var otras = new[] { T(s.Id, 0, 8, 7), T(s.Id, 1, 8, 7), T(s.Id, 3, 8, 7) };

        Assert.Empty(Evaluar(p, s, T(s.Id, 5, 8, 7), otras, regla).Where(x => x.Codigo == "MaxDiasConsecutivos"));
        // el día 2 une 0-1 con 3 → 4 días seguidos
        var v = Assert.Single(Evaluar(p, s, T(s.Id, 2, 8, 7), otras, regla), x => x.Codigo == "MaxDiasConsecutivos");
        Assert.Contains("4 días consecutivos", v.Mensaje);
    }

    // ── Incompatibilidad entre servicios ──

    [Fact]
    public void La_incompatibilidad_entre_servicios_aplica_el_margen_en_ambos_sentidos()
    {
        var (p, s) = Base();
        var otroServicio = Guid.NewGuid();
        var regla = R(TipoRegla.IncompatibilidadServicios, $$"""{"servicioAId":"{{s.Id}}","servicioBId":"{{otroServicio}}","margenHoras":24}""");

        var enOtro = T(otroServicio, 0, 8, 7);
        Assert.Contains(Evaluar(p, s, T(s.Id, 1, 6, 7), [enOtro], regla), x => x.Codigo == "IncompatibilidadServicios");   // 15 h < 24 h
        Assert.DoesNotContain(Evaluar(p, s, T(s.Id, 2, 8, 7), [enOtro], regla), x => x.Codigo == "IncompatibilidadServicios"); // 41 h
        // un turno del mismo servicio no cuenta
        Assert.DoesNotContain(Evaluar(p, s, T(s.Id, 1, 6, 7), [T(s.Id, 0, 8, 7)], regla), x => x.Codigo == "IncompatibilidadServicios");
    }

    // ── Severidad y excepciones configurables ──

    [Fact]
    public void La_severidad_y_la_posibilidad_de_excepcion_las_fija_la_regla()
    {
        var (p, s) = Base();
        var anterior = T(s.Id, 0, 8, 7);
        var nuevo = T(s.Id, 0, 16, 6);
        var datos = """{"horas":12}""";

        var aviso = Evaluar(p, s, nuevo, [anterior], R(TipoRegla.DescansoMinimo, datos, SeveridadRegla.Advertencia, excepcion: true)).Single(x => x.Codigo == "DescansoMinimo");
        Assert.False(aviso.Bloquea);

        var firme = Evaluar(p, s, nuevo, [anterior], R(TipoRegla.DescansoMinimo, datos, SeveridadRegla.Bloqueante, excepcion: false)).Single(x => x.Codigo == "DescansoMinimo");
        Assert.True(firme.Bloquea);
        Assert.False(firme.PermiteExcepcion);

        var admin = Evaluar(p, s, nuevo, [anterior], R(TipoRegla.DescansoMinimo, datos, admin: true)).Single(x => x.Codigo == "DescansoMinimo");
        Assert.True(admin.PermiteExcepcion);
        Assert.True(admin.RequiereAdministrador);
    }

    [Fact]
    public void Una_advertencia_no_puede_requerir_excepcion()
    {
        var r = new ReglaPlanificacion(Org, TipoRegla.DescansoMinimo, "X", null, null, SeveridadRegla.Advertencia, true, true, """{"horas":12}""", null, Ahora);
        Assert.False(r.PermiteExcepcion);
        Assert.False(r.ExcepcionRequiereAdministrador);
    }

    [Fact]
    public void Modificar_una_regla_exige_revisarla_de_nuevo()
    {
        var r = new ReglaPlanificacion(Org, TipoRegla.DescansoMinimo, "X", null, null, SeveridadRegla.Bloqueante, true, false, """{"horas":12}""", "Convenio", Ahora);
        Assert.Null(r.RevisadaEl);
        r.MarcarRevisada(Guid.NewGuid(), Ahora);
        Assert.NotNull(r.RevisadaEl);
        r.Actualizar("X", null, null, SeveridadRegla.Bloqueante, true, false, """{"horas":11}""", "Convenio");
        Assert.Null(r.RevisadaEl);
    }

    // ── Reglas de cuadrante ──

    [Theory]
    [InlineData(4, 4, 100, false)]
    [InlineData(3, 4, 100, true)]
    [InlineData(3, 4, 75, false)]
    [InlineData(2, 4, 75, true)]
    public void La_cobertura_minima_se_expresa_en_porcentaje_de_plazas_cubiertas(int cubiertas, int plazas, int minimo, bool infraccion)
    {
        var r = R(TipoRegla.CoberturaMinima, $$"""{"porcentajeMinimo":{{minimo}}}""");
        Assert.Equal(infraccion, MotorReglas.EvaluarCobertura(plazas, cubiertas, "Mañana", [r]).Any());
    }

    [Fact]
    public void Por_defecto_la_cobertura_incompleta_es_una_advertencia_y_lo_pendiente_tambien()
    {
        var reglas = MotorReglas.Efectivas([], Guid.NewGuid());
        var (regla, _) = Assert.Single(MotorReglas.EvaluarCobertura(2, 1, "Noche", reglas));
        Assert.Equal(SeveridadRegla.Advertencia, regla.Severidad);

        Assert.Single(MotorReglas.EvaluarPendiente(EstadoAsignacion.PendienteAprobacion, "Ana", "Noche", reglas));
        Assert.Empty(MotorReglas.EvaluarPendiente(EstadoAsignacion.Confirmada, "Ana", "Noche", reglas));
    }

    [Fact]
    public void El_horizonte_de_carga_crece_con_las_ventanas_configuradas()
    {
        Assert.Equal(3, MotorReglas.HorizonteDias([]));
        var r = new ReglaPlanificacion(Org, TipoRegla.MaxHorasPeriodo, "X", null, null, SeveridadRegla.Bloqueante, true, false, """{"ventanaDias":28,"maxHoras":160}""", null, Ahora);
        Assert.Equal(30, MotorReglas.HorizonteDias([r]));
    }
}
