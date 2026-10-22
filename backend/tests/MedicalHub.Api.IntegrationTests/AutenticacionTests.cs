using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MedicalHub.Api.IntegrationTests;

[Collection(ColeccionApi.Nombre)]
public class AutenticacionTests(FabricaApi fabrica)
{
    [Fact]
    public async Task Login_con_credenciales_incorrectas_devuelve_401_y_el_mismo_mensaje_exista_o_no_el_usuario()
    {
        var (_, emailAdmin, _, _) = await Ayudas.CrearSociedadAsync(fabrica);
        var http = fabrica.CreateClient();

        var existente = await AyudaSesion.LoginCrudoAsync(http, emailAdmin, "Incorrecta-123456!");
        var inexistente = await AyudaSesion.LoginCrudoAsync(http, "nadie@no-existe.test", "Incorrecta-123456!");

        Assert.Equal(HttpStatusCode.Unauthorized, existente.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, inexistente.StatusCode);
        Assert.Equal(await existente.Content.ReadAsStringAsync().ContinueWith(t => Detalle(t.Result)),
                     await inexistente.Content.ReadAsStringAsync().ContinueWith(t => Detalle(t.Result)));
    }

    private static string Detalle(string json) => System.Text.Json.JsonDocument.Parse(json).RootElement.GetProperty("detail").GetString()!;

    [Fact]
    public async Task Cinco_fallos_bloquean_la_cuenta_aunque_despues_se_use_la_contrasena_correcta()
    {
        var (_, email, contrasena, _) = await Ayudas.CrearSociedadAsync(fabrica);
        var http = fabrica.CreateClient();

        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await AyudaSesion.LoginCrudoAsync(http, email, "Mala-contrasena-1!")).StatusCode);

        var r = await AyudaSesion.LoginCrudoAsync(http, email, contrasena);
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Endpoint_protegido_sin_token_devuelve_401()
    {
        var r = await fabrica.CreateClient().GetAsync("/api/usuarios");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Token_manipulado_es_rechazado()
    {
        var (_, _, _, admin) = await Ayudas.CrearSociedadAsync(fabrica);
        var http = fabrica.CreateClient();
        var t = admin.Sesion.TokenAcceso;
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", t[..^3] + (t[^3] == 'A' ? "BBB" : "AAA"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/api/usuarios")).StatusCode);
    }

    [Fact]
    public async Task Refrescar_rota_el_token_y_la_reutilizacion_revoca_toda_la_sesion()
    {
        var (_, _, _, admin) = await Ayudas.CrearSociedadAsync(fabrica);
        var http = fabrica.CreateClient();

        var r1 = await http.PostAsJsonAsync("/api/autenticacion/refrescar", new { tokenRefresco = admin.Sesion.TokenRefresco });
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        var s1 = await r1.Content.ReadFromJsonAsync<SesionDto>(AyudaSesion.Json);
        Assert.NotEqual(admin.Sesion.TokenRefresco, s1!.TokenRefresco);

        // Reutilizar el token antiguo: 401 y se invalida la familia completa
        var reutilizado = await http.PostAsJsonAsync("/api/autenticacion/refrescar", new { tokenRefresco = admin.Sesion.TokenRefresco });
        Assert.Equal(HttpStatusCode.Unauthorized, reutilizado.StatusCode);

        var nuevoTrasRobo = await http.PostAsJsonAsync("/api/autenticacion/refrescar", new { tokenRefresco = s1.TokenRefresco });
        Assert.Equal(HttpStatusCode.Unauthorized, nuevoTrasRobo.StatusCode);
    }

    [Fact]
    public async Task Cerrar_sesion_invalida_el_token_de_refresco()
    {
        var (_, _, _, admin) = await Ayudas.CrearSociedadAsync(fabrica);
        var r = await admin.Cliente(fabrica).PostAsJsonAsync("/api/autenticacion/cerrar-sesion", new { tokenRefresco = admin.Sesion.TokenRefresco });
        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);

        var intento = await fabrica.CreateClient().PostAsJsonAsync("/api/autenticacion/refrescar", new { tokenRefresco = admin.Sesion.TokenRefresco });
        Assert.Equal(HttpStatusCode.Unauthorized, intento.StatusCode);
    }

    [Fact]
    public async Task Usuario_desactivado_no_puede_iniciar_sesion_ni_refrescar()
    {
        var (_, _, _, admin) = await Ayudas.CrearSociedadAsync(fabrica);
        var contrasena = AyudaSesion.ContrasenaValida("des");
        var u = await Ayudas.CrearUsuarioAsync(fabrica, admin, contrasena: contrasena);
        var sesion = await AyudaSesion.LoginAsync(fabrica.CreateClient(), u.Email, contrasena);

        var r = await admin.Cliente(fabrica).PostAsync($"/api/usuarios/{u.Id}/desactivar", null);
        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await AyudaSesion.LoginCrudoAsync(fabrica.CreateClient(), u.Email, contrasena)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await fabrica.CreateClient().PostAsJsonAsync("/api/autenticacion/refrescar", new { tokenRefresco = sesion.Sesion.TokenRefresco })).StatusCode);
    }

    [Fact]
    public async Task Cambiar_contrasena_revoca_las_sesiones_y_exige_la_nueva()
    {
        var (_, _, _, admin) = await Ayudas.CrearSociedadAsync(fabrica);
        var actual = AyudaSesion.ContrasenaValida("cam");
        var u = await Ayudas.CrearUsuarioAsync(fabrica, admin, contrasena: actual);
        var sesion = await AyudaSesion.LoginAsync(fabrica.CreateClient(), u.Email, actual);
        var nueva = AyudaSesion.ContrasenaValida("nueva");

        var r = await sesion.Cliente(fabrica).PostAsJsonAsync("/api/autenticacion/cambiar-contrasena",
            new { contrasenaActual = actual, contrasenaNueva = nueva });
        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await fabrica.CreateClient().PostAsJsonAsync("/api/autenticacion/refrescar", new { tokenRefresco = sesion.Sesion.TokenRefresco })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await AyudaSesion.LoginCrudoAsync(fabrica.CreateClient(), u.Email, actual)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AyudaSesion.LoginCrudoAsync(fabrica.CreateClient(), u.Email, nueva)).StatusCode);
    }

    [Fact]
    public async Task Contrasena_debil_se_rechaza_con_errores_por_campo()
    {
        var (_, _, _, admin) = await Ayudas.CrearSociedadAsync(fabrica);
        var r = await admin.Cliente(fabrica).PostAsJsonAsync("/api/usuarios", new
        {
            email = "debil@sociedad.test", nombreCompleto = "Débil", contrasena = "corta", roles = new[] { "ProfesionalSanitario" }
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("Contrasena", await r.Content.ReadAsStringAsync());
    }
}
