using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MedicalHub.Api.IntegrationTests;

[Collection(ColeccionApi.Nombre)]
public class AsignacionesTests(FabricaApi fabrica)
{
    [Fact]
    public async Task Asignar_y_quitar_actualiza_las_plazas_cubiertas_y_conserva_el_historial()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var cuadrante = await e.CuadranteAsync("2026-11-02", "2026-11-02");
        var necesidad = await e.NecesidadAsync("2026-11-02", e.Manana);
        var prof = await e.ProfesionalAsync(nombre: "Ana");

        var asignacion = await e.AsignarOkAsync(necesidad, prof);
        var turno = (await e.TurnosAsync("2026-11-02", "2026-11-02")).Single(t => t.GetProperty("tipoTurnoId").GetGuid() == e.Manana);
        Assert.Equal(1, turno.GetProperty("plazasCubiertas").GetInt32());
        Assert.StartsWith("Ana ", turno.GetProperty("necesidades")[0].GetProperty("asignaciones")[0].GetProperty("profesional").GetString());
        Assert.Equal(1, (await e.C.GetFromJsonAsync<JsonElement>($"/api/cuadrantes/{cuadrante.Id}", AyudaSesion.Json)).GetProperty("plazasCubiertas").GetInt32());

        Assert.Equal(HttpStatusCode.NoContent, (await e.C.DeleteAsync($"/api/asignaciones/{asignacion}")).StatusCode);
        turno = (await e.TurnosAsync("2026-11-02", "2026-11-02")).Single(t => t.GetProperty("tipoTurnoId").GetGuid() == e.Manana);
        Assert.Equal(0, turno.GetProperty("plazasCubiertas").GetInt32());

        // la plaza liberada puede volver a ocuparse por el mismo profesional (la cancelada no cuenta como duplicado)
        await e.AsignarOkAsync(necesidad, prof);
    }

    [Fact]
    public async Task No_se_puede_asignar_dos_veces_al_mismo_profesional_ni_superar_las_plazas()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-02");
        var necesidad = await e.NecesidadAsync("2026-11-02", e.Manana); // 2 plazas
        var a = await e.ProfesionalAsync();
        var b = await e.ProfesionalAsync();
        var c = await e.ProfesionalAsync();

        await e.AsignarOkAsync(necesidad, a);
        Assert.Equal(HttpStatusCode.Conflict, (await e.AsignarAsync(necesidad, a)).StatusCode);
        await e.AsignarOkAsync(necesidad, b);
        var llena = await e.AsignarAsync(necesidad, c);
        Assert.Equal(HttpStatusCode.Conflict, llena.StatusCode);
        Assert.Contains("plazas libres", await llena.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Las_condiciones_de_dominio_impiden_asignaciones_incompatibles()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-02");
        var manana = await e.NecesidadAsync("2026-11-02", e.Manana);
        var noche = await e.NecesidadAsync("2026-11-02", e.Noche); // exige categoría Adjunto

        async Task<string> Rechazo(Guid necesidad, Guid prof)
        {
            var r = await e.AsignarAsync(necesidad, prof);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
            return await r.Content.ReadAsStringAsync();
        }

        Assert.Contains("autorizado", await Rechazo(manana, await e.ProfesionalAsync(autorizado: false)));
        Assert.Contains("Falta la competencia", await Rechazo(manana, await e.ProfesionalAsync(conCompetencia: false)));
        Assert.Contains("inactivo", await Rechazo(manana, await e.ProfesionalAsync(activo: false)));
        Assert.Contains("no es la que exige esta necesidad", await Rechazo(noche, await e.ProfesionalAsync(categoria: e.Residente)));
    }

    [Fact]
    public async Task Un_profesional_no_puede_tener_turnos_solapados_ni_siquiera_en_otro_servicio()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-03");

        // Segundo servicio del mismo centro con un turno que solapa con la mañana del servicio 1
        var servicio2 = await Planif.CrearServicioAsync(e.C, e.Centro, competencias: [e.Competencia]);
        var tarde = (await Planif.LeerAsync<IdDto>(await e.C.PostAsJsonAsync($"/api/servicios/{servicio2}/tipos-turno",
            new { nombre = "Solapado", horaInicio = "14:00", horaFin = "20:00", esGuardia = false }), HttpStatusCode.Created)).Id;
        await e.C.PutAsJsonAsync($"/api/servicios/{servicio2}/requisitos", new { requisitos = new[] { new { tipoTurnoId = tarde, numeroProfesionales = 1, categoriaProfesionalId = (Guid?)null } } });
        await e.CuadranteAsync("2026-11-02", "2026-11-03", servicio: servicio2);

        var prof = await e.ProfesionalEnAsync([e.Servicio, servicio2]);
        var manana = await e.NecesidadAsync("2026-11-02", e.Manana);
        var solapada = (await e.TurnosAsync("2026-11-02", "2026-11-02")).Single(t => t.GetProperty("tipoTurnoId").GetGuid() == tarde).GetProperty("necesidades")[0].GetProperty("id").GetGuid();

        await e.AsignarOkAsync(manana, prof);
        var r = await e.AsignarAsync(solapada, prof);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
        Assert.Contains("se solapa", await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Turnos_contiguos_o_consecutivos_no_se_consideran_solapados()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-03");
        var prof = await e.ProfesionalAsync();

        // Noche del lunes (22:00 → 08:00 del martes) y mañana del martes (08:00 → 15:00): se tocan pero no se solapan.
        // (El descanso mínimo lo valorará el motor de reglas configurable de E4.)
        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-02", e.Noche), prof);
        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-03", e.Manana), prof);
    }

    [Fact]
    public async Task La_noche_que_cruza_la_medianoche_solapa_con_la_manana_del_dia_siguiente_si_se_adelanta()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-03");
        var prof = await e.ProfesionalAsync();
        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-02", e.Noche), prof);

        // Turno que empieza a las 07:00 del martes: solapa con el final de la noche del lunes
        var temprano = (await Planif.LeerAsync<IdDto>(await e.C.PostAsJsonAsync($"/api/servicios/{e.Servicio}/tipos-turno",
            new { nombre = "Temprano", horaInicio = "07:00", horaFin = "10:00", esGuardia = false }), HttpStatusCode.Created)).Id;
        var cuadrante = (await e.C.GetFromJsonAsync<List<JsonElement>>("/api/cuadrantes", AyudaSesion.Json))!.First(c => c.GetProperty("desde").GetString() == "2026-11-02").GetProperty("id").GetGuid();
        var nuevo = await Planif.LeerAsync<IdDto>(await e.C.PostAsJsonAsync($"/api/cuadrantes/{cuadrante}/turnos",
            new { fecha = "2026-11-03", tipoTurnoId = temprano, necesidades = new[] { new { numeroPlazas = 1, categoriaProfesionalId = (Guid?)null } } }), HttpStatusCode.Created);
        var necesidad = (await e.TurnosAsync("2026-11-03", "2026-11-03")).Single(t => t.GetProperty("id").GetGuid() == nuevo.Id).GetProperty("necesidades")[0].GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await e.AsignarAsync(necesidad, prof)).StatusCode);
    }

    [Fact]
    public async Task Los_candidatos_se_ordenan_y_explican_por_que_no_son_validos()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-02");
        var necesidad = await e.NecesidadAsync("2026-11-02", e.Manana);

        var libre = await e.ProfesionalAsync(nombre: "Libre");
        var ocupado = await e.ProfesionalAsync(nombre: "Ocupado");
        var sinComp = await e.ProfesionalAsync(conCompetencia: false, nombre: "SinComp");
        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-02", e.Manana, 0), ocupado);
        // ocupado ya está en esa necesidad: para el horario basta; creamos conflicto real con la noche→ no solapa; usamos la misma franja
        var candidatos = await e.C.GetFromJsonAsync<List<JsonElement>>($"/api/necesidades/{necesidad}/candidatos", AyudaSesion.Json);

        var porId = candidatos!.ToDictionary(c => c.GetProperty("profesionalId").GetGuid());
        Assert.True(porId[libre].GetProperty("elegible").GetBoolean());
        Assert.True(porId[libre].GetProperty("libre").GetBoolean());
        Assert.False(porId[ocupado].GetProperty("libre").GetBoolean());
        Assert.False(porId[ocupado].GetProperty("elegible").GetBoolean());
        Assert.Contains("solapa", string.Join(" ", porId[ocupado].GetProperty("motivos").EnumerateArray().Select(m => m.GetString())));
        Assert.Contains("Falta la competencia", string.Join(" ", porId[sinComp].GetProperty("motivos").EnumerateArray().Select(m => m.GetString())));
        Assert.True(candidatos!.First().GetProperty("elegible").GetBoolean()); // los válidos primero
    }

    // ── Concurrencia ──

    [Fact]
    public async Task Diez_aceptaciones_simultaneas_de_una_plaza_unica_solo_asignan_a_una_persona()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-02");
        var necesidad = await e.NecesidadAsync("2026-11-02", e.Noche); // 1 plaza
        var candidatos = new List<Guid>();
        for (var i = 0; i < 10; i++) candidatos.Add(await e.ProfesionalAsync());

        var respuestas = await Task.WhenAll(candidatos.Select(p => e.AsignarAsync(necesidad, p)));

        Assert.Equal(1, respuestas.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(9, respuestas.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        var turno = (await e.TurnosAsync("2026-11-02", "2026-11-02")).Single(t => t.GetProperty("tipoTurnoId").GetGuid() == e.Noche);
        Assert.Equal(1, turno.GetProperty("necesidades")[0].GetProperty("asignaciones").GetArrayLength());
    }

    [Fact]
    public async Task Un_mismo_profesional_solicitado_a_la_vez_en_dos_turnos_solapados_solo_acepta_uno()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-02");
        var turnos = (await e.TurnosAsync("2026-11-02", "2026-11-02")).Single(t => t.GetProperty("tipoTurnoId").GetGuid() == e.Manana);
        var necesidadA = turnos.GetProperty("necesidades")[0].GetProperty("id").GetGuid();

        // Segunda necesidad del mismo turno (misma franja horaria) → solapamiento seguro
        var turnoId = turnos.GetProperty("id").GetGuid();
        await e.C.PutAsJsonAsync($"/api/turnos/{turnoId}/necesidades", new
        {
            necesidades = new[] { new { numeroPlazas = 2, categoriaProfesionalId = (Guid?)null }, new { numeroPlazas = 1, categoriaProfesionalId = (Guid?)e.Residente } }
        });
        var necesidades = (await e.TurnosAsync("2026-11-02", "2026-11-02")).Single(t => t.GetProperty("id").GetGuid() == turnoId).GetProperty("necesidades");
        var ids = necesidades.EnumerateArray().Select(n => n.GetProperty("id").GetGuid()).ToList();
        Assert.Equal(2, ids.Count);

        var prof = await e.ProfesionalAsync(categoria: e.Residente);
        var respuestas = await Task.WhenAll(ids.Select(n => e.AsignarAsync(n, prof)));

        Assert.Equal(1, respuestas.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, respuestas.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity));
    }

    // ── Aislamiento y permisos ──

    [Fact]
    public async Task Otra_sociedad_no_ve_ni_modifica_cuadrantes_turnos_ni_asignaciones()
    {
        var a = await Escenario.CrearAsync(fabrica);
        var b = await Escenario.CrearAsync(fabrica);
        var cuadrante = await a.CuadranteAsync("2026-11-02", "2026-11-02");
        var necesidad = await a.NecesidadAsync("2026-11-02", a.Manana);
        var prof = await a.ProfesionalAsync();
        var asignacion = await a.AsignarOkAsync(necesidad, prof);
        var turnoId = (await a.TurnosAsync("2026-11-02", "2026-11-02"))[0].GetProperty("id").GetGuid();

        Assert.Empty(await b.TurnosAsync("2026-11-02", "2026-11-02"));
        Assert.Equal(HttpStatusCode.NotFound, (await b.C.GetAsync($"/api/cuadrantes/{cuadrante.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.C.PostAsync($"/api/cuadrantes/{cuadrante.Id}/publicar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.C.GetAsync($"/api/cuadrantes/{cuadrante.Id}/exportar.csv")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.C.DeleteAsync($"/api/cuadrantes/{cuadrante.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.C.DeleteAsync($"/api/turnos/{turnoId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.C.DeleteAsync($"/api/asignaciones/{asignacion}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.C.GetAsync($"/api/necesidades/{necesidad}/candidatos")).StatusCode);

        // Ni asignar a su propia plaza un profesional ajeno, ni su profesional a una plaza ajena
        var propia = await b.CuadranteAsync("2026-11-02", "2026-11-02");
        var necesidadB = await b.NecesidadAsync("2026-11-02", b.Manana);
        Assert.Equal(HttpStatusCode.NotFound, (await b.AsignarAsync(necesidadB, prof)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.AsignarAsync(necesidad, await b.ProfesionalAsync())).StatusCode);
        Assert.NotEqual(propia.Id, cuadrante.Id);

        // El cuadrante de A sigue intacto
        Assert.Equal(1, (await a.TurnosAsync("2026-11-02", "2026-11-02")).Single(t => t.GetProperty("tipoTurnoId").GetGuid() == a.Manana).GetProperty("plazasCubiertas").GetInt32());
    }

    [Fact]
    public async Task El_coordinador_elabora_cuadrantes_el_responsable_de_centro_solo_los_consulta_en_su_ambito()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var (_, coordinador) = await e.UsuarioAsync("CoordinadorMedico");
        var (responsable, resp) = await e.UsuarioAsync("ResponsableCentro");
        var (_, profUsuario) = await e.UsuarioAsync("ProfesionalSanitario");

        // El coordinador crea y asigna
        var r = await coordinador.PostAsJsonAsync("/api/cuadrantes", new { servicioId = e.Servicio, desde = "2026-11-02", hasta = "2026-11-02", generarTurnos = true });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var prof = await e.ProfesionalAsync();
        var necesidad = await e.NecesidadAsync("2026-11-02", e.Manana);
        Assert.Equal(HttpStatusCode.Created, (await e.AsignarAsync(necesidad, prof, coordinador)).StatusCode);

        // El responsable sin centros asignados no ve nada; no puede modificar
        Assert.Empty(await e.TurnosAsync("2026-11-02", "2026-11-02", cliente: resp));
        Assert.Equal(HttpStatusCode.Forbidden, (await resp.PostAsJsonAsync("/api/cuadrantes", new { servicioId = e.Servicio, desde = "2026-12-01", hasta = "2026-12-02" })).StatusCode);

        await e.C.PutAsJsonAsync($"/api/centros/{e.Centro}/responsables", new { usuariosIds = new[] { responsable.Id } });
        Assert.Equal(2, (await e.TurnosAsync("2026-11-02", "2026-11-02", cliente: resp)).Count);
        Assert.Equal(HttpStatusCode.Forbidden, (await e.AsignarAsync(necesidad, await e.ProfesionalAsync(), resp)).StatusCode);

        // El profesional sanitario no accede a la planificación general
        Assert.Equal(HttpStatusCode.Forbidden, (await profUsuario.GetAsync("/api/cuadrantes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await profUsuario.GetAsync("/api/turnos?desde=2026-11-02&hasta=2026-11-02")).StatusCode);
    }

    [Fact]
    public async Task Cada_profesional_solo_ve_su_propia_planificacion_y_solo_si_esta_publicada()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var pwd = AyudaSesion.ContrasenaValida("mp");
        var uA = await Ayudas.CrearUsuarioAsync(fabrica, e.Admin, "ProfesionalSanitario", contrasena: pwd);
        var uB = await Ayudas.CrearUsuarioAsync(fabrica, e.Admin, "ProfesionalSanitario", contrasena: pwd);
        var profA = await Planif.CrearProfesionalAsync(e.C, Planif.Profesional(e.Adjunto, competencias: [new { competenciaId = e.Competencia, vigenteHasta = (string?)null }], servicios: [e.Servicio], usuarioId: uA.Id));
        var profB = await Planif.CrearProfesionalAsync(e.C, Planif.Profesional(e.Adjunto, competencias: [new { competenciaId = e.Competencia, vigenteHasta = (string?)null }], servicios: [e.Servicio], usuarioId: uB.Id));

        var cuadrante = await e.CuadranteAsync("2026-11-02", "2026-11-02");
        var necesidad = await e.NecesidadAsync("2026-11-02", e.Manana);
        await e.AsignarOkAsync(necesidad, profA);
        await e.AsignarOkAsync(necesidad, profB);

        var cliA = (await AyudaSesion.LoginAsync(fabrica.CreateClient(), uA.Email, pwd)).Cliente(fabrica);
        var cliB = (await AyudaSesion.LoginAsync(fabrica.CreateClient(), uB.Email, pwd)).Cliente(fabrica);

        // En borrador no ven nada
        Assert.Empty((await cliA.GetFromJsonAsync<List<JsonElement>>("/api/mi-planificacion?desde=2026-11-02&hasta=2026-11-08", AyudaSesion.Json))!);

        await e.C.PostAsync($"/api/cuadrantes/{cuadrante.Id}/publicar", null);

        var miaA = (await cliA.GetFromJsonAsync<List<JsonElement>>("/api/mi-planificacion?desde=2026-11-02&hasta=2026-11-08", AyudaSesion.Json))!;
        Assert.Single(miaA);
        var asignaciones = miaA[0].GetProperty("necesidades")[0].GetProperty("asignaciones");
        Assert.Equal(1, asignaciones.GetArrayLength()); // no se ven los compañeros
        Assert.Equal(profA, asignaciones[0].GetProperty("profesionalId").GetGuid());
        Assert.Equal(profB, (await cliB.GetFromJsonAsync<List<JsonElement>>("/api/mi-planificacion?desde=2026-11-02&hasta=2026-11-08", AyudaSesion.Json))![0]
            .GetProperty("necesidades")[0].GetProperty("asignaciones")[0].GetProperty("profesionalId").GetGuid());
    }

    [Fact]
    public async Task Los_filtros_del_calendario_acotan_por_profesional_categoria_y_sin_cubrir()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-03");
        var prof = await e.ProfesionalAsync();
        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-02", e.Manana), prof);
        var nec2 = await e.NecesidadAsync("2026-11-02", e.Manana);
        await e.AsignarOkAsync(nec2, await e.ProfesionalAsync());

        // por profesional: solo el turno donde está asignado
        Assert.Single(await e.TurnosAsync("2026-11-02", "2026-11-03", $"&profesionalId={prof}"));
        // por categoría: solo turnos con necesidad de esa categoría (las noches exigen Adjunto)
        Assert.Equal(2, (await e.TurnosAsync("2026-11-02", "2026-11-03", $"&categoriaId={e.Adjunto}")).Count);
        // sin cubrir: la mañana del día 2 está completa (2/2)
        var sinCubrir = await e.TurnosAsync("2026-11-02", "2026-11-03", "&soloSinCubrir=true");
        Assert.Equal(3, sinCubrir.Count);
        // por servicio y centro
        Assert.Equal(4, (await e.TurnosAsync("2026-11-02", "2026-11-03", $"&servicioId={e.Servicio}&centroId={e.Centro}")).Count);
        Assert.Empty(await e.TurnosAsync("2026-11-02", "2026-11-03", $"&servicioId={Guid.NewGuid()}"));
    }
}
