using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MedicalHub.Api.IntegrationTests;

[Collection(ColeccionApi.Nombre)]
public class DisponibilidadTests(FabricaApi fabrica)
{
    private const int Lunes = 1, Martes = 2, Miercoles = 4;

    private static async Task<JsonElement> DeclararAsync(HttpClient c, string tipo, string desde, string hasta, int? dias = null, Guid? tipoTurnoId = null, string? comentario = null)
        => await Planif.LeerAsync<JsonElement>(await c.PostAsJsonAsync("/api/mi-disponibilidad", new { tipo, desde, hasta, dias, tipoTurnoId, comentario }), HttpStatusCode.Created);

    private static async Task AprobarAsync(HttpClient coordinador, JsonElement declaracion)
        => Assert.Equal(HttpStatusCode.OK, (await coordinador.PostAsJsonAsync($"/api/disponibilidades/{declaracion.GetProperty("id").GetGuid()}/aprobar", new { motivo = "Visto" })).StatusCode);

    private async Task<(Escenario e, Guid prof, HttpClient cliente)> ConCuadranteAsync()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-08");
        var (prof, cliente, _, _) = await e.ProfesionalConUsuarioAsync();
        return (e, prof, cliente);
    }

    // ── Declaración y aprobación ──

    [Fact]
    public async Task La_declaracion_empieza_como_declarada_y_el_coordinador_la_aprueba_o_rechaza()
    {
        var (e, prof, cliente) = await ConCuadranteAsync();
        var d = await DeclararAsync(cliente, "Bloqueo", "2026-11-04", "2026-11-04", comentario: "Curso externo");
        Assert.Equal("Declarada", d.GetProperty("estado").GetString());

        var pendientes = await e.C.GetFromJsonAsync<List<JsonElement>>("/api/disponibilidades?estado=Declarada", AyudaSesion.Json);
        Assert.Contains(pendientes!, x => x.GetProperty("profesionalId").GetGuid() == prof);

        var aprobada = await Planif.LeerAsync<JsonElement>(await e.C.PostAsJsonAsync($"/api/disponibilidades/{d.GetProperty("id").GetGuid()}/aprobar", new { motivo = "Ok" }));
        Assert.Equal("Aprobada", aprobada.GetProperty("estado").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await e.C.PostAsync($"/api/disponibilidades/{d.GetProperty("id").GetGuid()}/rechazar", null)).StatusCode);

        var otra = await DeclararAsync(cliente, "Evita", "2026-11-05", "2026-11-05");
        var rechazada = await Planif.LeerAsync<JsonElement>(await e.C.PostAsJsonAsync($"/api/disponibilidades/{otra.GetProperty("id").GetGuid()}/rechazar", new { motivo = "No viable" }));
        Assert.Equal("Rechazada", rechazada.GetProperty("estado").GetString());
        Assert.Equal("No viable", rechazada.GetProperty("motivoDecision").GetString());
    }

    [Fact]
    public async Task El_profesional_no_decide_ni_ve_las_de_otros_y_solo_retira_las_no_aprobadas()
    {
        var (e, _, cliente) = await ConCuadranteAsync();
        var d = await DeclararAsync(cliente, "Bloqueo", "2026-11-04", "2026-11-04");
        var id = d.GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsync($"/api/disponibilidades/{id}/aprobar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/disponibilidades")).StatusCode);

        // Aprobada: el profesional ya no puede borrarla; el coordinador sí
        await AprobarAsync(e.C, d);
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.DeleteAsync($"/api/mi-disponibilidad/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await e.C.DeleteAsync($"/api/disponibilidades/{id}")).StatusCode);

        // Sin aprobar sí la puede retirar
        var otra = await DeclararAsync(cliente, "Prefiere", "2026-11-06", "2026-11-06");
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.DeleteAsync($"/api/mi-disponibilidad/{otra.GetProperty("id").GetGuid()}")).StatusCode);
        Assert.Empty(await cliente.GetFromJsonAsync<List<JsonElement>>("/api/mi-disponibilidad", AyudaSesion.Json) ?? []);
    }

    [Fact]
    public async Task Las_declaraciones_validan_fechas_dias_y_tipo_de_turno()
    {
        var (e, _, cliente) = await ConCuadranteAsync();
        async Task<HttpStatusCode> Intentar(object cuerpo) => (await cliente.PostAsJsonAsync("/api/mi-disponibilidad", cuerpo)).StatusCode;

        Assert.Equal(HttpStatusCode.UnprocessableEntity, await Intentar(new { tipo = "Bloqueo", desde = "2026-11-05", hasta = "2026-11-01" }));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, await Intentar(new { tipo = "Bloqueo", desde = "2026-11-01", hasta = "2026-11-05", dias = 0 }));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, await Intentar(new { tipo = "Bloqueo", desde = "2026-11-01", hasta = "2026-11-05", dias = 200 }));
        Assert.Equal(HttpStatusCode.BadRequest, await Intentar(new { tipo = "Bloqueo", desde = "2026-11-01", hasta = "2026-11-05", tipoTurnoId = Guid.NewGuid() }));

        // Un tipo de turno de un servicio donde NO está autorizado tampoco
        var otroServicio = await Planif.CrearServicioAsync(e.C, e.Centro);
        var ajeno = (await Planif.LeerAsync<IdDto>(await e.C.PostAsJsonAsync($"/api/servicios/{otroServicio}/tipos-turno", new { nombre = "Ajeno", horaInicio = "08:00", horaFin = "15:00", esGuardia = false }), HttpStatusCode.Created)).Id;
        Assert.Equal(HttpStatusCode.BadRequest, await Intentar(new { tipo = "Bloqueo", desde = "2026-11-01", hasta = "2026-11-05", tipoTurnoId = ajeno }));
        Assert.Equal(HttpStatusCode.Created, await Intentar(new { tipo = "Bloqueo", desde = "2026-11-01", hasta = "2026-11-05", tipoTurnoId = e.Noche }));
    }

    [Fact]
    public async Task El_profesional_ve_los_tipos_de_turno_de_sus_servicios_autorizados()
    {
        var (e, _, cliente) = await ConCuadranteAsync();
        var propios = await cliente.GetFromJsonAsync<List<JsonElement>>("/api/mis-tipos-turno", AyudaSesion.Json);
        Assert.Equal(new[] { "Mañana", "Noche" }.Order(), propios!.Select(t => t.GetProperty("nombre").GetString()).Order());
        Assert.Contains(propios!, t => t.GetProperty("franja").GetString() == "22:00–08:00");
        Assert.NotNull(e);
    }

    [Fact]
    public async Task El_lote_declara_varios_bloques_por_hospital_y_un_hospital_ajeno_no_guarda_ninguno()
    {
        var (e, _, cliente) = await ConCuadranteAsync();
        var lote = await Planif.LeerAsync<List<JsonElement>>(await cliente.PostAsJsonAsync("/api/mi-disponibilidad/lote", new
        {
            bloques = new object[]
            {
                new { tipo = "Disponible", desde = "2026-11-01", hasta = "2026-11-30", dias = Lunes | Miercoles, tipoTurnoId = e.Manana, centroId = e.Centro },
                new { tipo = "Disponible", desde = "2026-11-01", hasta = "2026-11-30", dias = Martes, centroId = e.Centro },
            }
        }), HttpStatusCode.Created);
        Assert.Equal(2, lote.Count);
        Assert.All(lote, d => Assert.Equal(e.Centro, d.GetProperty("centroId").GetGuid()));
        Assert.False(string.IsNullOrEmpty(lote[0].GetProperty("centro").GetString()));

        // Un bloque inválido (hospital inexistente) invalida todo el lote
        var antes = (await cliente.GetFromJsonAsync<List<JsonElement>>("/api/mi-disponibilidad", AyudaSesion.Json))!.Count;
        var mal = await cliente.PostAsJsonAsync("/api/mi-disponibilidad/lote", new
        {
            bloques = new object[]
            {
                new { tipo = "Disponible", desde = "2026-12-01", hasta = "2026-12-31", dias = Lunes, centroId = e.Centro },
                new { tipo = "Disponible", desde = "2026-12-01", hasta = "2026-12-31", dias = Lunes, centroId = Guid.NewGuid() },
            }
        });
        Assert.Equal(HttpStatusCode.BadRequest, mal.StatusCode);
        Assert.Equal(antes, (await cliente.GetFromJsonAsync<List<JsonElement>>("/api/mi-disponibilidad", AyudaSesion.Json))!.Count);
    }

    [Fact]
    public async Task Un_bloqueo_de_hospital_aprobado_afecta_a_los_turnos_de_ese_hospital()
    {
        var (e, prof, cliente) = await ConCuadranteAsync();
        var lote = await Planif.LeerAsync<List<JsonElement>>(await cliente.PostAsJsonAsync("/api/mi-disponibilidad/lote", new
        {
            bloques = new object[] { new { tipo = "Bloqueo", desde = "2026-11-04", hasta = "2026-11-04", centroId = e.Centro } }
        }), HttpStatusCode.Created);
        await AprobarAsync(e.C, lote[0]);

        var r = await e.AsignarAsync(await e.NecesidadAsync("2026-11-04", e.Manana), prof);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
    }

    private static async Task<JsonElement> DeclararEnHospitalAsync(HttpClient c, string fecha, Guid centro, Guid? tipoTurno, string tipo = "Disponible")
        => (await Planif.LeerAsync<List<JsonElement>>(await c.PostAsJsonAsync("/api/mi-disponibilidad/lote", new
        {
            bloques = new object[] { new { tipo, desde = fecha, hasta = fecha, centroId = centro, tipoTurnoId = tipoTurno } }
        }), HttpStatusCode.Created))[0];

    [Fact]
    public async Task La_disponibilidad_aprobada_asigna_sola_al_turno_de_su_hospital_y_si_se_pasa_del_tope_queda_en_rojo()
    {
        var (e, prof, cliente) = await ConCuadranteAsync();
        var turno = async () => (await e.TurnosAsync("2026-11-04", "2026-11-04")).Single(t => t.GetProperty("tipoTurnoId").GetGuid() == e.Manana);

        // Pendiente de aprobar: no asigna. Para otro tipo de turno, tampoco
        var d1 = await DeclararEnHospitalAsync(cliente, "2026-11-04", e.Centro, e.Manana);
        Assert.Equal(0, (await turno()).GetProperty("plazasCubiertas").GetInt32());
        await AprobarAsync(e.C, await DeclararEnHospitalAsync(cliente, "2026-11-05", e.Centro, e.Noche)); // la noche del 5 no es la mañana del 4
        Assert.Equal(0, (await turno()).GetProperty("plazasCubiertas").GetInt32());

        // Aprobada: el profesional aparece asignado al turno (2 plazas: mañana)
        await AprobarAsync(e.C, d1);
        var t1 = await turno();
        Assert.Equal(1, t1.GetProperty("plazasCubiertas").GetInt32());
        Assert.Equal(0, t1.GetProperty("plazasExcedidas").GetInt32());
        Assert.Equal(prof, t1.GetProperty("necesidades")[0].GetProperty("asignaciones")[0].GetProperty("profesionalId").GetGuid());

        // Con dos más se completa el tope y el siguiente lo supera: se asigna igualmente, pero marcado en rojo para el administrador
        var segundo = await e.ProfesionalConUsuarioAsync(nombre: "Segundo");
        var tercero = await e.ProfesionalConUsuarioAsync(nombre: "Tercero");
        await AprobarAsync(e.C, await DeclararEnHospitalAsync(segundo.cliente, "2026-11-04", e.Centro, e.Manana));
        Assert.Equal(2, (await turno()).GetProperty("plazasCubiertas").GetInt32());
        await AprobarAsync(e.C, await DeclararEnHospitalAsync(tercero.cliente, "2026-11-04", e.Centro, e.Manana));
        var t3 = await turno();
        Assert.Equal(2, t3.GetProperty("plazasCubiertas").GetInt32());
        Assert.Equal(1, t3.GetProperty("plazasExcedidas").GetInt32());
        Assert.True(t3.GetProperty("excedido").GetBoolean());
        Assert.Equal(3, t3.GetProperty("necesidades").EnumerateArray().Sum(n => n.GetProperty("asignaciones").GetArrayLength()));
    }

    [Fact]
    public async Task Un_turno_creado_despues_recoge_a_quienes_ya_tienen_la_disponibilidad_aprobada()
    {
        var (e, prof, cliente) = await ConCuadranteAsync();
        var segundo = await e.CuadranteAsync("2026-11-16", "2026-11-22", generar: false);
        await AprobarAsync(e.C, await DeclararEnHospitalAsync(cliente, "2026-11-17", e.Centro, null));   // cualquier turno del hospital

        var r = await e.C.PostAsJsonAsync($"/api/cuadrantes/{segundo.Id}/turnos", new { fecha = "2026-11-17", tipoTurnoId = e.Manana });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var turno = (await e.TurnosAsync("2026-11-17", "2026-11-17")).Single();
        Assert.Equal(prof, turno.GetProperty("necesidades")[0].GetProperty("asignaciones")[0].GetProperty("profesionalId").GetGuid());
    }

    [Fact]
    public async Task El_dashboard_muestra_el_semaforo_de_cobertura_con_los_turnos_pasados_de_tope_en_rojo()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var dia = DateTime.UtcNow.Date.AddDays(1);
        await e.CuadranteAsync($"{dia:yyyy-MM-dd}", $"{dia.AddDays(2):yyyy-MM-dd}");
        for (var i = 0; i < 3; i++)   // 3 disponibles para una mañana de 2 plazas
        {
            var p = await e.ProfesionalConUsuarioAsync(nombre: $"Prof{i}");
            await AprobarAsync(e.C, await DeclararEnHospitalAsync(p.cliente, $"{dia:yyyy-MM-dd}", e.Centro, e.Manana));
        }

        var gestion = (await e.C.GetFromJsonAsync<JsonElement>("/api/dashboard", AyudaSesion.Json)).GetProperty("gestion");
        var semaforo = gestion.GetProperty("semaforo");
        Assert.Equal(1, semaforo.GetProperty("rojo").GetInt32());
        Assert.Equal(0, semaforo.GetProperty("verde").GetInt32());
        Assert.Equal(5, semaforo.GetProperty("amarillo").GetInt32());   // el resto de turnos de la ventana aún tiene plazas libres
        var primero = gestion.GetProperty("atencion")[0];
        Assert.Equal("Rojo", primero.GetProperty("estado").GetString());     // los rojos van primero
        Assert.Contains("3/2", primero.GetProperty("detalle").GetString());
    }

    // ── Efecto en las reglas: solo lo aprobado, y las preferencias no son prohibiciones ──

    [Fact]
    public async Task Un_bloqueo_pendiente_no_cuenta_pero_aprobado_exige_excepcion()
    {
        var (e, prof, cliente) = await ConCuadranteAsync();
        var d = await DeclararAsync(cliente, "Bloqueo", "2026-11-04", "2026-11-04");
        var necesidad = await e.NecesidadAsync("2026-11-04", e.Manana);

        var libre = await e.AsignarAsync(necesidad, prof);                         // sin aprobar: no cuenta
        Assert.Equal(HttpStatusCode.Created, libre.StatusCode);
        await e.C.DeleteAsync($"/api/asignaciones/{(await libre.Content.ReadFromJsonAsync<JsonElement>(AyudaSesion.Json)).GetProperty("id").GetGuid()}");

        await AprobarAsync(e.C, d);
        var r = await e.AsignarAsync(necesidad, prof);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
        var problema = await r.Content.ReadFromJsonAsync<JsonElement>(AyudaSesion.Json);
        Assert.Equal("requiere_excepcion", problema.GetProperty("codigo").GetString());
        Assert.Equal("DisponibilidadBloqueada", problema.GetProperty("violaciones")[0].GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task Una_preferencia_por_evitar_avisa_pero_no_impide_asignar()
    {
        var (e, prof, cliente) = await ConCuadranteAsync();
        await AprobarAsync(e.C, await DeclararAsync(cliente, "Evita", "2026-11-04", "2026-11-04"));

        var r = await Planif.LeerAsync<JsonElement>(await e.AsignarAsync(await e.NecesidadAsync("2026-11-04", e.Manana), prof), HttpStatusCode.Created);
        var aviso = Assert.Single(r.GetProperty("advertencias").EnumerateArray());
        Assert.Equal("PreferenciaEvitar", aviso.GetProperty("codigo").GetString());
        Assert.Empty(r.GetProperty("excepciones").EnumerateArray());
    }

    [Fact]
    public async Task La_sociedad_puede_convertir_la_preferencia_en_obligatoria_con_una_regla()
    {
        var (e, prof, cliente) = await ConCuadranteAsync();
        await AprobarAsync(e.C, await DeclararAsync(cliente, "Evita", "2026-11-04", "2026-11-04"));
        var regla = await e.C.PostAsJsonAsync("/api/reglas-planificacion", new { tipo = "PreferenciaEvitar", nombre = "Respetar preferencias", severidad = "Bloqueante", permiteExcepcion = true });
        Assert.Equal(HttpStatusCode.Created, regla.StatusCode);

        var r = await e.AsignarAsync(await e.NecesidadAsync("2026-11-04", e.Manana), prof);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
        Assert.Contains("prefiere evitar", await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task El_bloqueo_respeta_los_dias_de_la_semana_y_el_tipo_de_turno()
    {
        var (e, prof, cliente) = await ConCuadranteAsync();
        // Bloquea los lunes y miércoles de la semana (02 = lunes, 04 = miércoles) solo para las noches
        await AprobarAsync(e.C, await DeclararAsync(cliente, "Bloqueo", "2026-11-02", "2026-11-08", Lunes | Miercoles, e.Noche));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await e.AsignarAsync(await e.NecesidadAsync("2026-11-02", e.Noche), prof)).StatusCode);   // lunes noche
        Assert.Equal(HttpStatusCode.Created, (await e.AsignarAsync(await e.NecesidadAsync("2026-11-02", e.Manana), prof)).StatusCode);              // lunes mañana: otro tipo
        Assert.Equal(HttpStatusCode.Created, (await e.AsignarAsync(await e.NecesidadAsync("2026-11-03", e.Noche), prof)).StatusCode);              // martes noche: otro día
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await e.AsignarAsync(await e.NecesidadAsync("2026-11-04", e.Noche), prof)).StatusCode);  // miércoles noche
        Assert.NotEqual(Martes, Lunes);
    }

    [Fact]
    public async Task Los_candidatos_muestran_quien_prefiere_el_turno_o_ha_declarado_disponibilidad_y_van_primero()
    {
        var (e, prof, cliente) = await ConCuadranteAsync();
        var neutro = await e.ProfesionalAsync(nombre: "Aaa");                      // orden alfabético: iría antes
        await AprobarAsync(e.C, await DeclararAsync(cliente, "Prefiere", "2026-11-04", "2026-11-04", comentario: "Me viene bien"));

        var candidatos = (await e.C.GetFromJsonAsync<List<JsonElement>>($"/api/necesidades/{await e.NecesidadAsync("2026-11-04", e.Manana)}/candidatos", AyudaSesion.Json))!;
        var porId = candidatos.ToDictionary(c => c.GetProperty("profesionalId").GetGuid());

        Assert.Contains("Prefiere este turno.", porId[prof].GetProperty("notas").EnumerateArray().Select(n => n.GetString()));
        Assert.Empty(porId[neutro].GetProperty("notas").EnumerateArray());
        var orden = candidatos.Select(c => c.GetProperty("profesionalId").GetGuid()).ToList();
        Assert.True(orden.IndexOf(prof) < orden.IndexOf(neutro));
    }

    [Fact]
    public async Task La_disponibilidad_de_otra_sociedad_no_se_ve_ni_se_modifica()
    {
        var (a, _, clienteA) = await ConCuadranteAsync();
        var b = await Escenario.CrearAsync(fabrica);
        var d = await DeclararAsync(clienteA, "Bloqueo", "2026-11-04", "2026-11-04");
        var id = d.GetProperty("id").GetGuid();

        Assert.Empty(await b.C.GetFromJsonAsync<List<JsonElement>>("/api/disponibilidades", AyudaSesion.Json) ?? []);
        Assert.Equal(HttpStatusCode.NotFound, (await b.C.PostAsync($"/api/disponibilidades/{id}/aprobar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.C.DeleteAsync($"/api/disponibilidades/{id}")).StatusCode);
        Assert.NotNull(a);
    }

    [Fact]
    public async Task El_coordinador_puede_registrar_disponibilidad_de_un_profesional_ya_aprobada()
    {
        var (e, prof, _) = await ConCuadranteAsync();
        var r = await Planif.LeerAsync<JsonElement>(await e.C.PostAsJsonAsync("/api/disponibilidades",
            new { profesionalId = prof, tipo = "Bloqueo", desde = "2026-11-04", hasta = "2026-11-04", aprobar = true }), HttpStatusCode.Created);
        Assert.Equal("Aprobada", r.GetProperty("estado").GetString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await e.AsignarAsync(await e.NecesidadAsync("2026-11-04", e.Manana), prof)).StatusCode);
    }
}
