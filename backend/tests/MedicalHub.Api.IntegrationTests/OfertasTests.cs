using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MedicalHub.Api.IntegrationTests;

[Collection(ColeccionApi.Nombre)]
public class OfertasTests(FabricaApi fabrica)
{
    private const string Dia = "2026-11-03";

    private sealed record Caso(Escenario E, Guid Ausente, HttpClient ClienteAusente, Guid Vacante, Guid Necesidad);

    /// <summary>Profesional asignado el martes, con su ausencia aprobada: queda una vacante abierta.</summary>
    private async Task<Caso> CasoAsync()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-08");
        var (ausente, cliente, _, _) = await e.ProfesionalConUsuarioAsync(nombre: "Elena");
        var necesidad = await e.NecesidadAsync(Dia, e.Manana);
        await e.AsignarOkAsync(necesidad, ausente);

        var r = await cliente.PostAsJsonAsync("/api/mis-ausencias", new { tipo = "BajaTemporal", desde = Dia, hasta = Dia, comentario = (string?)null });
        var id = (await Planif.LeerAsync<JsonElement>(r, HttpStatusCode.Created)).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await e.C.PostAsync($"/api/ausencias/{id}/aprobar", null)).StatusCode);
        var vacante = (await e.C.GetFromJsonAsync<List<JsonElement>>("/api/vacantes?estado=Abierta", AyudaSesion.Json))!.Single().GetProperty("id").GetGuid();
        return new Caso(e, ausente, cliente, vacante, necesidad);
    }

    private static async Task<JsonElement> OfrecerUnoAsync(HttpClient c, Guid vacante, Guid profesional, int? horas = null)
    {
        var r = await c.PostAsJsonAsync($"/api/vacantes/{vacante}/ofertas", new { profesionalIds = new[] { profesional }, caducaEnHoras = horas });
        return (await Planif.LeerAsync<List<JsonElement>>(r, HttpStatusCode.Created)).Single();
    }

    private static async Task<List<JsonElement>> NotificacionesAsync(HttpClient c, bool soloNoLeidas = false)
        => (await c.GetFromJsonAsync<List<JsonElement>>($"/api/notificaciones?soloNoLeidas={soloNoLeidas.ToString().ToLowerInvariant()}", AyudaSesion.Json))!;

    private static bool Tiene(IEnumerable<JsonElement> lista, string tipo) => lista.Any(n => n.GetProperty("tipo").GetString() == tipo);

    private static async Task<JsonElement> MiOfertaAsync(HttpClient c, Guid id)
        => (await c.GetFromJsonAsync<List<JsonElement>>("/api/mis-ofertas", AyudaSesion.Json))!.Single(o => o.GetProperty("id").GetGuid() == id);

    // ── Flujo completo ──

    [Fact]
    public async Task Ofrecer_aceptar_y_aprobar_cubre_la_vacante_y_avisa_a_cada_parte()
    {
        var k = await CasoAsync();
        var (sustituto, cSustituto, _, _) = await k.E.ProfesionalConUsuarioAsync(nombre: "Luis");

        var oferta = await OfrecerUnoAsync(k.E.C, k.Vacante, sustituto, horas: 24);
        var id = oferta.GetProperty("id").GetGuid();
        Assert.Equal("Pendiente", oferta.GetProperty("estado").GetString());
        Assert.NotNull(oferta.GetProperty("caduca").GetString());
        Assert.Contains("Mañana", oferta.GetProperty("turno").GetString());
        Assert.StartsWith("Elena", oferta.GetProperty("profesionalAusente").GetString());

        // El profesional recibe el aviso y ve su oferta
        Assert.True(Tiene(await NotificacionesAsync(cSustituto, soloNoLeidas: true), "OfertaRecibida"));
        Assert.Equal("Pendiente", (await MiOfertaAsync(cSustituto, id)).GetProperty("estado").GetString());

        // Acepta: el coordinador recibe el aviso y la vacante sigue abierta (aún no hay asignación)
        var aceptada = await Planif.LeerAsync<JsonElement>(await cSustituto.PostAsJsonAsync($"/api/mis-ofertas/{id}/aceptar", new { motivo = "Puedo hacerlo" }));
        Assert.Equal("Aceptada", aceptada.GetProperty("estado").GetString());
        Assert.True(Tiene(await NotificacionesAsync(k.E.C), "OfertaAceptada"));
        Assert.Contains("Abierta", await k.E.C.GetStringAsync("/api/vacantes?estado=Abierta"));

        // El coordinador aprueba: asignación creada, vacante cubierta, oferta aprobada, plaza cubierta en el cuadrante
        var res = await Planif.LeerAsync<JsonElement>(await k.E.C.PostAsync($"/api/ofertas/{id}/aprobar", null), HttpStatusCode.OK);
        Assert.Equal(sustituto, res.GetProperty("profesionalId").GetGuid());
        Assert.Equal("Cubierta", (await k.E.C.GetFromJsonAsync<List<JsonElement>>("/api/vacantes?estado=Cubierta", AyudaSesion.Json))!.Single().GetProperty("estado").GetString());
        Assert.Equal("Aprobada", (await MiOfertaAsync(cSustituto, id)).GetProperty("estado").GetString());
        var turno = (await k.E.TurnosAsync(Dia, Dia)).Single(t => t.GetProperty("tipoTurnoId").GetGuid() == k.E.Manana);
        Assert.Equal(1, turno.GetProperty("plazasCubiertas").GetInt32());
        Assert.True(Tiene(await NotificacionesAsync(cSustituto), "SustitucionAprobada"));
    }

    [Fact]
    public async Task La_ausencia_genera_avisos_al_profesional_y_a_la_coordinacion()
    {
        var k = await CasoAsync();
        var delCoordinador = await NotificacionesAsync(k.E.C);
        Assert.True(Tiene(delCoordinador, "AusenciaSolicitada"));
        Assert.True(Tiene(delCoordinador, "VacanteAbierta"));
        var delProfesional = await NotificacionesAsync(k.ClienteAusente);
        Assert.True(Tiene(delProfesional, "AusenciaAprobada"));
        Assert.DoesNotContain("diagn", string.Join(" ", delProfesional.Concat(delCoordinador).Select(n => n.GetProperty("mensaje").GetString())), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Si_el_profesional_rechaza_la_vacante_sigue_abierta_y_quien_ofrecio_recibe_el_aviso()
    {
        var k = await CasoAsync();
        var (sustituto, cSustituto, _, _) = await k.E.ProfesionalConUsuarioAsync();
        var id = (await OfrecerUnoAsync(k.E.C, k.Vacante, sustituto)).GetProperty("id").GetGuid();

        var r = await Planif.LeerAsync<JsonElement>(await cSustituto.PostAsJsonAsync($"/api/mis-ofertas/{id}/rechazar", new { motivo = "No puedo" }));
        Assert.Equal("Rechazada", r.GetProperty("estado").GetString());
        Assert.Equal("No puedo", r.GetProperty("motivoRespuesta").GetString());
        Assert.True(Tiene(await NotificacionesAsync(k.E.C), "OfertaRechazada"));
        Assert.Single(await k.E.C.GetFromJsonAsync<List<JsonElement>>("/api/vacantes?estado=Abierta", AyudaSesion.Json) ?? []);

        // Una oferta rechazada ya no puede aceptarse ni aprobarse
        Assert.Equal(HttpStatusCode.Conflict, (await cSustituto.PostAsJsonAsync($"/api/mis-ofertas/{id}/aceptar", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await k.E.C.PostAsync($"/api/ofertas/{id}/aprobar", null)).StatusCode);
        // Se puede volver a ofrecer al mismo profesional
        await OfrecerUnoAsync(k.E.C, k.Vacante, sustituto);
    }

    [Fact]
    public async Task Al_aprobar_una_oferta_se_cierran_las_demas_y_sus_destinatarios_reciben_aviso()
    {
        var k = await CasoAsync();
        var (b, cB, _, _) = await k.E.ProfesionalConUsuarioAsync();
        var (c, cC, _, _) = await k.E.ProfesionalConUsuarioAsync();
        var r = await Planif.LeerAsync<List<JsonElement>>(await k.E.C.PostAsJsonAsync($"/api/vacantes/{k.Vacante}/ofertas", new { profesionalIds = new[] { b, c } }), HttpStatusCode.Created);
        Assert.Equal(2, r.Count);
        var idB = r.Single(o => o.GetProperty("profesionalId").GetGuid() == b).GetProperty("id").GetGuid();
        var idC = r.Single(o => o.GetProperty("profesionalId").GetGuid() == c).GetProperty("id").GetGuid();

        await cB.PostAsJsonAsync($"/api/mis-ofertas/{idB}/aceptar", new { });
        await cC.PostAsJsonAsync($"/api/mis-ofertas/{idC}/aceptar", new { });
        Assert.Equal(HttpStatusCode.OK, (await k.E.C.PostAsync($"/api/ofertas/{idB}/aprobar", null)).StatusCode);

        Assert.Equal("Cerrada", (await MiOfertaAsync(cC, idC)).GetProperty("estado").GetString());
        Assert.True(Tiene(await NotificacionesAsync(cC), "OfertaRetirada"));
        // La segunda aprobación ya no es posible: la vacante está cubierta
        Assert.Equal(HttpStatusCode.Conflict, (await k.E.C.PostAsync($"/api/ofertas/{idC}/aprobar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await cC.PostAsJsonAsync($"/api/mis-ofertas/{idC}/aceptar", new { })).StatusCode);
    }

    [Fact]
    public async Task Cubrir_directamente_la_vacante_cierra_las_ofertas_abiertas()
    {
        var k = await CasoAsync();
        var (b, cB, _, _) = await k.E.ProfesionalConUsuarioAsync();
        var otro = await k.E.ProfesionalAsync();
        var id = (await OfrecerUnoAsync(k.E.C, k.Vacante, b)).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.OK, (await k.E.C.PostAsJsonAsync($"/api/vacantes/{k.Vacante}/cubrir", new { profesionalId = otro })).StatusCode);
        Assert.Equal("Cerrada", (await MiOfertaAsync(cB, id)).GetProperty("estado").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await cB.PostAsJsonAsync($"/api/mis-ofertas/{id}/aceptar", new { })).StatusCode);
    }

    [Fact]
    public async Task El_coordinador_puede_retirar_o_denegar_y_el_profesional_lo_sabe()
    {
        var k = await CasoAsync();
        var (b, cB, _, _) = await k.E.ProfesionalConUsuarioAsync();
        var id = (await OfrecerUnoAsync(k.E.C, k.Vacante, b)).GetProperty("id").GetGuid();

        // Denegar exige que esté aceptada
        Assert.Equal(HttpStatusCode.Conflict, (await k.E.C.PostAsJsonAsync($"/api/ofertas/{id}/denegar", new { motivo = "No" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await k.E.C.PostAsync($"/api/ofertas/{id}/retirar", null)).StatusCode);
        Assert.Equal("Retirada", (await MiOfertaAsync(cB, id)).GetProperty("estado").GetString());
        Assert.True(Tiene(await NotificacionesAsync(cB), "OfertaRetirada"));

        var id2 = (await OfrecerUnoAsync(k.E.C, k.Vacante, b)).GetProperty("id").GetGuid();
        await cB.PostAsJsonAsync($"/api/mis-ofertas/{id2}/aceptar", new { });
        Assert.Equal(HttpStatusCode.BadRequest, (await k.E.C.PostAsJsonAsync($"/api/ofertas/{id2}/denegar", new { motivo = " " })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await k.E.C.PostAsJsonAsync($"/api/ofertas/{id2}/denegar", new { motivo = "Se prefiere otro perfil" })).StatusCode);
        var denegada = await MiOfertaAsync(cB, id2);
        Assert.Equal("Denegada", denegada.GetProperty("estado").GetString());
        Assert.Equal("Se prefiere otro perfil", denegada.GetProperty("motivoDecision").GetString());
        Assert.True(Tiene(await NotificacionesAsync(cB), "SustitucionDenegada"));
    }

    // ── Reglas ──

    [Fact]
    public async Task Solo_se_ofrece_a_candidatos_validos()
    {
        var k = await CasoAsync();
        var sinCompetencia = await k.E.ProfesionalAsync(conCompetencia: false);
        var sinUsuario = await k.E.ProfesionalAsync();
        var (b, _, _, _) = await k.E.ProfesionalConUsuarioAsync();

        async Task<HttpResponseMessage> Ofrecer(Guid p) => await k.E.C.PostAsJsonAsync($"/api/vacantes/{k.Vacante}/ofertas", new { profesionalIds = new[] { p } });
        Assert.Equal(HttpStatusCode.Conflict, (await Ofrecer(sinCompetencia)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Ofrecer(sinUsuario)).StatusCode);   // no podría responder
        Assert.Equal(HttpStatusCode.NotFound, (await Ofrecer(Guid.NewGuid())).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Ofrecer(b)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Ofrecer(b)).StatusCode);            // ya tiene una oferta abierta

        Assert.Equal(HttpStatusCode.BadRequest, (await k.E.C.PostAsJsonAsync($"/api/vacantes/{k.Vacante}/ofertas", new { profesionalIds = Array.Empty<Guid>() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await k.E.C.PostAsJsonAsync($"/api/vacantes/{k.Vacante}/ofertas", new { profesionalIds = new[] { b }, caducaEnHoras = 0 })).StatusCode);
    }

    [Fact]
    public async Task Al_aprobar_se_vuelven_a_evaluar_las_reglas_y_una_excepcion_justificada_permite_continuar()
    {
        var k = await CasoAsync();
        var (b, cB, _, _) = await k.E.ProfesionalConUsuarioAsync();
        var id = (await OfrecerUnoAsync(k.E.C, k.Vacante, b)).GetProperty("id").GetGuid();
        await cB.PostAsJsonAsync($"/api/mis-ofertas/{id}/aceptar", new { });

        // Entre la aceptación y la aprobación al profesional le aprueban unas vacaciones ese día
        var r = await k.E.C.PostAsJsonAsync("/api/ausencias", new { profesionalId = b, tipo = "Vacaciones", desde = Dia, hasta = Dia, aprobar = true });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);

        var bloqueada = await k.E.C.PostAsync($"/api/ofertas/{id}/aprobar", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, bloqueada.StatusCode);
        Assert.Contains("AusenciaAprobada", await bloqueada.Content.ReadAsStringAsync());
        Assert.Equal("Aceptada", (await MiOfertaAsync(cB, id)).GetProperty("estado").GetString());   // nada ha cambiado

        var conExcepcion = await k.E.C.PostAsJsonAsync($"/api/ofertas/{id}/aprobar", new { excepcion = new { justificacion = "El profesional se ofrece voluntario." } });
        Assert.Equal(HttpStatusCode.OK, conExcepcion.StatusCode);
        Assert.Equal("Aprobada", (await MiOfertaAsync(cB, id)).GetProperty("estado").GetString());
    }

    // ── Permisos, privacidad y aislamiento ──

    [Fact]
    public async Task Solo_la_coordinacion_gestiona_ofertas_y_cada_profesional_solo_ve_las_suyas()
    {
        var k = await CasoAsync();
        var (b, cB, _, _) = await k.E.ProfesionalConUsuarioAsync();
        var (c, cC, _, _) = await k.E.ProfesionalConUsuarioAsync();
        var (_, responsable) = await k.E.UsuarioAsync("ResponsableCentro");
        var id = (await OfrecerUnoAsync(k.E.C, k.Vacante, b)).GetProperty("id").GetGuid();

        foreach (var cliente in new[] { cB, responsable })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/ofertas")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync($"/api/vacantes/{k.Vacante}/ofertas", new { profesionalIds = new[] { c } })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsync($"/api/ofertas/{id}/aprobar", null)).StatusCode);
        }
        // Otro profesional no puede ver ni responder a la oferta de B
        Assert.Empty(await cC.GetFromJsonAsync<List<JsonElement>>("/api/mis-ofertas", AyudaSesion.Json) ?? []);
        Assert.Equal(HttpStatusCode.NotFound, (await cC.PostAsJsonAsync($"/api/mis-ofertas/{id}/aceptar", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cC.PostAsJsonAsync($"/api/mis-ofertas/{id}/rechazar", new { })).StatusCode);
    }

    [Fact]
    public async Task Las_ofertas_de_otra_sociedad_son_invisibles_e_intocables()
    {
        var k = await CasoAsync();
        var (b, _, _, _) = await k.E.ProfesionalConUsuarioAsync();
        var id = (await OfrecerUnoAsync(k.E.C, k.Vacante, b)).GetProperty("id").GetGuid();
        var otra = await Escenario.CrearAsync(fabrica);
        var propio = await otra.ProfesionalAsync();

        Assert.Empty(await otra.C.GetFromJsonAsync<List<JsonElement>>("/api/ofertas", AyudaSesion.Json) ?? []);
        Assert.Equal(HttpStatusCode.NotFound, (await otra.C.PostAsync($"/api/ofertas/{id}/aprobar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otra.C.PostAsync($"/api/ofertas/{id}/retirar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otra.C.PostAsJsonAsync($"/api/vacantes/{k.Vacante}/ofertas", new { profesionalIds = new[] { propio } })).StatusCode);
        Assert.Empty(await NotificacionesAsync(otra.C));
    }

    [Fact]
    public async Task Las_notificaciones_son_personales_y_se_pueden_marcar_como_leidas()
    {
        var k = await CasoAsync();
        var propias = await NotificacionesAsync(k.ClienteAusente, soloNoLeidas: true);
        Assert.NotEmpty(propias);
        var resumen = await k.ClienteAusente.GetFromJsonAsync<JsonElement>("/api/notificaciones/resumen", AyudaSesion.Json);
        Assert.Equal(propias.Count, resumen.GetProperty("noLeidas").GetInt32());

        // Otro usuario (aunque sea de la misma sociedad) no puede tocarlas
        var id = propias[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await k.E.C.PostAsync($"/api/notificaciones/{id}/leer", null)).StatusCode);
        Assert.DoesNotContain(await NotificacionesAsync(k.E.C), n => n.GetProperty("id").GetGuid() == id);

        Assert.Equal(HttpStatusCode.NoContent, (await k.ClienteAusente.PostAsync($"/api/notificaciones/{id}/leer", null)).StatusCode);
        Assert.Equal(propias.Count - 1, (await NotificacionesAsync(k.ClienteAusente, soloNoLeidas: true)).Count);
        Assert.Equal(HttpStatusCode.NoContent, (await k.ClienteAusente.PostAsync("/api/notificaciones/leer-todas", null)).StatusCode);
        Assert.Empty(await NotificacionesAsync(k.ClienteAusente, soloNoLeidas: true));
        Assert.Equal(HttpStatusCode.Unauthorized, (await fabrica.CreateClient().GetAsync("/api/notificaciones")).StatusCode);
    }

    [Fact]
    public async Task Dos_aprobaciones_simultaneas_de_ofertas_distintas_cubren_la_plaza_una_sola_vez()
    {
        var k = await CasoAsync();
        var (b, cB, _, _) = await k.E.ProfesionalConUsuarioAsync();
        var (c, cC, _, _) = await k.E.ProfesionalConUsuarioAsync();
        var r = await Planif.LeerAsync<List<JsonElement>>(await k.E.C.PostAsJsonAsync($"/api/vacantes/{k.Vacante}/ofertas", new { profesionalIds = new[] { b, c } }), HttpStatusCode.Created);
        var idB = r.Single(o => o.GetProperty("profesionalId").GetGuid() == b).GetProperty("id").GetGuid();
        var idC = r.Single(o => o.GetProperty("profesionalId").GetGuid() == c).GetProperty("id").GetGuid();
        await cB.PostAsJsonAsync($"/api/mis-ofertas/{idB}/aceptar", new { });
        await cC.PostAsJsonAsync($"/api/mis-ofertas/{idC}/aceptar", new { });

        var resultados = await Task.WhenAll(k.E.C.PostAsync($"/api/ofertas/{idB}/aprobar", null), k.E.C.PostAsync($"/api/ofertas/{idC}/aprobar", null));
        Assert.Single(resultados, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(resultados, x => x.StatusCode == HttpStatusCode.Conflict);
        var turno = (await k.E.TurnosAsync(Dia, Dia)).Single(t => t.GetProperty("tipoTurnoId").GetGuid() == k.E.Manana);
        Assert.Equal(1, turno.GetProperty("plazasCubiertas").GetInt32());
    }
}
