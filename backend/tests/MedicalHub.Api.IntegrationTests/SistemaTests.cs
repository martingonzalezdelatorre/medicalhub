using System.Net;
using System.Net.Http.Json;

namespace MedicalHub.Api.IntegrationTests;

[Collection(ColeccionApi.Nombre)]
public class SistemaTests
{
    private readonly HttpClient _cliente;

    public SistemaTests(FabricaApi fabrica)
    {
        _cliente = fabrica.CreateClient();
    }

    [Fact]
    public async Task Health_live_responde_200()
        => Assert.Equal(HttpStatusCode.OK, (await _cliente.GetAsync("/health/live")).StatusCode);

    [Fact]
    public async Task Health_ready_comprueba_la_base_de_datos()
        => Assert.Equal(HttpStatusCode.OK, (await _cliente.GetAsync("/health/ready")).StatusCode);

    [Fact]
    public async Task Info_devuelve_nombre_de_la_api()
    {
        var info = await _cliente.GetFromJsonAsync<Dictionary<string, string>>("/api/sistema/info");
        Assert.Equal("MedicalHub API", info!["nombre"]);
    }

    [Fact]
    public async Task OpenApi_se_genera()
    {
        var r = await _cliente.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("/api/sistema/info", await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Respuesta_incluye_id_de_correlacion_y_cabeceras_de_seguridad()
    {
        var r = await _cliente.GetAsync("/api/sistema/info");
        Assert.True(r.Headers.Contains("X-Correlation-Id"));
        Assert.Equal("nosniff", r.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task Ruta_inexistente_sin_sesion_exige_autenticacion_sin_revelar_rutas()
    {
        var r = await _cliente.GetAsync("/api/no-existe");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType?.MediaType);
    }
}
