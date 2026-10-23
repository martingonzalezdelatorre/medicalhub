using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MedicalHub.Api.IntegrationTests;

[Collection(ColeccionApi.Nombre)]
public class HorasInformesTests(FabricaApi fabrica)
{
    // Semana ya pasada (octubre, horario de verano en Madrid): el lunes 5 hay noche 22:00–08:00 y el martes 6 mañana 08:00–15:00
    private const string Lunes = "2026-10-05";
    private const string Martes = "2026-10-06";

    private async Task<(Escenario e, Guid prof, HttpClient cliente, Guid asigNoche, Guid asigManana)> CasoAsync()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-10-05", "2026-10-11");
        var (prof, cliente, _, _) = await e.ProfesionalConUsuarioAsync(nombre: "Marta");
        var noche = await e.AsignarOkAsync(await e.NecesidadAsync(Lunes, e.Noche), prof);
        var manana = await e.AsignarOkAsync(await e.NecesidadAsync(Martes, e.Manana), prof);
        return (e, prof, cliente, noche, manana);
    }

    private static string Periodo(string desde = "2026-10-05", string hasta = "2026-10-11") => $"desde={desde}&hasta={hasta}";

    // ── Horas planificadas, registradas y ajustes ──

    [Fact]
    public async Task El_informe_calcula_horas_planificadas_y_nocturnas_en_hora_local_del_centro()
    {
        var (e, prof, _, _, _) = await CasoAsync();
        var informe = await e.C.GetFromJsonAsync<JsonElement>($"/api/informes/horas?{Periodo()}", AyudaSesion.Json);
        var fila = Assert.Single(informe.GetProperty("filas").EnumerateArray());
        Assert.Equal(prof, fila.GetProperty("profesionalId").GetGuid());
        Assert.Equal(2, fila.GetProperty("turnos").GetInt32());
        Assert.Equal(600 + 420, fila.GetProperty("planificadasMin").GetInt32());     // noche de 10 h + mañana de 7 h
        Assert.Equal(480, fila.GetProperty("nocturnasPlanMin").GetInt32());           // 22:00–06:00
        Assert.Equal(0, fila.GetProperty("turnosRegistrados").GetInt32());
        Assert.Equal(1020, informe.GetProperty("total").GetProperty("planificadasMin").GetInt32());
    }

    [Fact]
    public async Task Registrar_horas_reales_y_ajustarlas_deja_rastro_y_actualiza_el_informe()
    {
        var (e, _, _, noche, _) = await CasoAsync();
        // Noche del 5/10: 22:00 local = 20:00Z. El profesional trabajó hasta las 08:30 local (06:30Z): 10,5 h
        var registro = await e.C.PutAsJsonAsync($"/api/asignaciones/{noche}/horas", new { inicioUtc = "2026-10-05T20:00:00Z", finUtc = "2026-10-06T06:30:00Z" });
        var linea = await Planif.LeerAsync<JsonElement>(registro);
        Assert.Equal(630, linea.GetProperty("realesMin").GetInt32());
        Assert.Equal(480, linea.GetProperty("nocturnasRealMin").GetInt32());
        Assert.Equal(630, linea.GetProperty("efectivasMin").GetInt32());

        // Ajuste con motivo; no se puede sin motivo suficiente
        var corto = await e.C.PostAsJsonAsync($"/api/asignaciones/{noche}/horas/ajustes", new { minutos = -30, motivo = "corto" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, corto.StatusCode);
        var ajustada = await Planif.LeerAsync<JsonElement>(
            await e.C.PostAsJsonAsync($"/api/asignaciones/{noche}/horas/ajustes", new { minutos = -30, motivo = "Se descuenta el retraso de la relevista" }), HttpStatusCode.Created);
        Assert.Equal(600, ajustada.GetProperty("efectivasMin").GetInt32());
        var ajuste = Assert.Single(ajustada.GetProperty("ajustes").EnumerateArray());
        Assert.Equal("Admin Sociedad", ajuste.GetProperty("usuario").GetString());
        Assert.Equal(-30, ajuste.GetProperty("minutos").GetInt32());

        // Corregir el registro conserva los ajustes ya hechos
        var corregida = await Planif.LeerAsync<JsonElement>(await e.C.PutAsJsonAsync($"/api/asignaciones/{noche}/horas", new { inicioUtc = "2026-10-05T20:00:00Z", finUtc = "2026-10-06T06:00:00Z" }));
        Assert.Equal(570, corregida.GetProperty("efectivasMin").GetInt32());

        var informe = await e.C.GetFromJsonAsync<JsonElement>($"/api/informes/horas?{Periodo()}", AyudaSesion.Json);
        var fila = informe.GetProperty("filas")[0];
        Assert.Equal(1, fila.GetProperty("turnosRegistrados").GetInt32());
        Assert.Equal(600, fila.GetProperty("realesMin").GetInt32());
        Assert.Equal(-30, fila.GetProperty("ajustesMin").GetInt32());
        Assert.Equal(570, fila.GetProperty("efectivasMin").GetInt32());
    }

    [Fact]
    public async Task Las_horas_se_validan_y_no_se_registran_turnos_futuros_ni_ajustes_sin_registro()
    {
        var (e, prof, _, noche, _) = await CasoAsync();
        // Ajustar sin registro previo
        Assert.Equal(HttpStatusCode.Conflict, (await e.C.PostAsJsonAsync($"/api/asignaciones/{noche}/horas/ajustes", new { minutos = 10, motivo = "Motivo suficiente aquí" })).StatusCode);
        // Fin anterior al inicio y más de 24 h
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await e.C.PutAsJsonAsync($"/api/asignaciones/{noche}/horas", new { inicioUtc = "2026-10-06T06:00:00Z", finUtc = "2026-10-05T20:00:00Z" })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await e.C.PutAsJsonAsync($"/api/asignaciones/{noche}/horas", new { inicioUtc = "2026-10-04T20:00:00Z", finUtc = "2026-10-06T06:00:00Z" })).StatusCode);

        // Un turno que no ha empezado todavía
        await e.CuadranteAsync("2027-05-03", "2027-05-09");
        var futura = await e.AsignarOkAsync(await e.NecesidadAsync("2027-05-04", e.Manana), prof);
        Assert.Equal(HttpStatusCode.Conflict, (await e.C.PutAsJsonAsync($"/api/asignaciones/{futura}/horas", new { inicioUtc = "2027-05-04T06:00:00Z", finUtc = "2027-05-04T13:00:00Z" })).StatusCode);
        // Asignación inexistente
        Assert.Equal(HttpStatusCode.NotFound, (await e.C.PutAsJsonAsync($"/api/asignaciones/{Guid.NewGuid()}/horas", new { inicioUtc = "2026-10-05T20:00:00Z", finUtc = "2026-10-06T06:00:00Z" })).StatusCode);
    }

    [Fact]
    public async Task El_listado_y_los_informes_validan_el_periodo()
    {
        var e = await Escenario.CrearAsync(fabrica);
        Assert.Equal(HttpStatusCode.BadRequest, (await e.C.GetAsync("/api/informes/horas?desde=2026-10-11&hasta=2026-10-05")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await e.C.GetAsync("/api/informes/cobertura?desde=2025-01-01&hasta=2026-12-31")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await e.C.GetAsync("/api/horas?desde=2025-01-01&hasta=2026-12-31")).StatusCode);
    }

    [Fact]
    public async Task Cada_profesional_ve_solo_sus_propias_horas()
    {
        var (e, _, cliente, noche, _) = await CasoAsync();
        var (_, otro, _, _) = await e.ProfesionalConUsuarioAsync(nombre: "Otro");
        await e.C.PutAsJsonAsync($"/api/asignaciones/{noche}/horas", new { inicioUtc = "2026-10-05T20:00:00Z", finUtc = "2026-10-06T06:00:00Z" });

        var mias = await cliente.GetFromJsonAsync<List<JsonElement>>($"/api/mis-horas?{Periodo()}", AyudaSesion.Json);
        Assert.Equal(2, mias!.Count);
        Assert.Contains(mias, l => l.GetProperty("registroId").ValueKind == JsonValueKind.String);
        Assert.Empty(await otro.GetFromJsonAsync<List<JsonElement>>($"/api/mis-horas?{Periodo()}", AyudaSesion.Json) ?? []);
    }

    // ── Cobertura ──

    [Fact]
    public async Task El_informe_de_cobertura_cuenta_plazas_cubiertas_y_vacantes_abiertas()
    {
        var (e, _, _, _, _) = await CasoAsync();
        var informe = await e.C.GetFromJsonAsync<JsonElement>($"/api/informes/cobertura?{Periodo()}", AyudaSesion.Json);
        var fila = Assert.Single(informe.GetProperty("filas").EnumerateArray());
        Assert.Equal(21, fila.GetProperty("plazas").GetInt32());          // 7 días × (2 de mañana + 1 de noche)
        Assert.Equal(2, fila.GetProperty("cubiertas").GetInt32());
        Assert.Equal(19, fila.GetProperty("libres").GetInt32());
        Assert.Equal(9.5, fila.GetProperty("porcentajeCobertura").GetDouble());
        Assert.Equal(0, fila.GetProperty("vacantesAbiertas").GetInt32());

    }

    // ── CSV y permisos ──

    [Fact]
    public async Task Los_informes_se_exportan_a_csv_con_bom_y_quedan_registrados()
    {
        var (e, _, _, _, _) = await CasoAsync();
        var r = await e.C.GetAsync($"/api/informes/horas.csv?{Periodo()}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var bytes = await r.Content.ReadAsByteArrayAsync();
        Assert.Equal([0xEF, 0xBB, 0xBF], bytes.Take(3).ToArray());
        var texto = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        Assert.StartsWith("Profesional;Turnos;Guardias;Horas planificadas", texto);
        Assert.Contains("17,00", texto);            // 1020 min
        Assert.Contains("Total;", texto);

        var cobertura = await e.C.GetAsync($"/api/informes/cobertura.csv?{Periodo()}");
        Assert.Equal(HttpStatusCode.OK, cobertura.StatusCode);
        Assert.Contains("% cobertura", Encoding.UTF8.GetString(await cobertura.Content.ReadAsByteArrayAsync()));
    }

    [Fact]
    public async Task Los_permisos_separan_lectura_gestion_y_profesional()
    {
        var (e, _, cliente, noche, _) = await CasoAsync();
        var (_, responsable) = await e.UsuarioAsync("ResponsableCentro");
        var (_, coordinador) = await e.UsuarioAsync("CoordinadorMedico");

        // El profesional no ve informes ni registra horas
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync($"/api/informes/horas?{Periodo()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync($"/api/horas?{Periodo()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PutAsJsonAsync($"/api/asignaciones/{noche}/horas", new { inicioUtc = "2026-10-05T20:00:00Z", finUtc = "2026-10-06T06:00:00Z" })).StatusCode);
        // El responsable de centro lee (con su alcance, aquí vacío) pero no registra ni exporta
        Assert.Equal(HttpStatusCode.OK, (await responsable.GetAsync($"/api/informes/horas?{Periodo()}")).StatusCode);
        Assert.Empty((await responsable.GetFromJsonAsync<JsonElement>($"/api/informes/horas?{Periodo()}", AyudaSesion.Json)).GetProperty("filas").EnumerateArray());
        Assert.Equal(HttpStatusCode.Forbidden, (await responsable.GetAsync($"/api/informes/horas.csv?{Periodo()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await responsable.PutAsJsonAsync($"/api/asignaciones/{noche}/horas", new { inicioUtc = "2026-10-05T20:00:00Z", finUtc = "2026-10-06T06:00:00Z" })).StatusCode);
        // El coordinador sí
        Assert.Equal(HttpStatusCode.OK, (await coordinador.PutAsJsonAsync($"/api/asignaciones/{noche}/horas", new { inicioUtc = "2026-10-05T20:00:00Z", finUtc = "2026-10-06T06:00:00Z" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await fabrica.CreateClient().GetAsync("/api/dashboard")).StatusCode);
    }

    [Fact]
    public async Task Los_datos_de_otra_sociedad_no_aparecen_en_informes_ni_se_pueden_tocar()
    {
        var (_, _, _, noche, _) = await CasoAsync();
        var otra = await Escenario.CrearAsync(fabrica);
        Assert.Empty((await otra.C.GetFromJsonAsync<JsonElement>($"/api/informes/horas?{Periodo()}", AyudaSesion.Json)).GetProperty("filas").EnumerateArray());
        Assert.Empty((await otra.C.GetFromJsonAsync<JsonElement>($"/api/informes/cobertura?{Periodo()}", AyudaSesion.Json)).GetProperty("filas").EnumerateArray());
        Assert.Empty(await otra.C.GetFromJsonAsync<List<JsonElement>>($"/api/horas?{Periodo()}", AyudaSesion.Json) ?? []);
        Assert.Equal(HttpStatusCode.NotFound, (await otra.C.PutAsJsonAsync($"/api/asignaciones/{noche}/horas", new { inicioUtc = "2026-10-05T20:00:00Z", finUtc = "2026-10-06T06:00:00Z" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otra.C.PostAsJsonAsync($"/api/asignaciones/{noche}/horas/ajustes", new { minutos = 5, motivo = "Motivo suficiente aquí" })).StatusCode);
    }

    // ── Dashboard ──

    [Fact]
    public async Task El_dashboard_muestra_cobertura_y_pendientes_a_la_coordinacion_y_proximos_turnos_al_profesional()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var manana = DateTime.UtcNow.Date.AddDays(1);
        var fin = manana.AddDays(6);
        await e.CuadranteAsync($"{manana:yyyy-MM-dd}", $"{fin:yyyy-MM-dd}");
        var (prof, cliente, _, _) = await e.ProfesionalConUsuarioAsync();
        await e.AsignarOkAsync(await e.NecesidadAsync($"{manana:yyyy-MM-dd}", e.Manana), prof);

        var admin = await e.C.GetFromJsonAsync<JsonElement>("/api/dashboard", AyudaSesion.Json);
        var gestion = admin.GetProperty("gestion");
        Assert.Equal(21, gestion.GetProperty("plazasTotales").GetInt32());
        Assert.Equal(1, gestion.GetProperty("plazasCubiertas").GetInt32());
        Assert.Equal(1, gestion.GetProperty("cuadrantesBorrador").GetInt32());
        Assert.NotEmpty(gestion.GetProperty("proximosSinCubrir").EnumerateArray());
        Assert.True(gestion.GetProperty("proximosSinCubrir").GetArrayLength() <= 8);
        Assert.Equal(JsonValueKind.Object, gestion.GetProperty("pendientes").ValueKind);
        Assert.Equal(JsonValueKind.Null, admin.GetProperty("profesional").ValueKind);

        var propio = await cliente.GetFromJsonAsync<JsonElement>("/api/dashboard", AyudaSesion.Json);
        Assert.Equal(JsonValueKind.Null, propio.GetProperty("gestion").ValueKind);
        var p = propio.GetProperty("profesional");
        Assert.Single(p.GetProperty("proximosTurnos").EnumerateArray());
        Assert.True(p.GetProperty("horasPlanificadasMesMin").GetInt32() >= 0);

        // El responsable de centro ve cobertura pero no los pendientes de gestión
        var (_, responsable) = await e.UsuarioAsync("ResponsableCentro");
        var r = await responsable.GetFromJsonAsync<JsonElement>("/api/dashboard", AyudaSesion.Json);
        Assert.Equal(JsonValueKind.Null, r.GetProperty("gestion").GetProperty("pendientes").ValueKind);
    }

    [Fact]
    public async Task El_dashboard_cuenta_los_pendientes_reales_de_la_sociedad()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-08");
        var (prof, cliente, _, _) = await e.ProfesionalConUsuarioAsync();
        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-03", e.Manana), prof);
        await cliente.PostAsJsonAsync("/api/mis-ausencias", new { tipo = "Vacaciones", desde = "2026-11-03", hasta = "2026-11-03" });
        await cliente.PostAsJsonAsync("/api/mi-disponibilidad", new { tipo = "Bloqueo", desde = "2026-11-09", hasta = "2026-11-10" });

        var pendientes = (await e.C.GetFromJsonAsync<JsonElement>("/api/dashboard", AyudaSesion.Json)).GetProperty("gestion").GetProperty("pendientes");
        Assert.Equal(1, pendientes.GetProperty("ausenciasSolicitadas").GetInt32());
        Assert.Equal(1, pendientes.GetProperty("disponibilidadesPendientes").GetInt32());
        Assert.Equal(0, pendientes.GetProperty("vacantesAbiertas").GetInt32());

        var ausencia = (await e.C.GetFromJsonAsync<List<JsonElement>>("/api/ausencias?estado=Solicitada", AyudaSesion.Json))!.Single().GetProperty("id").GetGuid();
        await e.C.PostAsync($"/api/ausencias/{ausencia}/aprobar", null);
        var tras = (await e.C.GetFromJsonAsync<JsonElement>("/api/dashboard", AyudaSesion.Json)).GetProperty("gestion").GetProperty("pendientes");
        Assert.Equal(0, tras.GetProperty("ausenciasSolicitadas").GetInt32());
        Assert.Equal(1, tras.GetProperty("vacantesAbiertas").GetInt32());
    }
}
