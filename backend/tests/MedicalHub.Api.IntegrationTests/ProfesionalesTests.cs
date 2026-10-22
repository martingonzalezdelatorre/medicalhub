using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MedicalHub.Api.IntegrationTests;

[Collection(ColeccionApi.Nombre)]
public class ProfesionalesTests(FabricaApi fabrica)
{
    private async Task<(AyudaSesion admin, HttpClient c)> NuevaSociedadAsync()
    {
        var (_, _, _, admin) = await Ayudas.CrearSociedadAsync(fabrica);
        return (admin, admin.Cliente(fabrica));
    }

    [Fact]
    public async Task Alta_consulta_modificacion_y_desactivacion_de_un_profesional()
    {
        var (_, c) = await NuevaSociedadAsync();
        var cat = await Planif.CrearCatalogoAsync(c, "categorias-profesionales");
        var esp = await Planif.CrearCatalogoAsync(c, "especialidades");
        var comp = await Planif.CrearCatalogoAsync(c, "competencias");
        var servicio = await Planif.CrearServicioAsync(c, await Planif.CrearCentroAsync(c));

        var id = await Planif.CrearProfesionalAsync(c, Planif.Profesional(cat, [esp], [new { competenciaId = comp, vigenteHasta = "2027-06-30" }], [servicio], "COL-1"));
        var ficha = await c.GetFromJsonAsync<JsonElement>($"/api/profesionales/{id}", AyudaSesion.Json);

        Assert.Equal(esp, ficha.GetProperty("especialidadesIds")[0].GetGuid());
        Assert.Equal("2027-06-30", ficha.GetProperty("competencias")[0].GetProperty("vigenteHasta").GetString());
        Assert.Equal(servicio, ficha.GetProperty("serviciosIds")[0].GetGuid());
        Assert.True(ficha.GetProperty("activo").GetBoolean());

        // modificar: quitar especialidad, cambiar vigencia
        var r = await c.PutAsJsonAsync($"/api/profesionales/{id}",
            Planif.Profesional(cat, [], [new { competenciaId = comp, vigenteHasta = "2028-01-01" }], [servicio], "COL-1", nombre: "Ana María"));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        ficha = await c.GetFromJsonAsync<JsonElement>($"/api/profesionales/{id}", AyudaSesion.Json);
        Assert.Equal(0, ficha.GetProperty("especialidadesIds").GetArrayLength());
        Assert.Equal("2028-01-01", ficha.GetProperty("competencias")[0].GetProperty("vigenteHasta").GetString());

        await c.PostAsync($"/api/profesionales/{id}/desactivar", null);
        var activos = await c.GetFromJsonAsync<List<JsonElement>>("/api/profesionales?activo=true", AyudaSesion.Json);
        Assert.DoesNotContain(activos!, e => e.GetProperty("id").GetGuid() == id);
        var inactivos = await c.GetFromJsonAsync<List<JsonElement>>("/api/profesionales?activo=false", AyudaSesion.Json);
        Assert.Contains(inactivos!, e => e.GetProperty("id").GetGuid() == id);
    }

    [Fact]
    public async Task El_numero_de_colegiado_es_unico_dentro_de_la_sociedad_pero_no_entre_sociedades()
    {
        var (_, a) = await NuevaSociedadAsync();
        var (_, b) = await NuevaSociedadAsync();
        var catA = await Planif.CrearCatalogoAsync(a, "categorias-profesionales");
        var catB = await Planif.CrearCatalogoAsync(b, "categorias-profesionales");

        await Planif.CrearProfesionalAsync(a, Planif.Profesional(catA, colegiado: "28/1234"));
        Assert.Equal(HttpStatusCode.Conflict, (await a.PostAsJsonAsync("/api/profesionales", Planif.Profesional(catA, colegiado: "28/1234"))).StatusCode);
        await Planif.CrearProfesionalAsync(b, Planif.Profesional(catB, colegiado: "28/1234"));
    }

    [Fact]
    public async Task Un_profesional_no_puede_usar_catalogos_ni_servicios_de_otra_sociedad()
    {
        var (_, a) = await NuevaSociedadAsync();
        var (_, b) = await NuevaSociedadAsync();
        var catA = await Planif.CrearCatalogoAsync(a, "categorias-profesionales");
        var espA = await Planif.CrearCatalogoAsync(a, "especialidades");
        var servA = await Planif.CrearServicioAsync(a, await Planif.CrearCentroAsync(a));
        var catB = await Planif.CrearCatalogoAsync(b, "categorias-profesionales");

        Assert.Equal(HttpStatusCode.BadRequest, (await b.PostAsJsonAsync("/api/profesionales", Planif.Profesional(catA))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await b.PostAsJsonAsync("/api/profesionales", Planif.Profesional(catB, especialidades: [espA]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await b.PostAsJsonAsync("/api/profesionales", Planif.Profesional(catB, servicios: [servA]))).StatusCode);
    }

    [Fact]
    public async Task Las_fichas_de_otra_sociedad_responden_404_y_no_aparecen_en_listados()
    {
        var (_, a) = await NuevaSociedadAsync();
        var (_, b) = await NuevaSociedadAsync();
        var catA = await Planif.CrearCatalogoAsync(a, "categorias-profesionales");
        var id = await Planif.CrearProfesionalAsync(a, Planif.Profesional(catA));

        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/profesionales/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/profesionales/{id}/historial")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsync($"/api/profesionales/{id}/desactivar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.PutAsJsonAsync($"/api/profesionales/{id}", Planif.Profesional(catA))).StatusCode);
        Assert.Empty(await b.GetFromJsonAsync<List<JsonElement>>("/api/profesionales", AyudaSesion.Json) ?? []);
    }

    [Fact]
    public async Task Vincular_un_usuario_exige_rol_profesional_y_ficha_unica_y_permite_ver_la_propia_ficha()
    {
        var (admin, c) = await NuevaSociedadAsync();
        var cat = await Planif.CrearCatalogoAsync(c, "categorias-profesionales");
        var contrasena = AyudaSesion.ContrasenaValida("vinc");
        var usuario = await Ayudas.CrearUsuarioAsync(fabrica, admin, "ProfesionalSanitario", contrasena: contrasena);
        var coordinador = await Ayudas.CrearUsuarioAsync(fabrica, admin, "CoordinadorMedico");

        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/profesionales", Planif.Profesional(cat, usuarioId: coordinador.Id))).StatusCode);

        var id = await Planif.CrearProfesionalAsync(c, Planif.Profesional(cat, usuarioId: usuario.Id));
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/profesionales", Planif.Profesional(cat, usuarioId: usuario.Id))).StatusCode);

        var propio = (await AyudaSesion.LoginAsync(fabrica.CreateClient(), usuario.Email, contrasena)).Cliente(fabrica);
        var ficha = await propio.GetFromJsonAsync<JsonElement>("/api/profesionales/mi-perfil", AyudaSesion.Json);
        Assert.Equal(id, ficha.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task El_historial_registra_los_cambios_relevantes_sin_datos_ajenos()
    {
        var (admin, c) = await NuevaSociedadAsync();
        var cat = await Planif.CrearCatalogoAsync(c, "categorias-profesionales");
        var esp = await Planif.CrearCatalogoAsync(c, "especialidades");
        var id = await Planif.CrearProfesionalAsync(c, Planif.Profesional(cat));

        await c.PutAsJsonAsync($"/api/profesionales/{id}", Planif.Profesional(cat, [esp], nombre: "Cambiado"));

        var historial = await c.GetFromJsonAsync<List<JsonElement>>($"/api/profesionales/{id}/historial", AyudaSesion.Json);
        var entidades = historial!.Select(h => h.GetProperty("entidad").GetString()).ToList();
        Assert.Contains("Profesional", entidades);
        Assert.Contains("ProfesionalEspecialidad", entidades);
        Assert.Contains(historial!, h => h.GetProperty("accion").GetString() == "Alta");
        Assert.All(historial!, h => Assert.Equal("Admin Sociedad", h.GetProperty("usuario").GetString()));

        // Solo el administrador de la sociedad ve el historial
        var coordPwd = AyudaSesion.ContrasenaValida("coord");
        var coord = await Ayudas.CrearUsuarioAsync(fabrica, admin, "CoordinadorMedico", contrasena: coordPwd);
        var rc = (await AyudaSesion.LoginAsync(fabrica.CreateClient(), coord.Email, coordPwd)).Cliente(fabrica);
        Assert.Equal(HttpStatusCode.Forbidden, (await rc.GetAsync($"/api/profesionales/{id}/historial")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await rc.GetAsync($"/api/profesionales/{id}")).StatusCode);
    }

    [Fact]
    public async Task Los_candidatos_elegibles_dependen_de_autorizacion_categoria_y_competencias_vigentes()
    {
        var (admin, c) = await NuevaSociedadAsync();
        var catSenior = await Planif.CrearCatalogoAsync(c, "categorias-profesionales");
        var catResidente = await Planif.CrearCatalogoAsync(c, "categorias-profesionales");
        var pediatria = await Planif.CrearCatalogoAsync(c, "competencias", "Anestesia pediátrica");
        var servicio = await Planif.CrearServicioAsync(c, await Planif.CrearCentroAsync(c), [catSenior], [pediatria]);

        var valido = await Planif.CrearProfesionalAsync(c, Planif.Profesional(catSenior, competencias: [new { competenciaId = pediatria, vigenteHasta = (string?)null }], servicios: [servicio]));
        var sinAutorizar = await Planif.CrearProfesionalAsync(c, Planif.Profesional(catSenior, competencias: [new { competenciaId = pediatria, vigenteHasta = (string?)null }]));
        var categoriaIncorrecta = await Planif.CrearProfesionalAsync(c, Planif.Profesional(catResidente, competencias: [new { competenciaId = pediatria, vigenteHasta = (string?)null }], servicios: [servicio]));
        var caducada = await Planif.CrearProfesionalAsync(c, Planif.Profesional(catSenior, competencias: [new { competenciaId = pediatria, vigenteHasta = "2020-01-01" }], servicios: [servicio]));

        var elegibles = await c.GetFromJsonAsync<List<JsonElement>>($"/api/servicios/{servicio}/profesionales-elegibles?fecha=2026-10-22", AyudaSesion.Json);
        Assert.Equal([valido], elegibles!.Select(e => e.GetProperty("profesionalId").GetGuid()));

        var todos = await c.GetFromJsonAsync<List<JsonElement>>($"/api/servicios/{servicio}/profesionales-elegibles?fecha=2026-10-22&incluirNoElegibles=true", AyudaSesion.Json);
        string Motivos(Guid id) => string.Join("|", todos!.Single(e => e.GetProperty("profesionalId").GetGuid() == id).GetProperty("motivos").EnumerateArray().Select(m => m.GetString()));
        Assert.Contains("autorizado", Motivos(sinAutorizar));
        Assert.Contains("categoría", Motivos(categoriaIncorrecta));
        Assert.Contains("«Anestesia pediátrica» está caducada", Motivos(caducada));

        // Un profesional sanitario no puede consultar candidatos
        var pwd = AyudaSesion.ContrasenaValida("cand");
        var u = await Ayudas.CrearUsuarioAsync(fabrica, admin, "ProfesionalSanitario", contrasena: pwd);
        var prof = (await AyudaSesion.LoginAsync(fabrica.CreateClient(), u.Email, pwd)).Cliente(fabrica);
        Assert.Equal(HttpStatusCode.Forbidden, (await prof.GetAsync($"/api/servicios/{servicio}/profesionales-elegibles")).StatusCode);
    }
}
