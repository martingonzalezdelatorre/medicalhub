using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MedicalHub.Infrastructure.Seguridad;

namespace MedicalHub.Api.IntegrationTests;

public sealed record PerfilDto(Guid Id, Guid? OrganizacionId, string Email, string[] Roles, bool MfaActivado, bool MfaPendiente);
public sealed record SesionDto(string TokenAcceso, string TokenRefresco, bool MfaPendiente, PerfilDto Perfil);
public sealed record UsuarioDto(Guid Id, string Email, string NombreCompleto, string[] Roles, bool Activo, bool MfaActivado);
public sealed record ConfiguracionMfaDto(string Secreto, string UriOtpAuth);

/// <summary>Sesión de pruebas: cliente HTTP autenticado y utilidades de MFA (simula la app autenticadora).</summary>
public sealed class AyudaSesion
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public SesionDto Sesion { get; private set; }
    public string? SecretoMfa { get; private set; }
    private long _ultimoPaso;

    private AyudaSesion(SesionDto sesion) => Sesion = sesion;

    public Guid UsuarioId => Sesion.Perfil.Id;

    public static async Task<AyudaSesion> LoginAsync(HttpClient http, string email, string contrasena, string? codigoMfa = null)
    {
        var r = await http.PostAsJsonAsync("/api/autenticacion/login", new { email, contrasena, codigoMfa });
        r.EnsureSuccessStatusCode();
        return new AyudaSesion((await r.Content.ReadFromJsonAsync<SesionDto>(Json))!);
    }

    public static async Task<HttpResponseMessage> LoginCrudoAsync(HttpClient http, string email, string contrasena, string? codigoMfa = null)
        => await http.PostAsJsonAsync("/api/autenticacion/login", new { email, contrasena, codigoMfa });

    /// <summary>Cliente HTTP con el token de acceso actual.</summary>
    public HttpClient Cliente(FabricaApi fabrica)
    {
        var c = fabrica.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Sesion.TokenAcceso);
        return c;
    }

    /// <summary>Configura y confirma el MFA; devuelve la sesión completa.</summary>
    public async Task<AyudaSesion> ActivarMfaAsync(HttpClient http)
    {
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Sesion.TokenAcceso);
        var cfg = await (await http.PostAsync("/api/autenticacion/mfa/configurar", null))
            .Content.ReadFromJsonAsync<ConfiguracionMfaDto>(Json);
        SecretoMfa = cfg!.Secreto;

        var r = await http.PostAsJsonAsync("/api/autenticacion/mfa/activar", new { codigo = SiguienteCodigo() });
        r.EnsureSuccessStatusCode();
        Sesion = (await r.Content.ReadFromJsonAsync<SesionDto>(Json))!;
        return this;
    }

    /// <summary>Código TOTP válido y no usado antes (avanza un paso por cada llamada, dentro de la ventana de ±1).</summary>
    public string SiguienteCodigo()
    {
        var ahora = ServicioTotp.PasoDe(DateTimeOffset.UtcNow);
        _ultimoPaso = Math.Max(_ultimoPaso + 1, ahora);
        return ServicioTotp.Generar(SecretoMfa!, _ultimoPaso);
    }

    public async Task<AyudaSesion> RelanzarLoginAsync(HttpClient http, string email, string contrasena)
    {
        var s = await LoginAsync(http, email, contrasena, SiguienteCodigo());
        s.SecretoMfa = SecretoMfa;
        s._ultimoPaso = _ultimoPaso;
        return s;
    }

    public static async Task<string?> CodigoErrorAsync(HttpResponseMessage r)
    {
        var doc = await r.Content.ReadFromJsonAsync<JsonElement>(Json);
        return doc.TryGetProperty("codigo", out var c) ? c.GetString() : null;
    }

    public static string ContrasenaValida(string semilla = "x") => $"Segura-{semilla}-Contrasena-2026!";
}

public static class Ayudas
{
    /// <summary>Crea una sociedad (sin MFA obligatorio por defecto) y devuelve la sesión de su administrador.</summary>
    public static async Task<(Guid OrganizacionId, string EmailAdmin, string Contrasena, AyudaSesion Admin)> CrearSociedadAsync(
        FabricaApi fabrica, bool mfaObligatorio = false)
    {
        var plataforma = await fabrica.SesionPlataformaAsync();
        var cliente = plataforma.Cliente(fabrica);
        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var email = $"admin-{sufijo}@sociedad.test";
        var contrasena = AyudaSesion.ContrasenaValida(sufijo);

        var r = await cliente.PostAsJsonAsync("/api/organizaciones", new
        {
            nombre = $"Sociedad {sufijo}",
            zonaHoraria = "Europe/Madrid",
            mfaObligatorio,
            emailAdministrador = email,
            nombreAdministrador = "Admin Sociedad",
            contrasenaAdministrador = contrasena
        });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var org = await r.Content.ReadFromJsonAsync<JsonElement>(AyudaSesion.Json);

        var admin = await AyudaSesion.LoginAsync(fabrica.CreateClient(), email, contrasena);
        return (org.GetProperty("id").GetGuid(), email, contrasena, admin);
    }

    public static async Task<UsuarioDto> CrearUsuarioAsync(FabricaApi fabrica, AyudaSesion admin, string rol = "ProfesionalSanitario",
        string? email = null, string? contrasena = null)
    {
        var sufijo = Guid.NewGuid().ToString("N")[..8];
        var r = await admin.Cliente(fabrica).PostAsJsonAsync("/api/usuarios", new
        {
            email = email ?? $"u-{sufijo}@sociedad.test",
            nombreCompleto = $"Usuario {sufijo}",
            contrasena = contrasena ?? AyudaSesion.ContrasenaValida(sufijo),
            roles = new[] { rol }
        });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<UsuarioDto>(AyudaSesion.Json))!;
    }
}
