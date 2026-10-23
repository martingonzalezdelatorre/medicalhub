using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MedicalHub.Api.IntegrationTests;

/// <summary>
/// Escenario de aceptación del proyecto, de principio a fin y solo a través de la API:
/// el coordinador publica el cuadrante → un profesional solicita una ausencia → se aprueba y aparece la vacante →
/// candidatos compatibles → oferta → el profesional acepta → el coordinador aprueba → cuadrante actualizado,
/// con notificaciones, historial, informes y aislamiento entre sociedades.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public class EscenarioPilotoTests(FabricaApi fabrica)
{
    [Fact]
    public async Task Recorrido_completo_del_escenario_piloto()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var dia = "2026-11-03";
        var cuadrante = await e.CuadranteAsync("2026-11-02", "2026-11-08");
        var (titular, clienteTitular, _, _) = await e.ProfesionalConUsuarioAsync(nombre: "Elena");
        var (sustituto, clienteSustituto, _, _) = await e.ProfesionalConUsuarioAsync(nombre: "Luis");
        var otroSinCompetencia = await e.ProfesionalAsync(conCompetencia: false);
        var necesidad = await e.NecesidadAsync(dia, e.Manana);

        // 1. El coordinador asigna y publica el cuadrante
        await e.AsignarOkAsync(necesidad, titular);
        Assert.Equal(HttpStatusCode.OK, (await e.C.PostAsync($"/api/cuadrantes/{cuadrante.Id}/publicar", null)).StatusCode);
        Assert.Equal("Publicado", (await e.C.GetFromJsonAsync<JsonElement>($"/api/cuadrantes/{cuadrante.Id}", AyudaSesion.Json)).GetProperty("estado").GetString());

        // 2. El profesional solicita una ausencia (sin datos de salud) y la coordinación recibe el aviso
        var solicitud = await clienteTitular.PostAsJsonAsync("/api/mis-ausencias", new { tipo = "Permiso", desde = dia, hasta = dia, comentario = "Asunto familiar" });
        var ausencia = (await Planif.LeerAsync<JsonElement>(solicitud, HttpStatusCode.Created)).GetProperty("id").GetGuid();
        Assert.Contains(await Notificaciones(e.C), n => n == "AusenciaSolicitada");

        // 3. El coordinador la aprueba: el sistema detecta la vacante
        var aprobada = await Planif.LeerAsync<JsonElement>(await e.C.PostAsync($"/api/ausencias/{ausencia}/aprobar", null));
        Assert.Single(aprobada.GetProperty("vacantes").EnumerateArray());
        var vacante = (await e.C.GetFromJsonAsync<List<JsonElement>>("/api/vacantes?estado=Abierta", AyudaSesion.Json))!.Single();
        var vacanteId = vacante.GetProperty("id").GetGuid();
        Assert.Contains(await Notificaciones(clienteTitular), n => n == "AusenciaAprobada");

        // 4. Candidatos compatibles: el sustituto lo es y quien no tiene la competencia exigida, no
        var candidatos = await e.C.GetFromJsonAsync<List<JsonElement>>($"/api/necesidades/{vacante.GetProperty("necesidadId").GetGuid()}/candidatos", AyudaSesion.Json);
        Assert.True(candidatos!.Single(c => c.GetProperty("profesionalId").GetGuid() == sustituto).GetProperty("elegible").GetBoolean());
        Assert.False(candidatos!.Single(c => c.GetProperty("profesionalId").GetGuid() == otroSinCompetencia).GetProperty("elegible").GetBoolean());

        // 5. Oferta → el profesional acepta → el coordinador aprueba
        var oferta = (await Planif.LeerAsync<List<JsonElement>>(
            await e.C.PostAsJsonAsync($"/api/vacantes/{vacanteId}/ofertas", new { profesionalIds = new[] { sustituto }, caducaEnHoras = 48 }), HttpStatusCode.Created)).Single();
        var ofertaId = oferta.GetProperty("id").GetGuid();
        Assert.Contains(await Notificaciones(clienteSustituto), n => n == "OfertaRecibida");
        Assert.Equal(HttpStatusCode.OK, (await clienteSustituto.PostAsJsonAsync($"/api/mis-ofertas/{ofertaId}/aceptar", new { })).StatusCode);
        Assert.Contains(await Notificaciones(e.C), n => n == "OfertaAceptada");
        Assert.Equal(HttpStatusCode.OK, (await e.C.PostAsync($"/api/ofertas/{ofertaId}/aprobar", null)).StatusCode);

        // 6. Cuadrante actualizado, vacante cubierta, avisos e historial conservado
        var turno = (await e.TurnosAsync(dia, dia)).Single(t => t.GetProperty("tipoTurnoId").GetGuid() == e.Manana);
        var asignados = turno.GetProperty("necesidades")[0].GetProperty("asignaciones").EnumerateArray().Select(a => a.GetProperty("profesionalId").GetGuid()).ToList();
        Assert.Contains(sustituto, asignados);
        Assert.DoesNotContain(titular, asignados);
        Assert.Single(await e.C.GetFromJsonAsync<List<JsonElement>>("/api/vacantes?estado=Cubierta", AyudaSesion.Json) ?? []);
        Assert.Contains(await Notificaciones(clienteSustituto), n => n == "SustitucionAprobada");
        var historial = await e.C.GetFromJsonAsync<JsonElement>($"/api/ausencias/{ausencia}", AyudaSesion.Json);
        Assert.Equal(["Solicitada", "Aprobada"], historial.GetProperty("decisiones").EnumerateArray().Select(d => d.GetProperty("decision").GetString()));
        Assert.Equal("Aprobada", (await clienteSustituto.GetFromJsonAsync<List<JsonElement>>("/api/mis-ofertas", AyudaSesion.Json))!.Single().GetProperty("estado").GetString());

        // 7. Informes desde el servidor y aislamiento entre sociedades
        var cobertura = await e.C.GetFromJsonAsync<JsonElement>("/api/informes/cobertura?desde=2026-11-02&hasta=2026-11-08", AyudaSesion.Json);
        Assert.Equal(1, cobertura.GetProperty("total").GetProperty("cubiertas").GetInt32());
        var horas = await e.C.GetFromJsonAsync<JsonElement>("/api/informes/horas?desde=2026-11-02&hasta=2026-11-08", AyudaSesion.Json);
        Assert.Equal(sustituto, Assert.Single(horas.GetProperty("filas").EnumerateArray()).GetProperty("profesionalId").GetGuid());

        var otra = await Escenario.CrearAsync(fabrica);
        Assert.Empty(await otra.C.GetFromJsonAsync<List<JsonElement>>("/api/vacantes", AyudaSesion.Json) ?? []);
        Assert.Empty(await otra.C.GetFromJsonAsync<List<JsonElement>>("/api/ofertas", AyudaSesion.Json) ?? []);
        Assert.Equal(HttpStatusCode.NotFound, (await otra.C.PostAsync($"/api/ofertas/{ofertaId}/aprobar", null)).StatusCode);
        Assert.Empty(await Notificaciones(otra.C));
    }

    private static async Task<List<string?>> Notificaciones(HttpClient c)
        => (await c.GetFromJsonAsync<List<JsonElement>>("/api/notificaciones", AyudaSesion.Json))!.Select(n => n.GetProperty("tipo").GetString()).ToList();
}
