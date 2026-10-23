using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MedicalHub.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalHub.Api.IntegrationTests;

[Collection(ColeccionApi.Nombre)]
public class AusenciasTests(FabricaApi fabrica)
{
    private static object Ausencia(string desde, string hasta, string tipo = "Vacaciones", string? comentario = null) => new { tipo, desde, hasta, comentario };

    private static async Task<JsonElement> SolicitarAsync(HttpClient c, string desde, string hasta, string tipo = "Vacaciones")
        => await Planif.LeerAsync<JsonElement>(await c.PostAsJsonAsync("/api/mis-ausencias", Ausencia(desde, hasta, tipo)), HttpStatusCode.Created);

    /// <summary>Cuadrante de una semana con el profesional en la mañana de los días indicados.</summary>
    private async Task<(Escenario e, Guid prof, HttpClient cliente, Guid cuadrante)> EscenarioConTurnosAsync(params string[] diasManana)
    {
        var e = await Escenario.CrearAsync(fabrica);
        var cuadrante = await e.CuadranteAsync("2026-11-02", "2026-11-08");
        var (prof, cliente, _, _) = await e.ProfesionalConUsuarioAsync();
        foreach (var dia in diasManana) await e.AsignarOkAsync(await e.NecesidadAsync(dia, e.Manana), prof);
        return (e, prof, cliente, cuadrante.Id);
    }

    private static async Task<Guid> VacanteDeAsync(HttpClient c, Guid ausenciaId, string estado = "Abierta")
        => (await c.GetFromJsonAsync<List<JsonElement>>($"/api/vacantes?estado={estado}", AyudaSesion.Json))!
            .Single(v => v.GetProperty("ausenciaId").GetGuid() == ausenciaId).GetProperty("id").GetGuid();

    // ── Solicitud ──

    [Fact]
    public async Task El_profesional_solicita_una_ausencia_y_ve_su_estado_y_el_coordinador_la_ve_pendiente()
    {
        var (e, prof, cliente, _) = await EscenarioConTurnosAsync();
        var a = await SolicitarAsync(cliente, "2026-12-21", "2026-12-31");

        Assert.Equal("Solicitada", a.GetProperty("estado").GetString());
        Assert.Equal(11, a.GetProperty("dias").GetInt32());
        Assert.Equal("Solicitada", a.GetProperty("decisiones")[0].GetProperty("decision").GetString());

        var mias = await cliente.GetFromJsonAsync<List<JsonElement>>("/api/mis-ausencias", AyudaSesion.Json);
        Assert.Single(mias!);

        var pendientes = await e.C.GetFromJsonAsync<List<JsonElement>>("/api/ausencias?estado=Solicitada", AyudaSesion.Json);
        var ausencia = Assert.Single(pendientes!, x => x.GetProperty("profesionalId").GetGuid() == prof);
        Assert.Equal("Vacaciones", ausencia.GetProperty("tipo").GetString());
    }

    [Fact]
    public async Task Las_solicitudes_validan_fechas_solapes_y_longitud_del_comentario()
    {
        var (_, _, cliente, _) = await EscenarioConTurnosAsync();
        await SolicitarAsync(cliente, "2026-12-21", "2026-12-31");

        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync("/api/mis-ausencias", Ausencia("2026-12-28", "2027-01-05"))).StatusCode);          // solapa
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await cliente.PostAsJsonAsync("/api/mis-ausencias", Ausencia("2027-02-10", "2027-02-01"))).StatusCode); // al revés
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await cliente.PostAsJsonAsync("/api/mis-ausencias", Ausencia("2027-03-01", "2028-06-01"))).StatusCode); // > 366 días
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await cliente.PostAsJsonAsync("/api/mis-ausencias", Ausencia("2027-04-01", "2027-04-02", comentario: new string('x', 301)))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/mis-ausencias", Ausencia("2020-01-01", "2020-01-05"))).StatusCode);       // ya empezada
    }

    [Fact]
    public async Task El_profesional_solo_cancela_una_solicitud_pendiente_y_no_una_aprobada()
    {
        var (e, _, cliente, _) = await EscenarioConTurnosAsync();
        var a = await SolicitarAsync(cliente, "2026-12-21", "2026-12-23");
        var id = a.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync($"/api/mis-ausencias/{id}/cancelar", null)).StatusCode);

        var b = await SolicitarAsync(cliente, "2026-12-21", "2026-12-23");   // al cancelarse, esas fechas quedan libres
        var idB = b.GetProperty("id").GetGuid();
        await e.C.PostAsync($"/api/ausencias/{idB}/aprobar", null);
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsync($"/api/mis-ausencias/{idB}/cancelar", null)).StatusCode);

        // El coordinador sí puede cancelar la aprobada
        Assert.Equal(HttpStatusCode.OK, (await e.C.PostAsJsonAsync($"/api/ausencias/{idB}/cancelar", new { motivo = "Cambio de planes" })).StatusCode);
    }

    // ── Turnos comprometidos y vacantes ──

    [Fact]
    public async Task Aprobar_una_ausencia_cancela_las_asignaciones_afectadas_y_abre_una_vacante_por_cada_una()
    {
        var (e, _, cliente, _) = await EscenarioConTurnosAsync("2026-11-02", "2026-11-03", "2026-11-06");
        var id = (await SolicitarAsync(cliente, "2026-11-03", "2026-11-05")).GetProperty("id").GetGuid();   // cubre solo el martes

        // Antes de decidir: se ve qué turnos quedarían comprometidos
        var comprometidos = await e.C.GetFromJsonAsync<List<JsonElement>>($"/api/ausencias/{id}/comprometidos", AyudaSesion.Json);
        Assert.Equal("2026-11-03", Assert.Single(comprometidos!).GetProperty("fecha").GetString());

        var r = await Planif.LeerAsync<JsonElement>(await e.C.PostAsJsonAsync($"/api/ausencias/{id}/aprobar", new { motivo = "Aprobada" }));
        Assert.Equal("Aprobada", r.GetProperty("ausencia").GetProperty("estado").GetString());
        Assert.Single(r.GetProperty("vacantes").EnumerateArray());

        // El martes queda sin cubrir; lunes y viernes siguen asignados
        var turnos = await e.TurnosAsync("2026-11-02", "2026-11-08");
        int Cubiertas(string dia) => turnos.Single(t => t.GetProperty("fecha").GetString() == dia && t.GetProperty("tipoTurnoId").GetGuid() == e.Manana).GetProperty("plazasCubiertas").GetInt32();
        Assert.Equal(1, Cubiertas("2026-11-02"));
        Assert.Equal(0, Cubiertas("2026-11-03"));
        Assert.Equal(1, Cubiertas("2026-11-06"));

        // Vacante abierta con su motivo, e historial de decisiones
        var vacante = (await e.C.GetFromJsonAsync<List<JsonElement>>("/api/vacantes?estado=Abierta", AyudaSesion.Json))!.Single(v => v.GetProperty("ausenciaId").GetGuid() == id);
        Assert.Equal("Ausencia aprobada", vacante.GetProperty("motivo").GetString());
        Assert.Contains("Mañana", vacante.GetProperty("turno").GetString());

        var ausencia = await e.C.GetFromJsonAsync<JsonElement>($"/api/ausencias/{id}", AyudaSesion.Json);
        Assert.Equal(["Solicitada", "Aprobada"], ausencia.GetProperty("decisiones").EnumerateArray().Select(d => d.GetProperty("decision").GetString()));
        Assert.Equal("Admin Sociedad", ausencia.GetProperty("decisiones")[1].GetProperty("usuario").GetString());

        // El profesional ve su ausencia aprobada
        Assert.Equal("Aprobada", (await cliente.GetFromJsonAsync<List<JsonElement>>("/api/mis-ausencias", AyudaSesion.Json))![0].GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Una_noche_que_empieza_antes_de_la_ausencia_pero_termina_dentro_tambien_se_ve_afectada()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-08");
        var (prof, cliente, _, _) = await e.ProfesionalConUsuarioAsync();
        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-02", e.Noche), prof);     // 22:00 del 02 → 08:00 del 03

        var id = (await SolicitarAsync(cliente, "2026-11-03", "2026-11-04")).GetProperty("id").GetGuid();
        Assert.Single(await e.C.GetFromJsonAsync<List<JsonElement>>($"/api/ausencias/{id}/comprometidos", AyudaSesion.Json) ?? []);

        // Una ausencia que empieza el día 04 ya no afecta a esa noche
        await e.C.PostAsync($"/api/ausencias/{id}/cancelar", null);
        var otra = (await SolicitarAsync(cliente, "2026-11-04", "2026-11-05")).GetProperty("id").GetGuid();
        Assert.Empty(await e.C.GetFromJsonAsync<List<JsonElement>>($"/api/ausencias/{otra}/comprometidos", AyudaSesion.Json) ?? []);
    }

    [Fact]
    public async Task Rechazar_exige_motivo_y_no_toca_las_asignaciones_y_una_solicitud_solo_se_decide_una_vez()
    {
        var (e, _, cliente, _) = await EscenarioConTurnosAsync("2026-11-03");
        var id = (await SolicitarAsync(cliente, "2026-11-03", "2026-11-03")).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.BadRequest, (await e.C.PostAsJsonAsync($"/api/ausencias/{id}/rechazar", new { motivo = " " })).StatusCode);
        var r = await Planif.LeerAsync<JsonElement>(await e.C.PostAsJsonAsync($"/api/ausencias/{id}/rechazar", new { motivo = "Hay demasiadas bajas esa semana" }));
        Assert.Equal("Rechazada", r.GetProperty("ausencia").GetProperty("estado").GetString());
        Assert.Empty(r.GetProperty("vacantes").EnumerateArray());

        var turno = (await e.TurnosAsync("2026-11-03", "2026-11-03")).Single(t => t.GetProperty("tipoTurnoId").GetGuid() == e.Manana);
        Assert.Equal(1, turno.GetProperty("plazasCubiertas").GetInt32());
        Assert.Equal(HttpStatusCode.Conflict, (await e.C.PostAsync($"/api/ausencias/{id}/aprobar", null)).StatusCode);

        // Las fechas de una solicitud rechazada quedan libres para solicitar de nuevo
        await SolicitarAsync(cliente, "2026-11-03", "2026-11-03");
    }

    [Fact]
    public async Task Las_asignaciones_ya_terminadas_no_se_convierten_en_vacantes()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2020-03-02", "2020-03-02");
        var prof = await e.ProfesionalAsync();
        await e.AsignarOkAsync(await e.NecesidadAsync("2020-03-02", e.Manana), prof);

        var r = await Planif.LeerAsync<JsonElement>(await e.C.PostAsJsonAsync("/api/ausencias",
            new { profesionalId = prof, tipo = "BajaTemporal", desde = "2020-03-01", hasta = "2020-03-10", aprobar = true }), HttpStatusCode.Created);
        Assert.Empty(r.GetProperty("vacantes").EnumerateArray());
        Assert.Equal(1, (await e.TurnosAsync("2020-03-02", "2020-03-02")).Single(t => t.GetProperty("tipoTurnoId").GetGuid() == e.Manana).GetProperty("plazasCubiertas").GetInt32());
    }

    [Fact]
    public async Task El_coordinador_puede_registrar_una_ausencia_ya_aprobada_con_sus_vacantes()
    {
        var (e, prof, _, _) = await EscenarioConTurnosAsync("2026-11-03", "2026-11-04");
        var r = await Planif.LeerAsync<JsonElement>(await e.C.PostAsJsonAsync("/api/ausencias",
            new { profesionalId = prof, tipo = "BajaTemporal", desde = "2026-11-03", hasta = "2026-11-04", aprobar = true }), HttpStatusCode.Created);
        Assert.Equal("Aprobada", r.GetProperty("ausencia").GetProperty("estado").GetString());
        Assert.Equal(2, r.GetProperty("vacantes").GetArrayLength());
    }

    [Fact]
    public async Task Varias_aprobaciones_simultaneas_de_la_misma_ausencia_solo_producen_un_efecto()
    {
        var (e, _, cliente, _) = await EscenarioConTurnosAsync("2026-11-03");
        var id = (await SolicitarAsync(cliente, "2026-11-03", "2026-11-03")).GetProperty("id").GetGuid();

        var respuestas = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => e.C.PostAsync($"/api/ausencias/{id}/aprobar", null)));

        Assert.Equal(1, respuestas.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(3, respuestas.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.Single(await e.C.GetFromJsonAsync<List<JsonElement>>("/api/vacantes", AyudaSesion.Json) ?? [], v => v.GetProperty("ausenciaId").GetGuid() == id);
    }

    // ── Cobertura de la vacante en un cuadrante publicado ──

    [Fact]
    public async Task Una_vacante_en_un_cuadrante_publicado_se_cubre_directamente_sin_reabrirlo()
    {
        var (e, _, cliente, cuadrante) = await EscenarioConTurnosAsync("2026-11-03");
        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-03", e.Manana), await e.ProfesionalAsync());
        await e.C.PostAsync($"/api/cuadrantes/{cuadrante}/publicar", null);

        var id = (await SolicitarAsync(cliente, "2026-11-03", "2026-11-03")).GetProperty("id").GetGuid();
        await e.C.PostAsync($"/api/ausencias/{id}/aprobar", null);

        var vacanteId = await VacanteDeAsync(e.C, id);
        var vacante = (await e.C.GetFromJsonAsync<List<JsonElement>>("/api/vacantes?estado=Abierta", AyudaSesion.Json))!.Single(v => v.GetProperty("id").GetGuid() == vacanteId);
        Assert.Equal("Publicado", vacante.GetProperty("cuadranteEstado").GetString());

        // Asignar por la vía normal sigue bloqueado en un publicado…
        var sustituto = await e.ProfesionalAsync(nombre: "Sustituto");
        Assert.Equal(HttpStatusCode.Conflict, (await e.AsignarAsync(vacante.GetProperty("necesidadId").GetGuid(), sustituto)).StatusCode);

        // …pero cubrir la vacante funciona y la cierra
        var ok = await e.C.PostAsJsonAsync($"/api/vacantes/{vacanteId}/cubrir", new { profesionalId = sustituto });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.DoesNotContain(await e.C.GetFromJsonAsync<List<JsonElement>>("/api/vacantes?estado=Abierta", AyudaSesion.Json) ?? [], v => v.GetProperty("id").GetGuid() == vacanteId);
        var cubierta = (await e.C.GetFromJsonAsync<List<JsonElement>>("/api/vacantes?estado=Cubierta", AyudaSesion.Json))!.Single(v => v.GetProperty("id").GetGuid() == vacanteId);
        Assert.NotNull(cubierta.GetProperty("fechaCierre").GetString());

        var turno = (await e.TurnosAsync("2026-11-03", "2026-11-03")).Single(t => t.GetProperty("tipoTurnoId").GetGuid() == e.Manana);
        Assert.Equal(2, turno.GetProperty("plazasCubiertas").GetInt32());
        Assert.Equal("Publicado", turno.GetProperty("cuadranteEstado").GetString());

        // Cubrirla otra vez → 409
        Assert.Equal(HttpStatusCode.Conflict, (await e.C.PostAsJsonAsync($"/api/vacantes/{vacanteId}/cubrir", new { profesionalId = await e.ProfesionalAsync() })).StatusCode);
    }

    [Fact]
    public async Task Cubrir_una_vacante_aplica_las_reglas_y_el_ausente_no_puede_volver_sin_excepcion()
    {
        var (e, prof, cliente, _) = await EscenarioConTurnosAsync("2026-11-03");
        var id = (await SolicitarAsync(cliente, "2026-11-03", "2026-11-03")).GetProperty("id").GetGuid();
        await e.C.PostAsync($"/api/ausencias/{id}/aprobar", null);
        var vacante = await VacanteDeAsync(e.C, id);

        var r = await e.C.PostAsJsonAsync($"/api/vacantes/{vacante}/cubrir", new { profesionalId = prof });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
        var problema = await r.Content.ReadFromJsonAsync<JsonElement>(AyudaSesion.Json);
        Assert.Equal("requiere_excepcion", problema.GetProperty("codigo").GetString());
        Assert.Equal("AusenciaAprobada", problema.GetProperty("violaciones")[0].GetProperty("codigo").GetString());

        // Un profesional sin competencia tampoco, y la vacante sigue abierta
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await e.C.PostAsJsonAsync($"/api/vacantes/{vacante}/cubrir", new { profesionalId = await e.ProfesionalAsync(conCompetencia: false) })).StatusCode);
        Assert.Contains(await e.C.GetFromJsonAsync<List<JsonElement>>("/api/vacantes?estado=Abierta", AyudaSesion.Json) ?? [], v => v.GetProperty("id").GetGuid() == vacante);
    }

    // ── Reglas: solo lo aprobado cuenta ──

    [Fact]
    public async Task Una_ausencia_aprobada_bloquea_nuevas_asignaciones_pero_una_pendiente_no()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-08");
        var (prof, cliente, _, _) = await e.ProfesionalConUsuarioAsync();
        var id = (await SolicitarAsync(cliente, "2026-11-04", "2026-11-04")).GetProperty("id").GetGuid();

        var necesidad = await e.NecesidadAsync("2026-11-04", e.Manana);
        var pendiente = await e.AsignarAsync(necesidad, prof);                  // solicitada: no cuenta todavía
        Assert.Equal(HttpStatusCode.Created, pendiente.StatusCode);
        await e.C.DeleteAsync($"/api/asignaciones/{(await pendiente.Content.ReadFromJsonAsync<JsonElement>(AyudaSesion.Json)).GetProperty("id").GetGuid()}");

        await e.C.PostAsync($"/api/ausencias/{id}/aprobar", null);
        var bloqueada = await e.AsignarAsync(necesidad, prof);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, bloqueada.StatusCode);
        Assert.Contains("ausencia aprobada (vacaciones)", await bloqueada.Content.ReadAsStringAsync());

        // Fuera de las fechas no hay problema, y con excepción justificada se puede
        Assert.Equal(HttpStatusCode.Created, (await e.AsignarAsync(await e.NecesidadAsync("2026-11-05", e.Manana), prof)).StatusCode);
        var conExcepcion = await e.C.PostAsJsonAsync($"/api/necesidades/{necesidad}/asignaciones", new { profesionalId = prof, excepcion = new { justificacion = "Se ofrece voluntario a trabajar ese día." } });
        Assert.Equal(HttpStatusCode.Created, conExcepcion.StatusCode);
    }

    [Fact]
    public async Task Las_ausencias_rechazadas_no_bloquean()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-08");
        var (prof, cliente, _, _) = await e.ProfesionalConUsuarioAsync();
        var id = (await SolicitarAsync(cliente, "2026-11-04", "2026-11-04")).GetProperty("id").GetGuid();
        await e.C.PostAsJsonAsync($"/api/ausencias/{id}/rechazar", new { motivo = "No procede" });
        Assert.Equal(HttpStatusCode.Created, (await e.AsignarAsync(await e.NecesidadAsync("2026-11-04", e.Manana), prof)).StatusCode);
    }

    // ── Permisos, privacidad y aislamiento ──

    [Fact]
    public async Task Solo_el_administrador_y_el_coordinador_gestionan_ausencias_y_vacantes()
    {
        var (e, _, cliente, _) = await EscenarioConTurnosAsync();
        var id = (await SolicitarAsync(cliente, "2026-12-21", "2026-12-22")).GetProperty("id").GetGuid();
        var (_, responsable) = await e.UsuarioAsync("ResponsableCentro");
        var (_, coordinador) = await e.UsuarioAsync("CoordinadorMedico");

        foreach (var c in new[] { cliente, responsable })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/ausencias")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsync($"/api/ausencias/{id}/aprobar", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/vacantes")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/disponibilidades")).StatusCode);
        }
        Assert.Equal(HttpStatusCode.OK, (await coordinador.GetAsync("/api/ausencias")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await coordinador.PostAsync($"/api/ausencias/{id}/aprobar", null)).StatusCode);
    }

    [Fact]
    public async Task Un_usuario_sin_ficha_profesional_no_puede_solicitar_ausencias()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var (_, sinFicha) = await e.UsuarioAsync("ProfesionalSanitario");
        Assert.Equal(HttpStatusCode.NotFound, (await sinFicha.GetAsync("/api/mis-ausencias")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await sinFicha.PostAsJsonAsync("/api/mis-ausencias", Ausencia("2026-12-21", "2026-12-22"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await sinFicha.GetAsync("/api/mi-disponibilidad")).StatusCode);
    }

    [Fact]
    public async Task Las_ausencias_y_vacantes_de_otra_sociedad_son_invisibles_e_intocables()
    {
        var (a, profA, clienteA, _) = await EscenarioConTurnosAsync("2026-11-03");
        var b = await Escenario.CrearAsync(fabrica);
        var id = (await SolicitarAsync(clienteA, "2026-11-03", "2026-11-03")).GetProperty("id").GetGuid();
        await a.C.PostAsync($"/api/ausencias/{id}/aprobar", null);
        var vacante = await VacanteDeAsync(a.C, id);

        Assert.Empty(await b.C.GetFromJsonAsync<List<JsonElement>>("/api/ausencias", AyudaSesion.Json) ?? []);
        Assert.Empty(await b.C.GetFromJsonAsync<List<JsonElement>>("/api/vacantes", AyudaSesion.Json) ?? []);
        Assert.Equal(HttpStatusCode.NotFound, (await b.C.GetAsync($"/api/ausencias/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.C.PostAsync($"/api/ausencias/{id}/cancelar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.C.PostAsJsonAsync($"/api/vacantes/{vacante}/cubrir", new { profesionalId = await b.ProfesionalAsync() })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.C.PostAsJsonAsync("/api/ausencias", new { profesionalId = profA, tipo = "Permiso", desde = "2026-12-01", hasta = "2026-12-02", aprobar = true })).StatusCode);
    }

    [Fact]
    public async Task La_ausencia_no_guarda_datos_de_salud_solo_el_tipo_y_un_comentario_acotado()
    {
        var (_, _, cliente, _) = await EscenarioConTurnosAsync();
        var a = await SolicitarAsync(cliente, "2026-12-21", "2026-12-22", "BajaTemporal");
        var campos = a.EnumerateObject().Select(p => p.Name).ToList();
        Assert.DoesNotContain(campos, c => c.Contains("diagn", StringComparison.OrdinalIgnoreCase) || c.Contains("enfermedad", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("tipo", campos);
        Assert.Contains("comentario", campos);
    }

    [Fact]
    public async Task Las_decisiones_sobre_una_ausencia_quedan_auditadas()
    {
        var (e, _, cliente, _) = await EscenarioConTurnosAsync();
        var id = (await SolicitarAsync(cliente, "2026-12-21", "2026-12-22")).GetProperty("id").GetGuid();
        await e.C.PostAsync($"/api/ausencias/{id}/aprobar", null);

        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<MedicalHubDbContext>();
        var acciones = await db.RegistrosAuditoria.IgnoreQueryFilters().Where(x => x.Entidad == "Ausencia" && x.EntidadId == id.ToString()).Select(x => x.Accion).ToListAsync();
        Assert.Contains("Alta", acciones);
        Assert.Contains("Modificación", acciones);
    }
}
