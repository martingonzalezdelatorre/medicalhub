using System.Net;
using System.Net.Http.Json;

namespace MedicalHub.Api.IntegrationTests;

[Collection(ColeccionApi.Nombre)]
public class MfaTests(FabricaApi fabrica)
{
    [Fact]
    public async Task Administrador_de_plataforma_sin_mfa_solo_puede_configurar_el_mfa()
    {
        // Sesión recién iniciada del administrador de plataforma: MFA pendiente (cuenta aún sin MFA en una BD limpia o ya activada).
        var plataforma = await fabrica.SesionPlataformaAsync();
        Assert.False(plataforma.Sesion.MfaPendiente);
        Assert.True(plataforma.Sesion.Perfil.MfaActivado);
    }

    [Fact]
    public async Task Sesion_con_mfa_pendiente_no_accede_a_funciones_de_negocio()
    {
        var (_, _, _, admin) = await Ayudas.CrearSociedadAsync(fabrica, mfaObligatorio: true);
        Assert.True(admin.Sesion.MfaPendiente);

        var cliente = admin.Cliente(fabrica);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/autenticacion/perfil")).StatusCode);
    }

    [Fact]
    public async Task Tras_activar_el_mfa_la_sesion_queda_completa_y_el_login_exige_codigo()
    {
        var (_, email, contrasena, admin) = await Ayudas.CrearSociedadAsync(fabrica, mfaObligatorio: true);
        var completa = await admin.ActivarMfaAsync(fabrica.CreateClient());

        Assert.False(completa.Sesion.MfaPendiente);
        Assert.Equal(HttpStatusCode.OK, (await completa.Cliente(fabrica).GetAsync("/api/usuarios")).StatusCode);

        // Login sin código: 401 con codigo = mfa_requerido
        var sinCodigo = await AyudaSesion.LoginCrudoAsync(fabrica.CreateClient(), email, contrasena);
        Assert.Equal(HttpStatusCode.Unauthorized, sinCodigo.StatusCode);
        Assert.Equal("mfa_requerido", await AyudaSesion.CodigoErrorAsync(sinCodigo));

        // Código incorrecto
        var mal = await AyudaSesion.LoginCrudoAsync(fabrica.CreateClient(), email, contrasena, "000000");
        Assert.Equal(HttpStatusCode.Unauthorized, mal.StatusCode);

        // Código correcto
        var codigo = completa.SiguienteCodigo();
        var bien = await AyudaSesion.LoginCrudoAsync(fabrica.CreateClient(), email, contrasena, codigo);
        Assert.Equal(HttpStatusCode.OK, bien.StatusCode);

        // El mismo código no puede reutilizarse
        var repetido = await AyudaSesion.LoginCrudoAsync(fabrica.CreateClient(), email, contrasena, codigo);
        Assert.Equal(HttpStatusCode.Unauthorized, repetido.StatusCode);
    }

    [Fact]
    public async Task Activar_mfa_con_codigo_incorrecto_falla_y_no_activa()
    {
        var (_, _, _, admin) = await Ayudas.CrearSociedadAsync(fabrica, mfaObligatorio: true);
        var http = admin.Cliente(fabrica);
        await http.PostAsync("/api/autenticacion/mfa/configurar", null);

        var r = await http.PostAsJsonAsync("/api/autenticacion/mfa/activar", new { codigo = "123456" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);

        var perfil = await http.GetFromJsonAsync<PerfilDto>("/api/autenticacion/perfil", AyudaSesion.Json);
        Assert.False(perfil!.MfaActivado);
    }

    [Fact]
    public async Task El_administrador_puede_reiniciar_el_mfa_de_un_usuario()
    {
        var (_, _, _, admin) = await Ayudas.CrearSociedadAsync(fabrica);
        var contrasena = AyudaSesion.ContrasenaValida("rst");
        var u = await Ayudas.CrearUsuarioAsync(fabrica, admin, contrasena: contrasena);

        var s = await AyudaSesion.LoginAsync(fabrica.CreateClient(), u.Email, contrasena);
        await s.ActivarMfaAsync(fabrica.CreateClient());
        Assert.Equal(HttpStatusCode.Unauthorized, (await AyudaSesion.LoginCrudoAsync(fabrica.CreateClient(), u.Email, contrasena)).StatusCode);

        var r = await admin.Cliente(fabrica).PostAsync($"/api/usuarios/{u.Id}/reiniciar-mfa", null);
        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AyudaSesion.LoginCrudoAsync(fabrica.CreateClient(), u.Email, contrasena)).StatusCode);
    }
}
