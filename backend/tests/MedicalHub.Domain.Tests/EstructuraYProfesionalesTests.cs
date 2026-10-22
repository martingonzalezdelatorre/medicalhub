using MedicalHub.Domain.Catalogos;
using MedicalHub.Domain.Comun;
using MedicalHub.Domain.Estructura;
using MedicalHub.Domain.Profesionales;

namespace MedicalHub.Domain.Tests;

public class EstructuraYProfesionalesTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid Org = Guid.NewGuid();
    private static readonly DateOnly Hoy = new(2026, 10, 22);

    [Fact]
    public void El_nombre_de_un_catalogo_es_obligatorio_y_se_recorta()
    {
        Assert.Throws<ExcepcionDominio>(() => Especialidad.Crear(Org, "  ", null, Ahora));
        Assert.Equal("Anestesia", Especialidad.Crear(Org, "  Anestesia ", null, Ahora).Nombre);
    }

    [Fact]
    public void Una_entidad_no_puede_crearse_sin_organizacion()
        => Assert.Throws<ExcepcionDominio>(() => Especialidad.Crear(Guid.Empty, "X", null, Ahora));

    [Fact]
    public void El_centro_valida_la_zona_horaria_iana()
    {
        Assert.Throws<ExcepcionDominio>(() => new Centro(Org, "Hospital", TipoCentro.Hospital, null, "Mordor/Orodruin", Ahora));
        Assert.Equal("Atlantic/Canary", new Centro(Org, "Hospital", TipoCentro.Hospital, null, "Atlantic/Canary", Ahora).ZonaHoraria);
    }

    [Theory]
    [InlineData(8, 15, false, 7)]    // mañana
    [InlineData(22, 8, true, 10)]    // noche: termina al día siguiente
    [InlineData(8, 8, true, 24)]     // guardia de 24 h
    [InlineData(0, 12, false, 12)]
    public void El_tipo_de_turno_calcula_cruce_de_medianoche_y_duracion(int inicio, int fin, bool cruza, double horas)
    {
        var t = new TipoTurno(Org, Guid.NewGuid(), "Turno", new TimeOnly(inicio, 0), new TimeOnly(fin, 0), false);
        Assert.Equal(cruza, t.CruzaMedianoche);
        Assert.Equal(horas, t.Duracion.TotalHours);
    }

    [Fact]
    public void La_configuracion_de_reglas_debe_ser_json_valido()
    {
        var s = new Servicio(Org, Guid.NewGuid(), "Quirófano", null, Ahora);
        Assert.Throws<ExcepcionDominio>(() => s.EstablecerConfiguracionReglas("{no es json"));
        s.EstablecerConfiguracionReglas("""{"maxHorasSemana": 48}""");
        Assert.NotNull(s.ConfiguracionReglas);
        s.EstablecerConfiguracionReglas(" ");
        Assert.Null(s.ConfiguracionReglas);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void El_requisito_exige_entre_1_y_50_profesionales(int n)
        => Assert.Throws<ExcepcionDominio>(() => new RequisitoCobertura(Org, Guid.NewGuid(), Guid.NewGuid(), n, null));

    [Fact]
    public void Establecer_colecciones_conserva_lo_existente_y_aplica_la_diferencia()
    {
        var s = new Servicio(Org, Guid.NewGuid(), "UCI", null, Ahora);
        Guid a = Guid.NewGuid(), b = Guid.NewGuid(), c = Guid.NewGuid();
        s.EstablecerCategoriasAdmitidas([a, b]);
        s.EstablecerCategoriasAdmitidas([b, c, c]);
        Assert.Equal(new[] { b, c }.Order(), s.CategoriasAdmitidas.Select(x => x.CategoriaProfesionalId).Order());
    }

    // ── Elegibilidad ──

    private static (Profesional p, Servicio s, Guid categoria, Guid competencia) Escenario()
    {
        var categoria = Guid.NewGuid();
        var competencia = Guid.NewGuid();
        var s = new Servicio(Org, Guid.NewGuid(), "Quirófano 1", null, Ahora);
        s.EstablecerCategoriasAdmitidas([categoria]);
        s.EstablecerCompetenciasRequeridas([competencia]);

        var p = new Profesional(Org, "Ana", "García", categoria, Ahora);
        p.EstablecerServiciosAutorizados([s.Id]);
        p.EstablecerCompetencias([(competencia, null)]);
        return (p, s, categoria, competencia);
    }

    [Fact]
    public void Un_profesional_que_cumple_todo_es_elegible()
    {
        var (p, s, _, _) = Escenario();
        var r = EvaluadorElegibilidad.Evaluar(p, s, Hoy);
        Assert.True(r.Elegible);
        Assert.Empty(r.Motivos);
    }

    [Fact]
    public void Sin_autorizacion_para_el_servicio_no_es_elegible()
    {
        var (p, s, _, _) = Escenario();
        p.EstablecerServiciosAutorizados([]);
        var r = EvaluadorElegibilidad.Evaluar(p, s, Hoy);
        Assert.False(r.Elegible);
        Assert.Contains(r.Motivos, m => m.Contains("autorizado"));
    }

    [Fact]
    public void Una_categoria_no_admitida_impide_la_asignacion()
    {
        var (p, s, _, _) = Escenario();
        s.EstablecerCategoriasAdmitidas([Guid.NewGuid()]);
        Assert.Contains(EvaluadorElegibilidad.Evaluar(p, s, Hoy).Motivos, m => m.Contains("categoría"));
    }

    [Fact]
    public void Un_servicio_sin_categorias_admite_cualquiera()
    {
        var (p, s, _, _) = Escenario();
        s.EstablecerCategoriasAdmitidas([]);
        Assert.True(EvaluadorElegibilidad.Evaluar(p, s, Hoy).Elegible);
    }

    [Fact]
    public void Falta_de_competencia_y_competencia_caducada_se_distinguen()
    {
        var (p, s, _, comp) = Escenario();

        p.EstablecerCompetencias([]);
        Assert.Contains(EvaluadorElegibilidad.Evaluar(p, s, Hoy).Motivos, m => m.StartsWith("Falta"));

        p.EstablecerCompetencias([(comp, new DateOnly(2026, 10, 21))]);
        Assert.Contains(EvaluadorElegibilidad.Evaluar(p, s, Hoy).Motivos, m => m.Contains("caducada"));

        // El último día de vigencia todavía vale (inclusive)
        p.EstablecerCompetencias([(comp, Hoy)]);
        Assert.True(EvaluadorElegibilidad.Evaluar(p, s, Hoy).Elegible);
        Assert.False(EvaluadorElegibilidad.Evaluar(p, s, Hoy.AddDays(1)).Elegible);
    }

    [Fact]
    public void Un_profesional_o_servicio_inactivo_no_es_elegible()
    {
        var (p, s, _, _) = Escenario();
        p.Desactivar();
        Assert.Contains(EvaluadorElegibilidad.Evaluar(p, s, Hoy).Motivos, m => m.Contains("profesional está inactivo"));
        p.Activar();
        s.Desactivar();
        Assert.Contains(EvaluadorElegibilidad.Evaluar(p, s, Hoy).Motivos, m => m.Contains("servicio está inactivo"));
    }

    [Fact]
    public void Los_datos_de_contacto_se_validan()
    {
        var p = new Profesional(Org, "Ana", "García", Guid.NewGuid(), Ahora);
        Assert.Throws<ExcepcionDominio>(() => p.ActualizarDatos("Ana", "García", "sin-arroba", null, null, Guid.NewGuid()));
        p.ActualizarDatos("Ana", "García", " ANA@Sociedad.es ", "600 000 000", "  ", Guid.NewGuid());
        Assert.Equal("ana@sociedad.es", p.Email);
        Assert.Null(p.NumeroColegiado);
    }
}
