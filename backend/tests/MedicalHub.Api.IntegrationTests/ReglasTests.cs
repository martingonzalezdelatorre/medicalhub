using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MedicalHub.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalHub.Api.IntegrationTests;

[Collection(ColeccionApi.Nombre)]
public class ReglasTests(FabricaApi fabrica)
{
    private const string Justificacion = "Cobertura urgente por baja imprevista del titular.";

    private static async Task<JsonElement> CrearReglaAsync(HttpClient c, string tipo, object? parametros = null, string severidad = "Bloqueante",
        bool permiteExcepcion = true, bool requiereAdmin = false, Guid? servicio = null, string nombre = "Regla de prueba")
    {
        var r = await c.PostAsJsonAsync("/api/reglas-planificacion", new
        {
            tipo, nombre, servicioId = servicio, severidad, permiteExcepcion, excepcionRequiereAdministrador = requiereAdmin,
            parametros, fundamento = "Convenio de prueba"
        });
        return await Planif.LeerAsync<JsonElement>(r, HttpStatusCode.Created);
    }

    private static object Descanso(int horas) => new { horas };

    /// <summary>Profesional con noche del lunes (hasta las 08:00 del martes) y la mañana del martes libre para asignar.</summary>
    private async Task<(Escenario e, Guid prof, Guid manana)> NocheYMananaAsync(string tipoRegla = "DescansoMinimo", object? parametros = null,
        string severidad = "Bloqueante", bool permite = true, bool admin = false)
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-03");
        await CrearReglaAsync(e.C, tipoRegla, parametros ?? Descanso(12), severidad, permite, admin);
        var prof = await e.ProfesionalAsync();
        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-02", e.Noche), prof);
        return (e, prof, await e.NecesidadAsync("2026-11-03", e.Manana));
    }

    // ── Gestión de reglas ──

    [Fact]
    public async Task El_catalogo_de_tipos_describe_los_parametros_de_cada_regla()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var tipos = await e.C.GetFromJsonAsync<List<JsonElement>>("/api/reglas-planificacion/tipos", AyudaSesion.Json);
        Assert.Equal(11, tipos!.Count);
        var maxHoras = tipos.Single(t => t.GetProperty("tipo").GetString() == "MaxHorasPeriodo");
        Assert.Equal(["ventanaDias", "maxHoras"], maxHoras.GetProperty("parametros").EnumerateArray().Select(p => p.GetProperty("clave").GetString()));
        Assert.Contains("No se aplica", tipos.Single(t => t.GetProperty("tipo").GetString() == "DescansoMinimo").GetProperty("comportamientoPorDefecto").GetString());
    }

    [Fact]
    public async Task Crear_una_regla_valida_sus_parametros_segun_el_tipo()
    {
        var e = await Escenario.CrearAsync(fabrica);

        var regla = await CrearReglaAsync(e.C, "DescansoMinimo", Descanso(11));
        Assert.Equal(11, regla.GetProperty("parametros").GetProperty("horas").GetInt32());
        Assert.True(regla.GetProperty("pendienteDeRevision").GetBoolean());

        async Task<HttpResponseMessage> Intentar(string tipo, object? p) => await e.C.PostAsJsonAsync("/api/reglas-planificacion",
            new { tipo, nombre = "X", severidad = "Bloqueante", permiteExcepcion = true, parametros = p });
        Assert.Equal(HttpStatusCode.BadRequest, (await Intentar("DescansoMinimo", null)).StatusCode);                          // falta el parámetro
        Assert.Equal(HttpStatusCode.BadRequest, (await Intentar("DescansoMinimo", new { horas = 500 })).StatusCode);            // fuera de rango
        Assert.Equal(HttpStatusCode.BadRequest, (await Intentar("DescansoMinimo", new { horas = "doce" })).StatusCode);         // tipo erróneo
        Assert.Equal(HttpStatusCode.BadRequest, (await Intentar("MaxHorasPeriodo", new { ventanaDias = 7 })).StatusCode);       // falta maxHoras
        Assert.Equal(HttpStatusCode.BadRequest, (await Intentar("IncompatibilidadServicios",
            new { servicioAId = e.Servicio, servicioBId = e.Servicio, margenHoras = 12 })).StatusCode);                         // servicios iguales
        Assert.Equal(HttpStatusCode.BadRequest, (await Intentar("IncompatibilidadServicios",
            new { servicioAId = e.Servicio, servicioBId = Guid.NewGuid(), margenHoras = 12 })).StatusCode);                     // servicio inexistente
        Assert.Equal(HttpStatusCode.Created, (await Intentar("CompetenciasRequeridas", null)).StatusCode);                      // sin parámetros
    }

    [Fact]
    public async Task Las_reglas_se_actualizan_se_desactivan_y_se_revisan()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var regla = await CrearReglaAsync(e.C, "DescansoMinimo", Descanso(11));
        var id = regla.GetProperty("id").GetGuid();

        var revisada = await Planif.LeerAsync<JsonElement>(await e.C.PostAsync($"/api/reglas-planificacion/{id}/revisar", null));
        Assert.False(revisada.GetProperty("pendienteDeRevision").GetBoolean());
        Assert.Equal("Admin Sociedad", revisada.GetProperty("revisadaPor").GetString());

        // Cambiar la regla la devuelve a «pendiente de revisión»; el tipo no se puede cambiar
        var cambiada = await Planif.LeerAsync<JsonElement>(await e.C.PutAsJsonAsync($"/api/reglas-planificacion/{id}", new
        { tipo = "DescansoMinimo", nombre = "Descanso 10 h", severidad = "Advertencia", permiteExcepcion = true, parametros = Descanso(10) }));
        Assert.True(cambiada.GetProperty("pendienteDeRevision").GetBoolean());
        Assert.Equal("Advertencia", cambiada.GetProperty("severidad").GetString());
        Assert.False(cambiada.GetProperty("permiteExcepcion").GetBoolean()); // una advertencia no requiere excepción
        Assert.Equal(HttpStatusCode.BadRequest, (await e.C.PutAsJsonAsync($"/api/reglas-planificacion/{id}", new
        { tipo = "MaxDiasConsecutivos", nombre = "X", severidad = "Bloqueante", parametros = new { maxDias = 5 } })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await e.C.PostAsync($"/api/reglas-planificacion/{id}/desactivar", null)).StatusCode);
        var lista = await e.C.GetFromJsonAsync<List<JsonElement>>("/api/reglas-planificacion", AyudaSesion.Json);
        Assert.False(lista!.Single(r => r.GetProperty("id").GetGuid() == id).GetProperty("activa").GetBoolean());
    }

    [Fact]
    public async Task Solo_el_administrador_configura_las_reglas_y_el_coordinador_las_consulta()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var regla = await CrearReglaAsync(e.C, "DescansoMinimo", Descanso(11));
        var id = regla.GetProperty("id").GetGuid();
        var (_, coordinador) = await e.UsuarioAsync("CoordinadorMedico");
        var (_, profesional) = await e.UsuarioAsync("ProfesionalSanitario");
        var (_, responsable) = await e.UsuarioAsync("ResponsableCentro");

        Assert.Equal(HttpStatusCode.OK, (await coordinador.GetAsync("/api/reglas-planificacion")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await coordinador.PostAsJsonAsync("/api/reglas-planificacion", new { tipo = "DescansoMinimo", nombre = "X", severidad = "Bloqueante", parametros = Descanso(8) })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await coordinador.PostAsync($"/api/reglas-planificacion/{id}/desactivar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await coordinador.PostAsync($"/api/reglas-planificacion/{id}/revisar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await profesional.GetAsync("/api/reglas-planificacion")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await responsable.GetAsync("/api/reglas-planificacion")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await profesional.GetAsync("/api/excepciones-reglas")).StatusCode);
    }

    [Fact]
    public async Task Las_reglas_de_una_sociedad_no_son_visibles_ni_editables_desde_otra()
    {
        var a = await Escenario.CrearAsync(fabrica);
        var b = await Escenario.CrearAsync(fabrica);
        var id = (await CrearReglaAsync(a.C, "DescansoMinimo", Descanso(11))).GetProperty("id").GetGuid();

        Assert.Empty(await b.C.GetFromJsonAsync<List<JsonElement>>("/api/reglas-planificacion", AyudaSesion.Json) ?? []);
        Assert.Equal(HttpStatusCode.NotFound, (await b.C.PostAsync($"/api/reglas-planificacion/{id}/desactivar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.C.PostAsync($"/api/reglas-planificacion/{id}/revisar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.C.PutAsJsonAsync($"/api/reglas-planificacion/{id}", new { tipo = "DescansoMinimo", nombre = "X", severidad = "Bloqueante", parametros = Descanso(8) })).StatusCode);

        // Una regla de A no afecta a las asignaciones de B
        await b.CuadranteAsync("2026-11-02", "2026-11-03");
        var prof = await b.ProfesionalAsync();
        await b.AsignarOkAsync(await b.NecesidadAsync("2026-11-02", b.Noche), prof);
        await b.AsignarOkAsync(await b.NecesidadAsync("2026-11-03", b.Manana), prof);

        // Y B no puede usar un servicio de A en una regla
        Assert.Equal(HttpStatusCode.BadRequest, (await b.C.PostAsJsonAsync("/api/reglas-planificacion", new { tipo = "DescansoMinimo", nombre = "X", severidad = "Bloqueante", servicioId = a.Servicio, parametros = Descanso(8) })).StatusCode);
    }

    [Fact]
    public async Task Crear_una_regla_queda_auditado()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var id = (await CrearReglaAsync(e.C, "DescansoMinimo", Descanso(11))).GetProperty("id").GetGuid();

        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<MedicalHubDbContext>();
        var registro = await db.RegistrosAuditoria.IgnoreQueryFilters().SingleAsync(a => a.Entidad == "ReglaPlanificacion" && a.EntidadId == id.ToString() && a.Accion == "Alta");
        Assert.Equal(e.Admin.UsuarioId, registro.UsuarioId);
        Assert.Contains("DescansoMinimo", registro.Cambios);
    }

    // ── Asignación con reglas y excepciones ──

    [Fact]
    public async Task Una_infraccion_bloqueante_exige_una_excepcion_justificada_que_queda_registrada()
    {
        var (e, prof, manana) = await NocheYMananaAsync();

        // Sin excepción: 422 con el detalle para que el cliente pueda ofrecerla
        var rechazo = await e.AsignarAsync(manana, prof);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, rechazo.StatusCode);
        var problema = await rechazo.Content.ReadFromJsonAsync<JsonElement>(AyudaSesion.Json);
        Assert.Equal("requiere_excepcion", problema.GetProperty("codigo").GetString());
        var violacion = problema.GetProperty("violaciones").EnumerateArray().Single();
        Assert.Equal("DescansoMinimo", violacion.GetProperty("codigo").GetString());
        Assert.True(violacion.GetProperty("permiteExcepcion").GetBoolean());
        Assert.Contains("mínimo 12 h", violacion.GetProperty("mensaje").GetString());

        // Justificación demasiado corta
        var corta = await e.C.PostAsJsonAsync($"/api/necesidades/{manana}/asignaciones", new { profesionalId = prof, excepcion = new { justificacion = "urgente" } });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, corta.StatusCode);

        // Con justificación válida
        var ok = await Planif.LeerAsync<JsonElement>(await e.C.PostAsJsonAsync($"/api/necesidades/{manana}/asignaciones",
            new { profesionalId = prof, excepcion = new { justificacion = Justificacion } }), HttpStatusCode.Created);
        Assert.Single(ok.GetProperty("excepciones").EnumerateArray());

        // Registro de excepciones
        var lista = await e.C.GetFromJsonAsync<List<JsonElement>>($"/api/excepciones-reglas?profesionalId={prof}", AyudaSesion.Json);
        var ex = Assert.Single(lista!);
        Assert.Equal(Justificacion, ex.GetProperty("justificacion").GetString());
        Assert.Equal("Admin Sociedad", ex.GetProperty("autorizadaPor").GetString());
        Assert.Equal("DescansoMinimo", ex.GetProperty("codigo").GetString());
        Assert.True(ex.GetProperty("asignacionActiva").GetBoolean());
        Assert.Contains("Mañana del 03/11/2026", ex.GetProperty("turno").GetString());
    }

    [Fact]
    public async Task Una_excepcion_autorizada_no_bloquea_la_publicacion_y_se_ve_en_la_validacion()
    {
        var (e, prof, manana) = await NocheYMananaAsync();
        await e.C.PostAsJsonAsync($"/api/necesidades/{manana}/asignaciones", new { profesionalId = prof, excepcion = new { justificacion = Justificacion } });
        var cuadrante = (await e.C.GetFromJsonAsync<List<JsonElement>>("/api/cuadrantes", AyudaSesion.Json))!.Single().GetProperty("id").GetGuid();

        var validacion = await e.C.GetFromJsonAsync<JsonElement>($"/api/cuadrantes/{cuadrante}/validacion", AyudaSesion.Json);
        Assert.True(validacion.GetProperty("puedePublicarse").GetBoolean());
        var incidencia = validacion.GetProperty("incidencias").EnumerateArray().Single(i => i.GetProperty("codigo").GetString() == "DescansoMinimo");
        Assert.True(incidencia.GetProperty("excepcionada").GetBoolean());
        Assert.Equal(Justificacion, incidencia.GetProperty("justificacion").GetString());
        Assert.Equal("Admin Sociedad", incidencia.GetProperty("autorizadaPor").GetString());

        Assert.Equal(HttpStatusCode.OK, (await e.C.PostAsync($"/api/cuadrantes/{cuadrante}/publicar", null)).StatusCode);
    }

    [Fact]
    public async Task Sin_excepcion_una_regla_que_se_endurece_despues_bloquea_la_publicacion()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var cuadrante = await e.CuadranteAsync("2026-11-02", "2026-11-03");
        var prof = await e.ProfesionalAsync();
        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-02", e.Noche), prof);
        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-03", e.Manana), prof);   // sin reglas, se puede

        await CrearReglaAsync(e.C, "DescansoMinimo", Descanso(12));                      // la sociedad la configura después

        var validacion = await e.C.GetFromJsonAsync<JsonElement>($"/api/cuadrantes/{cuadrante.Id}/validacion", AyudaSesion.Json);
        Assert.False(validacion.GetProperty("puedePublicarse").GetBoolean());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await e.C.PostAsync($"/api/cuadrantes/{cuadrante.Id}/publicar", null)).StatusCode);
    }

    [Fact]
    public async Task Al_cancelar_la_asignacion_la_excepcion_queda_como_historial_inactivo()
    {
        var (e, prof, manana) = await NocheYMananaAsync();
        var asignacion = await Planif.LeerAsync<JsonElement>(await e.C.PostAsJsonAsync($"/api/necesidades/{manana}/asignaciones",
            new { profesionalId = prof, excepcion = new { justificacion = Justificacion } }), HttpStatusCode.Created);

        await e.C.DeleteAsync($"/api/asignaciones/{asignacion.GetProperty("id").GetGuid()}");

        var ex = (await e.C.GetFromJsonAsync<List<JsonElement>>($"/api/excepciones-reglas?profesionalId={prof}", AyudaSesion.Json))!.Single();
        Assert.False(ex.GetProperty("asignacionActiva").GetBoolean());
    }

    [Fact]
    public async Task Cuando_la_excepcion_exige_al_administrador_el_coordinador_no_puede_autorizarla()
    {
        var (e, prof, manana) = await NocheYMananaAsync(admin: true);
        var (_, coordinador) = await e.UsuarioAsync("CoordinadorMedico");

        var r = await e.C.PostAsJsonAsync($"/api/necesidades/{manana}/asignaciones", new { profesionalId = prof, excepcion = new { justificacion = Justificacion } });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode); // el administrador sí
        await e.C.DeleteAsync($"/api/asignaciones/{(await r.Content.ReadFromJsonAsync<JsonElement>(AyudaSesion.Json)).GetProperty("id").GetGuid()}");

        var denegada = await coordinador.PostAsJsonAsync($"/api/necesidades/{manana}/asignaciones", new { profesionalId = prof, excepcion = new { justificacion = Justificacion } });
        Assert.Equal(HttpStatusCode.Forbidden, denegada.StatusCode);
        Assert.Equal("requiere_administrador", (await denegada.Content.ReadFromJsonAsync<JsonElement>(AyudaSesion.Json)).GetProperty("codigo").GetString());

        // Los candidatos lo reflejan: para el coordinador no puede autorizar, para el administrador sí
        var paraCoord = (await coordinador.GetFromJsonAsync<List<JsonElement>>($"/api/necesidades/{manana}/candidatos", AyudaSesion.Json))!.Single(c => c.GetProperty("profesionalId").GetGuid() == prof);
        var paraAdmin = (await e.C.GetFromJsonAsync<List<JsonElement>>($"/api/necesidades/{manana}/candidatos", AyudaSesion.Json))!.Single(c => c.GetProperty("profesionalId").GetGuid() == prof);
        Assert.True(paraCoord.GetProperty("requiereExcepcion").GetBoolean());
        Assert.False(paraCoord.GetProperty("puedeAutorizar").GetBoolean());
        Assert.True(paraAdmin.GetProperty("puedeAutorizar").GetBoolean());
    }

    [Fact]
    public async Task Cuando_la_regla_permite_la_excepcion_al_coordinador_este_puede_autorizarla()
    {
        var (e, prof, manana) = await NocheYMananaAsync(admin: false);
        var (_, coordinador) = await e.UsuarioAsync("CoordinadorMedico");
        var r = await coordinador.PostAsJsonAsync($"/api/necesidades/{manana}/asignaciones", new { profesionalId = prof, excepcion = new { justificacion = Justificacion } });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var ex = (await e.C.GetFromJsonAsync<List<JsonElement>>("/api/excepciones-reglas", AyudaSesion.Json))!.Single(x => x.GetProperty("profesionalId").GetGuid() == prof);
        Assert.Equal("Usuario", ex.GetProperty("autorizadaPor").GetString()!.Split(' ')[0]);
    }

    [Fact]
    public async Task Una_regla_sin_excepcion_posible_no_se_salta_ni_con_justificacion()
    {
        var (e, prof, manana) = await NocheYMananaAsync(permite: false);
        var r = await e.C.PostAsJsonAsync($"/api/necesidades/{manana}/asignaciones", new { profesionalId = prof, excepcion = new { justificacion = Justificacion } });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
        Assert.Equal("regla_incumplida", (await r.Content.ReadFromJsonAsync<JsonElement>(AyudaSesion.Json)).GetProperty("codigo").GetString());
        Assert.Empty(await e.C.GetFromJsonAsync<List<JsonElement>>("/api/excepciones-reglas", AyudaSesion.Json) ?? []);
    }

    [Fact]
    public async Task El_solapamiento_no_admite_excepcion_nunca()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-02");
        var turno = (await e.TurnosAsync("2026-11-02", "2026-11-02")).Single(t => t.GetProperty("tipoTurnoId").GetGuid() == e.Manana);
        await e.C.PutAsJsonAsync($"/api/turnos/{turno.GetProperty("id").GetGuid()}/necesidades", new { necesidades = new[] { new { numeroPlazas = 1, categoriaProfesionalId = (Guid?)null }, new { numeroPlazas = 1, categoriaProfesionalId = (Guid?)e.Residente } } });
        var ids = (await e.TurnosAsync("2026-11-02", "2026-11-02")).Single(t => t.GetProperty("tipoTurnoId").GetGuid() == e.Manana).GetProperty("necesidades").EnumerateArray().Select(n => n.GetProperty("id").GetGuid()).ToList();
        var prof = await e.ProfesionalAsync(categoria: e.Residente);
        await e.AsignarOkAsync(ids[0], prof);

        var r = await e.C.PostAsJsonAsync($"/api/necesidades/{ids[1]}/asignaciones", new { profesionalId = prof, excepcion = new { justificacion = Justificacion } });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
        Assert.Equal("regla_incumplida", (await r.Content.ReadFromJsonAsync<JsonElement>(AyudaSesion.Json)).GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Una_advertencia_no_impide_asignar_pero_se_notifica_y_aparece_al_validar()
    {
        var (e, prof, manana) = await NocheYMananaAsync(severidad: "Advertencia");
        var ok = await Planif.LeerAsync<JsonElement>(await e.AsignarAsync(manana, prof), HttpStatusCode.Created);

        Assert.Empty(ok.GetProperty("excepciones").EnumerateArray());
        var aviso = Assert.Single(ok.GetProperty("advertencias").EnumerateArray());
        Assert.Equal("DescansoMinimo", aviso.GetProperty("codigo").GetString());
        Assert.Empty(await e.C.GetFromJsonAsync<List<JsonElement>>("/api/excepciones-reglas", AyudaSesion.Json) ?? []);

        var cuadrante = (await e.C.GetFromJsonAsync<List<JsonElement>>("/api/cuadrantes", AyudaSesion.Json))!.Single().GetProperty("id").GetGuid();
        var validacion = await e.C.GetFromJsonAsync<JsonElement>($"/api/cuadrantes/{cuadrante}/validacion", AyudaSesion.Json);
        Assert.True(validacion.GetProperty("puedePublicarse").GetBoolean());
        Assert.Contains(validacion.GetProperty("incidencias").EnumerateArray(), i => i.GetProperty("codigo").GetString() == "DescansoMinimo" && i.GetProperty("severidad").GetString() == "Advertencia");
    }

    [Fact]
    public async Task Una_regla_de_un_servicio_no_afecta_a_otros_servicios()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var otroServicio = await Planif.CrearServicioAsync(e.C, e.Centro, competencias: [e.Competencia]);
        var tipo = (await Planif.LeerAsync<IdDto>(await e.C.PostAsJsonAsync($"/api/servicios/{otroServicio}/tipos-turno", new { nombre = "Tarde", horaInicio = "15:00", horaFin = "22:00", esGuardia = false }), HttpStatusCode.Created)).Id;
        await e.C.PutAsJsonAsync($"/api/servicios/{otroServicio}/requisitos", new { requisitos = new[] { new { tipoTurnoId = tipo, numeroProfesionales = 1, categoriaProfesionalId = (Guid?)null } } });
        await e.CuadranteAsync("2026-11-02", "2026-11-02");
        await e.CuadranteAsync("2026-11-02", "2026-11-02", servicio: otroServicio);

        // Descanso de 12 h SOLO para el servicio original
        await CrearReglaAsync(e.C, "DescansoMinimo", Descanso(12), servicio: e.Servicio);
        var prof = await e.ProfesionalEnAsync([e.Servicio, otroServicio]);
        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-02", e.Manana), prof);     // 08:00–15:00

        // En el otro servicio (15:00–22:00, sin hueco) la regla no aplica
        var tarde = (await e.TurnosAsync("2026-11-02", "2026-11-02")).Single(t => t.GetProperty("tipoTurnoId").GetGuid() == tipo).GetProperty("necesidades")[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Created, (await e.AsignarAsync(tarde, prof)).StatusCode);

        // En el servicio original sí
        var noche = await e.NecesidadAsync("2026-11-02", e.Noche);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await e.AsignarAsync(noche, prof)).StatusCode);
    }

    [Fact]
    public async Task La_sociedad_puede_rebajar_o_anular_una_regla_base_de_elegibilidad()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-02");
        var sinCompetencia = await e.ProfesionalAsync(conCompetencia: false);
        var manana = await e.NecesidadAsync("2026-11-02", e.Manana);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await e.AsignarAsync(manana, sinCompetencia)).StatusCode);   // por defecto: bloqueante

        var regla = await CrearReglaAsync(e.C, "CompetenciasRequeridas", null, "Advertencia", permiteExcepcion: false, nombre: "Competencias (aviso)");
        var ok = await Planif.LeerAsync<JsonElement>(await e.AsignarAsync(manana, sinCompetencia), HttpStatusCode.Created);
        Assert.Contains("Falta la competencia", ok.GetProperty("advertencias")[0].GetProperty("mensaje").GetString());

        // Desactivar la regla la anula por completo (no vuelve el comportamiento por defecto)
        await e.C.PostAsync($"/api/reglas-planificacion/{regla.GetProperty("id").GetGuid()}/desactivar", null);
        var otro = await e.ProfesionalAsync(conCompetencia: false);
        var r = await Planif.LeerAsync<JsonElement>(await e.AsignarAsync(manana, otro), HttpStatusCode.Created);
        Assert.Empty(r.GetProperty("advertencias").EnumerateArray());
        Assert.Empty(r.GetProperty("excepciones").EnumerateArray());
    }

    [Fact]
    public async Task El_maximo_de_horas_en_una_ventana_bloquea_la_tercera_manana_sin_excepcion()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-09");
        await CrearReglaAsync(e.C, "MaxHorasPeriodo", new { ventanaDias = 7, maxHoras = 20 });   // 3 mañanas de 7 h = 21 h
        var prof = await e.ProfesionalAsync();

        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-02", e.Manana), prof);
        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-03", e.Manana), prof);
        var tercera = await e.AsignarAsync(await e.NecesidadAsync("2026-11-04", e.Manana), prof);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tercera.StatusCode);
        Assert.Contains("21 h", await tercera.Content.ReadAsStringAsync());
        Assert.Contains("máximo: 20 h", await tercera.Content.ReadAsStringAsync());

        // fuera de la ventana de 7 días sí
        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-09", e.Manana), prof);
    }

    [Fact]
    public async Task La_incompatibilidad_entre_servicios_bloquea_turnos_cercanos_en_ambos()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var otroServicio = await Planif.CrearServicioAsync(e.C, e.Centro, competencias: [e.Competencia]);
        var tipo = (await Planif.LeerAsync<IdDto>(await e.C.PostAsJsonAsync($"/api/servicios/{otroServicio}/tipos-turno", new { nombre = "Tarde", horaInicio = "16:00", horaFin = "22:00", esGuardia = false }), HttpStatusCode.Created)).Id;
        await e.C.PutAsJsonAsync($"/api/servicios/{otroServicio}/requisitos", new { requisitos = new[] { new { tipoTurnoId = tipo, numeroProfesionales = 1, categoriaProfesionalId = (Guid?)null } } });
        await e.CuadranteAsync("2026-11-02", "2026-11-02");
        await e.CuadranteAsync("2026-11-02", "2026-11-02", servicio: otroServicio);
        await CrearReglaAsync(e.C, "IncompatibilidadServicios", new { servicioAId = e.Servicio, servicioBId = otroServicio, margenHoras = 3 });

        var prof = await e.ProfesionalEnAsync([e.Servicio, otroServicio]);
        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-02", e.Manana), prof);   // termina 15:00
        var tarde = (await e.TurnosAsync("2026-11-02", "2026-11-02")).Single(t => t.GetProperty("tipoTurnoId").GetGuid() == tipo).GetProperty("necesidades")[0].GetProperty("id").GetGuid();
        var r = await e.AsignarAsync(tarde, prof);                                       // empieza 16:00: solo 1 h
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
        Assert.Contains("Incompatible con", await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Una_cobertura_minima_bloqueante_impide_publicar_hasta_alcanzarla()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var cuadrante = await e.CuadranteAsync("2026-11-02", "2026-11-02");
        await CrearReglaAsync(e.C, "CoberturaMinima", new { porcentajeMinimo = 100 }, permiteExcepcion: false);

        async Task<bool> PuedePublicar() => (await e.C.GetFromJsonAsync<JsonElement>($"/api/cuadrantes/{cuadrante.Id}/validacion", AyudaSesion.Json)).GetProperty("puedePublicarse").GetBoolean();
        Assert.False(await PuedePublicar());

        // Cubrimos las 2 plazas de mañana y la de noche
        var mananaN = await e.NecesidadAsync("2026-11-02", e.Manana);
        await e.AsignarOkAsync(mananaN, await e.ProfesionalAsync());
        await e.AsignarOkAsync(mananaN, await e.ProfesionalAsync());
        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-02", e.Noche), await e.ProfesionalAsync());
        Assert.True(await PuedePublicar());
        Assert.Equal(HttpStatusCode.OK, (await e.C.PostAsync($"/api/cuadrantes/{cuadrante.Id}/publicar", null)).StatusCode);
    }

    [Fact]
    public async Task Los_candidatos_distinguen_validos_los_que_requieren_excepcion_y_los_no_permitidos()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-02");
        var necesidad = await e.NecesidadAsync("2026-11-02", e.Manana);
        var valido = await e.ProfesionalAsync();
        var sinCompetencia = await e.ProfesionalAsync(conCompetencia: false);   // requiere excepción (regla por defecto)
        var inactivo = await e.ProfesionalAsync(activo: false);                  // los inactivos ni se listan
        var ocupado = await e.ProfesionalAsync();
        await e.AsignarOkAsync(necesidad, ocupado);                              // ya está en esta necesidad

        var c = (await e.C.GetFromJsonAsync<List<JsonElement>>($"/api/necesidades/{necesidad}/candidatos", AyudaSesion.Json))!.ToDictionary(x => x.GetProperty("profesionalId").GetGuid());
        Assert.True(c[valido].GetProperty("elegible").GetBoolean());
        Assert.False(c[valido].GetProperty("requiereExcepcion").GetBoolean());
        Assert.False(c[sinCompetencia].GetProperty("elegible").GetBoolean());
        Assert.True(c[sinCompetencia].GetProperty("requiereExcepcion").GetBoolean());
        Assert.True(c[sinCompetencia].GetProperty("puedeAutorizar").GetBoolean());
        Assert.False(c[ocupado].GetProperty("elegible").GetBoolean());
        Assert.False(c.ContainsKey(inactivo));
    }

    [Fact]
    public async Task El_registro_de_excepciones_esta_aislado_por_sociedad_y_admite_filtros()
    {
        var (a, profA, mananaA) = await NocheYMananaAsync();
        await a.C.PostAsJsonAsync($"/api/necesidades/{mananaA}/asignaciones", new { profesionalId = profA, excepcion = new { justificacion = Justificacion } });
        var b = await Escenario.CrearAsync(fabrica);

        Assert.Empty(await b.C.GetFromJsonAsync<List<JsonElement>>("/api/excepciones-reglas", AyudaSesion.Json) ?? []);
        Assert.Single(await a.C.GetFromJsonAsync<List<JsonElement>>("/api/excepciones-reglas?desde=2000-01-01&hasta=2100-01-01", AyudaSesion.Json) ?? []);
        Assert.Empty(await a.C.GetFromJsonAsync<List<JsonElement>>($"/api/excepciones-reglas?profesionalId={Guid.NewGuid()}", AyudaSesion.Json) ?? []);
        Assert.Empty(await a.C.GetFromJsonAsync<List<JsonElement>>("/api/excepciones-reglas?desde=2000-01-01&hasta=2000-12-31", AyudaSesion.Json) ?? []);
    }
}

[Collection(ColeccionApi.Nombre)]
public class ExcepcionRetroactivaTests(FabricaApi fabrica)
{
    private const string Justificacion = "Acuerdo puntual aprobado por la dirección médica.";

    private static async Task CrearDescansoAsync(HttpClient c, bool admin = false)
    {
        var r = await c.PostAsJsonAsync("/api/reglas-planificacion", new
        { tipo = "DescansoMinimo", nombre = "Descanso 12 h", severidad = "Bloqueante", permiteExcepcion = true, excepcionRequiereAdministrador = admin, parametros = new { horas = 12 } });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    /// <summary>Dos turnos pegados asignados al mismo profesional ANTES de que exista la regla.</summary>
    private async Task<(Escenario e, Guid cuadrante, Guid asignacionTardia)> EscenarioAsync()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var cuadrante = await e.CuadranteAsync("2026-11-02", "2026-11-03");
        var prof = await e.ProfesionalAsync();
        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-02", e.Noche), prof);
        var tardia = await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-03", e.Manana), prof);
        return (e, cuadrante.Id, tardia);
    }

    [Fact]
    public async Task La_infraccion_entre_dos_turnos_se_atribuye_una_sola_vez_a_la_asignacion_mas_reciente()
    {
        var (e, cuadrante, tardia) = await EscenarioAsync();
        await CrearDescansoAsync(e.C);

        var validacion = await e.C.GetFromJsonAsync<JsonElement>($"/api/cuadrantes/{cuadrante}/validacion", AyudaSesion.Json);
        var descansos = validacion.GetProperty("incidencias").EnumerateArray().Where(i => i.GetProperty("codigo").GetString() == "DescansoMinimo").ToList();
        Assert.Single(descansos);
        Assert.False(validacion.GetProperty("puedePublicarse").GetBoolean());
        Assert.NotEqual(Guid.Empty, tardia);
    }

    [Fact]
    public async Task Se_puede_autorizar_a_posteriori_una_excepcion_y_entonces_se_puede_publicar()
    {
        var (e, cuadrante, tardia) = await EscenarioAsync();
        await CrearDescansoAsync(e.C);

        var corta = await e.C.PostAsJsonAsync($"/api/asignaciones/{tardia}/excepciones", new { justificacion = "ok" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, corta.StatusCode);

        var r = await e.C.PostAsJsonAsync($"/api/asignaciones/{tardia}/excepciones", new { justificacion = Justificacion });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Single((await r.Content.ReadFromJsonAsync<JsonElement>(AyudaSesion.Json)).EnumerateArray());

        var validacion = await e.C.GetFromJsonAsync<JsonElement>($"/api/cuadrantes/{cuadrante}/validacion", AyudaSesion.Json);
        Assert.True(validacion.GetProperty("puedePublicarse").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await e.C.PostAsync($"/api/cuadrantes/{cuadrante}/publicar", null)).StatusCode);

        // Una segunda autorización ya no tiene nada pendiente (y el cuadrante publicado no admite cambios)
        Assert.Equal(HttpStatusCode.Conflict, (await e.C.PostAsJsonAsync($"/api/asignaciones/{tardia}/excepciones", new { justificacion = Justificacion })).StatusCode);
    }

    [Fact]
    public async Task La_excepcion_retroactiva_respeta_quien_puede_autorizarla_y_el_aislamiento()
    {
        var (e, _, tardia) = await EscenarioAsync();
        await CrearDescansoAsync(e.C, admin: true);
        var (_, coordinador) = await e.UsuarioAsync("CoordinadorMedico");
        var otra = await Escenario.CrearAsync(fabrica);

        Assert.Equal(HttpStatusCode.Forbidden, (await coordinador.PostAsJsonAsync($"/api/asignaciones/{tardia}/excepciones", new { justificacion = Justificacion })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otra.C.PostAsJsonAsync($"/api/asignaciones/{tardia}/excepciones", new { justificacion = Justificacion })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await e.C.PostAsJsonAsync($"/api/asignaciones/{tardia}/excepciones", new { justificacion = Justificacion })).StatusCode);
    }

    [Fact]
    public async Task No_hay_excepcion_retroactiva_si_no_hay_infracciones()
    {
        var (e, _, tardia) = await EscenarioAsync();
        Assert.Equal(HttpStatusCode.Conflict, (await e.C.PostAsJsonAsync($"/api/asignaciones/{tardia}/excepciones", new { justificacion = Justificacion })).StatusCode);
    }
}
