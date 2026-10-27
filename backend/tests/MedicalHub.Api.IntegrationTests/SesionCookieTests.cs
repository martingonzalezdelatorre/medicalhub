using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MedicalHub.Api.IntegrationTests;

/// <summary>Refresco de sesión de la web en cookie HttpOnly (el token no llega nunca al JavaScript).</summary>
[Collection(ColeccionApi.Nombre)]
public class SesionCookieTests(FabricaApi fabrica)
{
    private const string Cabecera = "X-Sesion-Cookie";

    private HttpClient Cliente(bool https = false) => fabrica.CreateClient(new WebApplicationFactoryClientOptions
    {
        HandleCookies = false,                                   // las cookies se gestionan a mano para comprobar cada regla
        BaseAddress = new Uri(https ? "https://localhost" : "http://localhost")
    });

    private async Task<(string email, string pwd)> UsuarioAsync()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var pwd = AyudaSesion.ContrasenaValida("ck");
        var u = await Ayudas.CrearUsuarioAsync(fabrica, e.Admin, "ProfesionalSanitario", contrasena: pwd);
        return (u.Email, pwd);
    }

    private static HttpRequestMessage Peticion(string ruta, object? cuerpo, bool modoCookie, string? cookie = null)
    {
        var r = new HttpRequestMessage(HttpMethod.Post, ruta) { Content = JsonContent.Create(cuerpo ?? new { }) };
        if (modoCookie) r.Headers.Add(Cabecera, "1");
        if (cookie is not null) r.Headers.Add("Cookie", $"mh_refresh={cookie}");
        return r;
    }

    private static string? ValorCookie(HttpResponseMessage r)
    {
        var linea = r.Headers.TryGetValues("Set-Cookie", out var v) ? v.FirstOrDefault(x => x.StartsWith("mh_refresh=")) : null;
        return linea?.Split(';')[0]["mh_refresh=".Length..];
    }

    private static string? LineaCookie(HttpResponseMessage r) => r.Headers.TryGetValues("Set-Cookie", out var v) ? v.FirstOrDefault(x => x.StartsWith("mh_refresh=")) : null;

    private async Task<(HttpResponseMessage resp, JsonElement cuerpo, string cookie)> LoginCookieAsync(HttpClient c, string email, string pwd)
    {
        var r = await c.SendAsync(Peticion("/api/autenticacion/login", new { email, contrasena = pwd }, modoCookie: true));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (r, await r.Content.ReadFromJsonAsync<JsonElement>(AyudaSesion.Json), ValorCookie(r)!);
    }

    [Fact]
    public async Task En_modo_cookie_el_login_entrega_el_refresco_solo_en_una_cookie_HttpOnly_y_no_en_el_cuerpo()
    {
        var (email, pwd) = await UsuarioAsync();
        var (resp, cuerpo, cookie) = await LoginCookieAsync(Cliente(), email, pwd);

        Assert.False(string.IsNullOrEmpty(cookie));
        Assert.Equal("", cuerpo.GetProperty("tokenRefresco").GetString());           // fuera del alcance del JavaScript
        Assert.False(string.IsNullOrEmpty(cuerpo.GetProperty("tokenAcceso").GetString()));
        var linea = LineaCookie(resp)!.ToLowerInvariant();
        Assert.Contains("httponly", linea);
        Assert.Contains("samesite=strict", linea);
        Assert.Contains("path=/api/autenticacion", linea);
        Assert.Contains("expires=", linea);
        Assert.DoesNotContain("secure", linea.Replace("samesite", ""));              // HTTP en desarrollo: sin Secure
    }

    [Fact]
    public async Task Por_HTTPS_la_cookie_lleva_Secure()
    {
        var (email, pwd) = await UsuarioAsync();
        var r = await Cliente(https: true).SendAsync(Peticion("/api/autenticacion/login", new { email, contrasena = pwd }, modoCookie: true));
        Assert.Contains("secure", LineaCookie(r)!.ToLowerInvariant().Replace("samesite", ""));
    }

    [Fact]
    public async Task Sin_la_cabecera_de_modo_cookie_el_comportamiento_para_moviles_no_cambia()
    {
        var (email, pwd) = await UsuarioAsync();
        var c = Cliente();
        var r = await c.SendAsync(Peticion("/api/autenticacion/login", new { email, contrasena = pwd }, modoCookie: false));
        var cuerpo = await r.Content.ReadFromJsonAsync<JsonElement>(AyudaSesion.Json);
        Assert.Null(LineaCookie(r));
        var refresco = cuerpo.GetProperty("tokenRefresco").GetString();
        Assert.False(string.IsNullOrEmpty(refresco));

        var renovado = await c.SendAsync(Peticion("/api/autenticacion/refrescar", new { tokenRefresco = refresco }, modoCookie: false));
        Assert.Equal(HttpStatusCode.OK, renovado.StatusCode);
        Assert.Null(LineaCookie(renovado));
    }

    [Fact]
    public async Task El_refresco_por_cookie_rota_el_token_y_detecta_la_reutilizacion()
    {
        var (email, pwd) = await UsuarioAsync();
        var c = Cliente();
        var (_, _, cookie1) = await LoginCookieAsync(c, email, pwd);

        var r2 = await c.SendAsync(Peticion("/api/autenticacion/refrescar", null, modoCookie: true, cookie1));
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
        var cookie2 = ValorCookie(r2)!;
        Assert.NotEqual(cookie1, cookie2);
        Assert.Equal("", (await r2.Content.ReadFromJsonAsync<JsonElement>(AyudaSesion.Json)).GetProperty("tokenRefresco").GetString());

        // Reutilizar la cookie ya rotada: se rechaza, se borra la cookie y se invalida toda la familia
        var reuso = await c.SendAsync(Peticion("/api/autenticacion/refrescar", null, modoCookie: true, cookie1));
        Assert.Equal(HttpStatusCode.Unauthorized, reuso.StatusCode);
        Assert.Contains("expires=thu, 01 jan 1970", LineaCookie(reuso)!.ToLowerInvariant());
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.SendAsync(Peticion("/api/autenticacion/refrescar", null, modoCookie: true, cookie2))).StatusCode);
    }

    [Fact]
    public async Task Una_peticion_sin_la_cabecera_personalizada_no_puede_usar_la_cookie_defensa_CSRF()
    {
        var (email, pwd) = await UsuarioAsync();
        var c = Cliente();
        var (_, _, cookie) = await LoginCookieAsync(c, email, pwd);

        // Lo que haría un formulario o una imagen de otro sitio: manda la cookie pero no puede añadir la cabecera
        var csrf = await c.SendAsync(Peticion("/api/autenticacion/refrescar", null, modoCookie: false, cookie));
        Assert.Equal(HttpStatusCode.Unauthorized, csrf.StatusCode);
        // y la cookie sigue siendo válida para quien sí lo hace bien
        Assert.Equal(HttpStatusCode.OK, (await c.SendAsync(Peticion("/api/autenticacion/refrescar", null, modoCookie: true, cookie))).StatusCode);
    }

    [Fact]
    public async Task Sin_cookie_o_con_una_cookie_inventada_el_refresco_responde_401()
    {
        var c = Cliente();
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.SendAsync(Peticion("/api/autenticacion/refrescar", null, modoCookie: true))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.SendAsync(Peticion("/api/autenticacion/refrescar", null, modoCookie: true, "inventada"))).StatusCode);
    }

    [Fact]
    public async Task Cerrar_sesion_en_modo_cookie_borra_la_cookie_y_revoca_el_token()
    {
        var (email, pwd) = await UsuarioAsync();
        var c = Cliente();
        var (_, cuerpo, cookie) = await LoginCookieAsync(c, email, pwd);

        var cierre = Peticion("/api/autenticacion/cerrar-sesion", null, modoCookie: true, cookie);
        cierre.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cuerpo.GetProperty("tokenAcceso").GetString());
        var r = await c.SendAsync(cierre);
        Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        Assert.Contains("expires=thu, 01 jan 1970", LineaCookie(r)!.ToLowerInvariant());
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.SendAsync(Peticion("/api/autenticacion/refrescar", null, modoCookie: true, cookie))).StatusCode);
    }

    private static string? Valor(HttpResponseMessage r, string cabecera)
        => r.Headers.Concat(r.Content.Headers).Where(h => h.Key.Equals(cabecera, StringComparison.OrdinalIgnoreCase)).SelectMany(h => h.Value).FirstOrDefault();

    [Fact]
    public async Task CORS_permite_credenciales_solo_a_los_origenes_configurados()
    {
        var c = Cliente();
        HttpRequestMessage Previo(string origen)
        {
            var r = new HttpRequestMessage(HttpMethod.Options, "/api/autenticacion/refrescar");
            r.Headers.Add("Origin", origen);
            r.Headers.Add("Access-Control-Request-Method", "POST");
            r.Headers.Add("Access-Control-Request-Headers", $"content-type,{Cabecera.ToLowerInvariant()}");
            return r;
        }
        var permitido = await c.SendAsync(Previo("http://localhost:8080"));
        Assert.Equal("true", Valor(permitido, "Access-Control-Allow-Credentials"));
        Assert.Equal("http://localhost:8080", Valor(permitido, "Access-Control-Allow-Origin"));

        var ajeno = await c.SendAsync(Previo("https://sitio-malicioso.example"));
        Assert.Null(Valor(ajeno, "Access-Control-Allow-Origin"));
        Assert.Null(Valor(ajeno, "Access-Control-Allow-Credentials"));
    }
}
