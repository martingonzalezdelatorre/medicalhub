using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MedicalHub.Api.IntegrationTests;

[Collection(ColeccionApi.Nombre)]
public class TarifasTests(FabricaApi fabrica)
{
    private async Task<(AyudaSesion admin, HttpClient c)> NuevaSociedadAsync()
    {
        var (_, _, _, admin) = await Ayudas.CrearSociedadAsync(fabrica);
        return (admin, admin.Cliente(fabrica));
    }

    private static async Task<Guid> CrearTipoTurnoAsync(HttpClient c, Guid servicioId, string nombre = "Guardia 24 h")
        => (await Planif.LeerAsync<IdDto>(await c.PostAsJsonAsync($"/api/servicios/{servicioId}/tipos-turno",
            new { nombre, horaInicio = "08:00:00", horaFin = "08:00:00", esGuardia = true }), HttpStatusCode.Created)).Id;

    private static async Task<List<JsonElement>> ListarAsync(HttpClient c, Guid servicioId)
        => await c.GetFromJsonAsync<List<JsonElement>>($"/api/servicios/{servicioId}/tarifas", AyudaSesion.Json) ?? [];

    [Fact]
    public async Task Las_tarifas_se_guardan_actualizan_y_quitan_por_servicio()
    {
        var (_, c) = await NuevaSociedadAsync();
        var servicio = await Planif.CrearServicioAsync(c, await Planif.CrearCentroAsync(c));
        var turno = await CrearTipoTurnoAsync(c, servicio);
        var anestesista = await Planif.CrearCatalogoAsync(c, "categorias-profesionales", "Médico anestesista");
        var enfermeria = await Planif.CrearCatalogoAsync(c, "categorias-profesionales", "Enfermería");

        var r = await c.PutAsJsonAsync($"/api/servicios/{servicio}/tarifas", new
        {
            tarifas = new[]
            {
                new { tipoTurnoId = turno, categoriaProfesionalId = anestesista, importe = 600.5m },
                new { tipoTurnoId = turno, categoriaProfesionalId = enfermeria, importe = 250m }
            }
        });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var lista = await ListarAsync(c, servicio);
        Assert.Equal(2, lista.Count);
        Assert.Equal(600.5m, lista.Single(t => t.GetProperty("categoriaProfesionalId").GetGuid() == anestesista).GetProperty("importe").GetDecimal());

        // Reemplazar: cambia una, quita la otra.
        await c.PutAsJsonAsync($"/api/servicios/{servicio}/tarifas", new
        {
            tarifas = new[] { new { tipoTurnoId = turno, categoriaProfesionalId = anestesista, importe = 700m } }
        });
        lista = await ListarAsync(c, servicio);
        Assert.Single(lista);
        Assert.Equal(700m, lista[0].GetProperty("importe").GetDecimal());
    }

    [Fact]
    public async Task Rechaza_importes_negativos_duplicados_y_turnos_ajenos_al_servicio()
    {
        var (_, c) = await NuevaSociedadAsync();
        var centro = await Planif.CrearCentroAsync(c);
        var servicio = await Planif.CrearServicioAsync(c, centro);
        var otro = await Planif.CrearServicioAsync(c, centro);
        var turno = await CrearTipoTurnoAsync(c, servicio);
        var turnoAjeno = await CrearTipoTurnoAsync(c, otro);
        var perfil = await Planif.CrearCatalogoAsync(c, "categorias-profesionales");

        Task<HttpResponseMessage> Enviar(params object[] tarifas) => c.PutAsJsonAsync($"/api/servicios/{servicio}/tarifas", new { tarifas });

        Assert.Equal(HttpStatusCode.BadRequest, (await Enviar(new { tipoTurnoId = turno, categoriaProfesionalId = perfil, importe = -1m })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Enviar(
            new { tipoTurnoId = turno, categoriaProfesionalId = perfil, importe = 1m },
            new { tipoTurnoId = turno, categoriaProfesionalId = perfil, importe = 2m })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Enviar(new { tipoTurnoId = turnoAjeno, categoriaProfesionalId = perfil, importe = 1m })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Enviar(new { tipoTurnoId = turno, categoriaProfesionalId = Guid.NewGuid(), importe = 1m })).StatusCode);
        Assert.Empty(await ListarAsync(c, servicio));
    }

    [Fact]
    public async Task RRHH_gestiona_perfiles_turnos_y_tarifas_pero_no_estructura_ni_usuarios()
    {
        var (admin, c) = await NuevaSociedadAsync();
        var servicio = await Planif.CrearServicioAsync(c, await Planif.CrearCentroAsync(c));

        var contrasena = AyudaSesion.ContrasenaValida("p1");
        var u = await Ayudas.CrearUsuarioAsync(fabrica, admin, "RecursosHumanos", contrasena: contrasena);
        var rrhh = (await AyudaSesion.LoginAsync(fabrica.CreateClient(), u.Email, contrasena)).Cliente(fabrica);

        // Perfiles (catálogo) y turnos
        var perfil = await Planif.CrearCatalogoAsync(rrhh, "categorias-profesionales", "Celador");
        var turno = await CrearTipoTurnoAsync(rrhh, servicio, "Noche");
        Assert.Equal(HttpStatusCode.NoContent, (await rrhh.PostAsync($"/api/servicios/{servicio}/tipos-turno/{turno}/desactivar", null)).StatusCode);

        // Tarifas
        var r = await rrhh.PutAsJsonAsync($"/api/servicios/{servicio}/tarifas",
            new { tarifas = new[] { new { tipoTurnoId = turno, categoriaProfesionalId = perfil, importe = 90m } } });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Single(await ListarAsync(rrhh, servicio));

        // Lectura de centros y servicios
        Assert.Equal(HttpStatusCode.OK, (await rrhh.GetAsync("/api/centros")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await rrhh.GetAsync($"/api/servicios/{servicio}")).StatusCode);

        // Lo demás sigue siendo del administrador o de planificación
        Assert.Equal(HttpStatusCode.Forbidden, (await rrhh.PostAsJsonAsync("/api/centros", new { nombre = "X", tipo = "Hospital", zonaHoraria = "Europe/Madrid" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await rrhh.PostAsync($"/api/servicios/{servicio}/desactivar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await rrhh.GetAsync("/api/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await rrhh.GetAsync("/api/profesionales")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await rrhh.GetAsync("/api/cuadrantes")).StatusCode);
    }

    [Fact]
    public async Task Solo_el_administrador_de_la_sociedad_ve_las_tarifas_y_otra_sociedad_no_las_alcanza()
    {
        var (admin, c) = await NuevaSociedadAsync();
        var servicio = await Planif.CrearServicioAsync(c, await Planif.CrearCentroAsync(c));

        var contrasena = AyudaSesion.ContrasenaValida("p1");
        var u = await Ayudas.CrearUsuarioAsync(fabrica, admin, "CoordinadorMedico", contrasena: contrasena);
        var coord = (await AyudaSesion.LoginAsync(fabrica.CreateClient(), u.Email, contrasena)).Cliente(fabrica);
        Assert.Equal(HttpStatusCode.Forbidden, (await coord.GetAsync($"/api/servicios/{servicio}/tarifas")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await coord.PutAsJsonAsync($"/api/servicios/{servicio}/tarifas", new { tarifas = Array.Empty<object>() })).StatusCode);

        var (_, otra) = await NuevaSociedadAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await otra.GetAsync($"/api/servicios/{servicio}/tarifas")).StatusCode);
    }
}
