using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MedicalHub.Api.IntegrationTests;

[Collection(ColeccionApi.Nombre)]
public class EstructuraTests(FabricaApi fabrica)
{
    private async Task<(AyudaSesion admin, HttpClient c)> NuevaSociedadAsync()
    {
        var (_, _, _, admin) = await Ayudas.CrearSociedadAsync(fabrica);
        return (admin, admin.Cliente(fabrica));
    }

    // ── Catálogos ──

    [Theory]
    [InlineData("especialidades")]
    [InlineData("categorias-profesionales")]
    [InlineData("competencias")]
    public async Task Los_catalogos_se_crean_listan_y_desactivan(string ruta)
    {
        var (_, c) = await NuevaSociedadAsync();
        var nombre = Planif.Unico("Item");
        var id = await Planif.CrearCatalogoAsync(c, ruta, nombre);

        // duplicado → 409
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/{ruta}", new { nombre })).StatusCode);

        Assert.Contains(await c.GetFromJsonAsync<List<JsonElement>>($"/api/{ruta}", AyudaSesion.Json) ?? [],
            e => e.GetProperty("id").GetGuid() == id);

        await c.PostAsync($"/api/{ruta}/{id}/desactivar", null);
        Assert.DoesNotContain(await c.GetFromJsonAsync<List<JsonElement>>($"/api/{ruta}", AyudaSesion.Json) ?? [],
            e => e.GetProperty("id").GetGuid() == id);
        Assert.Contains(await c.GetFromJsonAsync<List<JsonElement>>($"/api/{ruta}?incluirInactivas=true", AyudaSesion.Json) ?? [],
            e => e.GetProperty("id").GetGuid() == id);
    }

    [Fact]
    public async Task El_mismo_nombre_de_catalogo_puede_existir_en_sociedades_distintas()
    {
        var (_, a) = await NuevaSociedadAsync();
        var (_, b) = await NuevaSociedadAsync();
        var nombre = Planif.Unico("Anestesia");
        await Planif.CrearCatalogoAsync(a, "especialidades", nombre);
        await Planif.CrearCatalogoAsync(b, "especialidades", nombre);
    }

    [Fact]
    public async Task Un_profesional_puede_leer_los_catalogos_pero_no_modificarlos()
    {
        var (admin, c) = await NuevaSociedadAsync();
        var contrasena = AyudaSesion.ContrasenaValida("p1");
        var u = await Ayudas.CrearUsuarioAsync(fabrica, admin, "ProfesionalSanitario", contrasena: contrasena);
        var prof = (await AyudaSesion.LoginAsync(fabrica.CreateClient(), u.Email, contrasena)).Cliente(fabrica);

        Assert.Equal(HttpStatusCode.OK, (await prof.GetAsync("/api/especialidades")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await prof.PostAsJsonAsync("/api/especialidades", new { nombre = "X" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await prof.GetAsync("/api/centros")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await prof.GetAsync("/api/profesionales")).StatusCode);
    }

    [Fact]
    public async Task Los_catalogos_de_otra_sociedad_son_invisibles_y_no_editables()
    {
        var (_, a) = await NuevaSociedadAsync();
        var (_, b) = await NuevaSociedadAsync();
        var id = await Planif.CrearCatalogoAsync(a, "competencias");

        var lista = await b.GetFromJsonAsync<List<JsonElement>>("/api/competencias", AyudaSesion.Json);
        Assert.DoesNotContain(lista!, e => e.GetProperty("id").GetGuid() == id);
        Assert.Equal(HttpStatusCode.NotFound, (await b.PutAsJsonAsync($"/api/competencias/{id}", new { nombre = "Hack" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsync($"/api/competencias/{id}/desactivar", null)).StatusCode);
    }

    // ── Centros ──

    [Fact]
    public async Task Crear_un_centro_valida_la_zona_horaria_y_el_nombre_duplicado()
    {
        var (_, c) = await NuevaSociedadAsync();
        var r = await c.PostAsJsonAsync("/api/centros", new { nombre = "H1", tipo = "Hospital", zonaHoraria = "Marte/Olimpo" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);

        var ok = await c.PostAsJsonAsync("/api/centros", new { nombre = "Hospital Único", tipo = "Hospital", zonaHoraria = "Atlantic/Canary" });
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await c.PostAsJsonAsync("/api/centros", new { nombre = "Hospital Único", tipo = "Hospital", zonaHoraria = "Europe/Madrid" })).StatusCode);
    }

    [Fact]
    public async Task Un_centro_de_otra_sociedad_responde_404()
    {
        var (_, a) = await NuevaSociedadAsync();
        var (_, b) = await NuevaSociedadAsync();
        var centro = await Planif.CrearCentroAsync(a);

        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/centros/{centro}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.PutAsJsonAsync($"/api/centros/{centro}",
            new { nombre = "Hack", tipo = "Hospital", zonaHoraria = "Europe/Madrid" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsync($"/api/centros/{centro}/desactivar", null)).StatusCode);

        var lista = await b.GetFromJsonAsync<List<JsonElement>>("/api/centros", AyudaSesion.Json);
        Assert.DoesNotContain(lista!, e => e.GetProperty("id").GetGuid() == centro);
    }

    [Fact]
    public async Task El_responsable_de_centro_solo_ve_sus_centros_servicios_y_profesionales_sin_contacto()
    {
        var (admin, c) = await NuevaSociedadAsync();
        var cat = await Planif.CrearCatalogoAsync(c, "categorias-profesionales");
        var centroA = await Planif.CrearCentroAsync(c);
        var centroB = await Planif.CrearCentroAsync(c);
        var servA = await Planif.CrearServicioAsync(c, centroA);
        var servB = await Planif.CrearServicioAsync(c, centroB);
        var profA = await Planif.CrearProfesionalAsync(c, Planif.Profesional(cat, servicios: [servA]));
        var profB = await Planif.CrearProfesionalAsync(c, Planif.Profesional(cat, servicios: [servB]));

        var contrasena = AyudaSesion.ContrasenaValida("resp");
        var resp = await Ayudas.CrearUsuarioAsync(fabrica, admin, "ResponsableCentro", contrasena: contrasena);
        var asignar = await c.PutAsJsonAsync($"/api/centros/{centroA}/responsables", new { usuariosIds = new[] { resp.Id } });
        Assert.Equal(HttpStatusCode.OK, asignar.StatusCode);

        var r = (await AyudaSesion.LoginAsync(fabrica.CreateClient(), resp.Email, contrasena)).Cliente(fabrica);

        var centros = await r.GetFromJsonAsync<List<JsonElement>>("/api/centros", AyudaSesion.Json);
        Assert.Equal([centroA], centros!.Select(e => e.GetProperty("id").GetGuid()));
        Assert.Equal(HttpStatusCode.NotFound, (await r.GetAsync($"/api/centros/{centroB}")).StatusCode);

        var servicios = await r.GetFromJsonAsync<List<JsonElement>>("/api/servicios", AyudaSesion.Json);
        Assert.Equal([servA], servicios!.Select(e => e.GetProperty("id").GetGuid()));
        Assert.Equal(HttpStatusCode.NotFound, (await r.GetAsync($"/api/servicios/{servB}")).StatusCode);

        var profesionales = await r.GetFromJsonAsync<List<JsonElement>>("/api/profesionales", AyudaSesion.Json);
        Assert.Equal([profA], profesionales!.Select(e => e.GetProperty("id").GetGuid()));
        Assert.Equal(JsonValueKind.Null, profesionales![0].GetProperty("email").ValueKind); // sin datos de contacto
        Assert.Equal(HttpStatusCode.NotFound, (await r.GetAsync($"/api/profesionales/{profB}")).StatusCode);

        // ...y no puede modificar nada
        Assert.Equal(HttpStatusCode.Forbidden, (await r.PostAsJsonAsync("/api/centros",
            new { nombre = "X", tipo = "Hospital", zonaHoraria = "Europe/Madrid" })).StatusCode);
    }

    [Fact]
    public async Task Solo_usuarios_con_rol_de_responsable_pueden_ser_responsables_de_un_centro()
    {
        var (admin, c) = await NuevaSociedadAsync();
        var centro = await Planif.CrearCentroAsync(c);
        var prof = await Ayudas.CrearUsuarioAsync(fabrica, admin, "ProfesionalSanitario");

        var r = await c.PutAsJsonAsync($"/api/centros/{centro}/responsables", new { usuariosIds = new[] { prof.Id } });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);

        // Un usuario de OTRA sociedad (aunque tenga el rol) tampoco vale
        var (adminB, _) = await NuevaSociedadAsync();
        var ajeno = await Ayudas.CrearUsuarioAsync(fabrica, adminB, "ResponsableCentro");
        Assert.Equal(HttpStatusCode.BadRequest,
            (await c.PutAsJsonAsync($"/api/centros/{centro}/responsables", new { usuariosIds = new[] { ajeno.Id } })).StatusCode);
    }

    // ── Servicios ──

    [Fact]
    public async Task Un_servicio_gestiona_tipos_de_turno_nocturnos_y_requisitos_de_cobertura()
    {
        var (_, c) = await NuevaSociedadAsync();
        var cat = await Planif.CrearCatalogoAsync(c, "categorias-profesionales");
        var comp = await Planif.CrearCatalogoAsync(c, "competencias");
        var servicio = await Planif.CrearServicioAsync(c, await Planif.CrearCentroAsync(c), [cat], [comp], """{"maxHorasSemana":48}""");

        var noche = await Planif.LeerAsync<JsonElement>(await c.PostAsJsonAsync($"/api/servicios/{servicio}/tipos-turno",
            new { nombre = "Noche", horaInicio = "22:00", horaFin = "08:00", esGuardia = false }), HttpStatusCode.Created);
        Assert.True(noche.GetProperty("cruzaMedianoche").GetBoolean());
        Assert.Equal(10, noche.GetProperty("duracionHoras").GetDouble());

        var guardia = await Planif.LeerAsync<JsonElement>(await c.PostAsJsonAsync($"/api/servicios/{servicio}/tipos-turno",
            new { nombre = "Guardia 24 h", horaInicio = "08:00", horaFin = "08:00", esGuardia = true }), HttpStatusCode.Created);
        Assert.Equal(24, guardia.GetProperty("duracionHoras").GetDouble());

        // nombre duplicado dentro del servicio
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/servicios/{servicio}/tipos-turno",
            new { nombre = "Noche", horaInicio = "20:00", horaFin = "06:00", esGuardia = false })).StatusCode);

        var tipoNoche = noche.GetProperty("id").GetGuid();
        var req = await c.PutAsJsonAsync($"/api/servicios/{servicio}/requisitos", new
        {
            requisitos = new[] { new { tipoTurnoId = tipoNoche, numeroProfesionales = 2, categoriaProfesionalId = (Guid?)cat } }
        });
        Assert.Equal(HttpStatusCode.OK, req.StatusCode);

        var detalle = await c.GetFromJsonAsync<JsonElement>($"/api/servicios/{servicio}", AyudaSesion.Json);
        Assert.Equal(2, detalle.GetProperty("tiposTurno").GetArrayLength());
        Assert.Equal(2, detalle.GetProperty("requisitos")[0].GetProperty("numeroProfesionales").GetInt32());
        Assert.Contains("48", detalle.GetProperty("configuracionReglas").GetString());

        // reemplazar por una lista vacía elimina los requisitos
        await c.PutAsJsonAsync($"/api/servicios/{servicio}/requisitos", new { requisitos = Array.Empty<object>() });
        detalle = await c.GetFromJsonAsync<JsonElement>($"/api/servicios/{servicio}", AyudaSesion.Json);
        Assert.Equal(0, detalle.GetProperty("requisitos").GetArrayLength());
    }

    [Fact]
    public async Task Los_requisitos_deben_referirse_a_tipos_de_turno_del_propio_servicio()
    {
        var (_, c) = await NuevaSociedadAsync();
        var centro = await Planif.CrearCentroAsync(c);
        var s1 = await Planif.CrearServicioAsync(c, centro);
        var s2 = await Planif.CrearServicioAsync(c, centro);
        var tipoS2 = (await Planif.LeerAsync<IdDto>(await c.PostAsJsonAsync($"/api/servicios/{s2}/tipos-turno",
            new { nombre = "Mañana", horaInicio = "08:00", horaFin = "15:00", esGuardia = false }), HttpStatusCode.Created)).Id;

        var r = await c.PutAsJsonAsync($"/api/servicios/{s1}/requisitos",
            new { requisitos = new[] { new { tipoTurnoId = tipoS2, numeroProfesionales = 1, categoriaProfesionalId = (Guid?)null } } });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Un_servicio_no_puede_referenciar_datos_de_otra_sociedad()
    {
        var (_, a) = await NuevaSociedadAsync();
        var (_, b) = await NuevaSociedadAsync();
        var catA = await Planif.CrearCatalogoAsync(a, "categorias-profesionales");
        var compA = await Planif.CrearCatalogoAsync(a, "competencias");
        var centroA = await Planif.CrearCentroAsync(a);
        var centroB = await Planif.CrearCentroAsync(b);

        // categoría/competencia ajenas → 400
        var r = await b.PostAsJsonAsync("/api/servicios", new
        {
            centroId = centroB, nombre = "S", categoriasAdmitidasIds = new[] { catA }, competenciasRequeridasIds = new[] { compA }
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);

        // centro ajeno → 404
        Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsJsonAsync("/api/servicios", new { centroId = centroA, nombre = "S" })).StatusCode);

        // servicio ajeno → 404 en lectura y escritura
        var servA = await Planif.CrearServicioAsync(a, centroA);
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/servicios/{servA}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsJsonAsync($"/api/servicios/{servA}/tipos-turno",
            new { nombre = "X", horaInicio = "08:00", horaFin = "15:00", esGuardia = false })).StatusCode);
    }

    [Fact]
    public async Task La_configuracion_de_reglas_invalida_se_rechaza()
    {
        var (_, c) = await NuevaSociedadAsync();
        var centro = await Planif.CrearCentroAsync(c);
        var r = await c.PostAsJsonAsync("/api/servicios", new { centroId = centro, nombre = "S", configuracionReglas = "{no json" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
    }

    [Fact]
    public async Task El_coordinador_del_servicio_debe_ser_un_coordinador_de_la_sociedad()
    {
        var (admin, c) = await NuevaSociedadAsync();
        var centro = await Planif.CrearCentroAsync(c);
        var prof = await Ayudas.CrearUsuarioAsync(fabrica, admin, "ProfesionalSanitario");
        var coord = await Ayudas.CrearUsuarioAsync(fabrica, admin, "CoordinadorMedico");

        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/servicios",
            new { centroId = centro, nombre = "S1", coordinadorUsuarioId = prof.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync("/api/servicios",
            new { centroId = centro, nombre = "S2", coordinadorUsuarioId = coord.Id })).StatusCode);
    }
}
