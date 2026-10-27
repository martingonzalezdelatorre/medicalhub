using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalHub.Api.IntegrationTests;

/// <summary>
/// Red de seguridad: ningún endpoint puede quedar expuesto por olvido. Todo endpoint declara su autorización de forma explícita
/// y los públicos son exactamente los de la lista blanca.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class CoberturaAutorizacionTests(FabricaApi fabrica)
{
    /// <summary>Únicos endpoints accesibles sin sesión (método y ruta).</summary>
    private static readonly string[] PublicosPermitidos =
    [
        "POST api/autenticacion/login",
        "POST api/autenticacion/refrescar",
        "GET api/sistema/info",
        "ANY health/live",
        "ANY health/ready"
    ];

    private IEnumerable<RouteEndpoint> Endpoints()
    {
        _ = fabrica.CreateClient();
        return fabrica.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>();
    }

    private static string Clave(RouteEndpoint e)
    {
        var metodo = e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.FirstOrDefault() ?? "ANY";
        return $"{metodo} {e.RoutePattern.RawText?.TrimStart('/')}";
    }

    [Fact]
    public void Todo_endpoint_declara_su_autorizacion_de_forma_explicita()
    {
        var sinDeclarar = Endpoints()
            .Where(e => e.Metadata.GetMetadata<IAuthorizeData>() is null && e.Metadata.GetMetadata<IAllowAnonymous>() is null)
            .Select(Clave).ToList();
        Assert.True(sinDeclarar.Count == 0, "Endpoints sin [Authorize] ni [AllowAnonymous]: " + string.Join(", ", sinDeclarar));
    }

    [Fact]
    public void Los_unicos_endpoints_publicos_son_los_de_la_lista_blanca()
    {
        var publicos = Endpoints().Where(e => e.Metadata.GetMetadata<IAllowAnonymous>() is not null).Select(Clave).Order().ToList();
        // OpenAPI solo se publica en Desarrollo
        var inesperados = publicos.Where(p => !PublicosPermitidos.Contains(p) && !p.Contains("openapi")).ToList();
        Assert.True(inesperados.Count == 0, "Endpoints públicos no previstos: " + string.Join(", ", inesperados));
    }

    [Fact]
    public async Task Sin_sesion_los_endpoints_protegidos_responden_401()
    {
        var cliente = fabrica.CreateClient();
        var protegidos = Endpoints()
            .Where(e => e.Metadata.GetMetadata<IAuthorizeData>() is not null && e.Metadata.GetMetadata<IAllowAnonymous>() is null)
            .Where(e => e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains("GET") == true && !(e.RoutePattern.RawText ?? "").Contains('{'))
            .Select(e => "/" + e.RoutePattern.RawText!.TrimStart('/')).Distinct().ToList();
        Assert.NotEmpty(protegidos);
        foreach (var ruta in protegidos)
            Assert.True((await cliente.GetAsync(ruta)).StatusCode == HttpStatusCode.Unauthorized, $"{ruta} debería exigir sesión");
    }

    [Fact]
    public async Task La_api_envia_cabeceras_de_seguridad_y_no_revela_el_servidor()
    {
        var r = await fabrica.CreateClient().GetAsync("/health/live");
        Assert.Equal("nosniff", r.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", r.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("frame-ancestors 'none'", r.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("no-referrer", r.Headers.GetValues("Referrer-Policy").Single());
        Assert.False(r.Headers.Contains("Server"));
    }

    [Fact]
    public void Las_pruebas_no_se_ven_limitadas_por_los_topes_de_peticiones()
    {
        var o = fabrica.Services.GetRequiredService<MedicalHub.Application.Comun.OpcionesSeguridad>();
        Assert.True(o.LimiteGlobalPorMinuto >= 1_000_000, $"Límite global efectivo: {o.LimiteGlobalPorMinuto}");
        Assert.True(o.LimiteLoginPorMinuto >= 10_000, $"Límite de login efectivo: {o.LimiteLoginPorMinuto}");
    }

    [Fact]
    public void Kestrel_limita_el_cuerpo_de_las_peticiones_y_oculta_su_cabecera()
    {
        var kestrel = fabrica.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>>().Value;
        Assert.Equal(1_048_576, kestrel.Limits.MaxRequestBodySize);
        Assert.False(kestrel.AddServerHeader);
    }
}
