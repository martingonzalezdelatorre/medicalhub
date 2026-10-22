using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MedicalHub.Api.IntegrationTests;

/// <summary>Sociedad con un servicio de quirófano (turnos Mañana 08–15 y Noche 22–08) y requisitos de cobertura.</summary>
public sealed class Escenario
{
    public required FabricaApi Fabrica { get; init; }
    public required AyudaSesion Admin { get; init; }
    public required HttpClient C { get; init; }
    public Guid Adjunto { get; init; }
    public Guid Residente { get; init; }
    public Guid Competencia { get; init; }
    public Guid Centro { get; init; }
    public Guid Servicio { get; init; }
    public Guid Manana { get; init; }
    public Guid Noche { get; init; }

    public static async Task<Escenario> CrearAsync(FabricaApi fabrica, string zona = "Europe/Madrid")
    {
        var (_, _, _, admin) = await Ayudas.CrearSociedadAsync(fabrica);
        var c = admin.Cliente(fabrica);
        var adjunto = await Planif.CrearCatalogoAsync(c, "categorias-profesionales", Planif.Unico("Adjunto"));
        var residente = await Planif.CrearCatalogoAsync(c, "categorias-profesionales", Planif.Unico("Residente"));
        var competencia = await Planif.CrearCatalogoAsync(c, "competencias", Planif.Unico("Via aerea"));
        var centro = await Planif.CrearCentroAsync(c, zona);
        var servicio = await Planif.CrearServicioAsync(c, centro, [adjunto, residente], [competencia]);

        var manana = (await Planif.LeerAsync<IdDto>(await c.PostAsJsonAsync($"/api/servicios/{servicio}/tipos-turno",
            new { nombre = "Mañana", horaInicio = "08:00", horaFin = "15:00", esGuardia = false }), HttpStatusCode.Created)).Id;
        var noche = (await Planif.LeerAsync<IdDto>(await c.PostAsJsonAsync($"/api/servicios/{servicio}/tipos-turno",
            new { nombre = "Noche", horaInicio = "22:00", horaFin = "08:00", esGuardia = false }), HttpStatusCode.Created)).Id;
        var req = await c.PutAsJsonAsync($"/api/servicios/{servicio}/requisitos", new
        {
            requisitos = new object[]
            {
                new { tipoTurnoId = manana, numeroProfesionales = 2, categoriaProfesionalId = (Guid?)null },
                new { tipoTurnoId = noche, numeroProfesionales = 1, categoriaProfesionalId = (Guid?)adjunto }
            }
        });
        Assert.Equal(HttpStatusCode.OK, req.StatusCode);

        return new Escenario
        {
            Fabrica = fabrica, Admin = admin, C = c, Adjunto = adjunto, Residente = residente, Competencia = competencia,
            Centro = centro, Servicio = servicio, Manana = manana, Noche = noche
        };
    }

    /// <summary>Profesional elegible para el servicio (autorizado y con la competencia) salvo que se indique lo contrario.</summary>
    public Task<Guid> ProfesionalAsync(bool autorizado = true, bool conCompetencia = true, Guid? categoria = null, bool activo = true, string? nombre = null)
        => ProfesionalEnAsync(autorizado ? [Servicio] : [], conCompetencia, categoria, activo, nombre);

    public async Task<Guid> ProfesionalEnAsync(Guid[] servicios, bool conCompetencia = true, Guid? categoria = null, bool activo = true, string? nombre = null)
    {
        var id = await Planif.CrearProfesionalAsync(C, Planif.Profesional(categoria ?? Adjunto,
            competencias: conCompetencia ? [new { competenciaId = Competencia, vigenteHasta = (string?)null }] : null,
            servicios: servicios, nombre: nombre));
        if (!activo) await C.PostAsync($"/api/profesionales/{id}/desactivar", null);
        return id;
    }

    public async Task<CuadranteResp> CuadranteAsync(string desde = "2026-11-02", string hasta = "2026-11-08", bool generar = true, Guid? servicio = null)
    {
        var r = await C.PostAsJsonAsync("/api/cuadrantes", new { servicioId = servicio ?? Servicio, nombre = Planif.Unico("Cuad"), desde, hasta, generarTurnos = generar });
        var json = await Planif.LeerAsync<JsonElement>(r, HttpStatusCode.Created);
        return new CuadranteResp(json.GetProperty("id").GetGuid(), json);
    }

    public async Task<List<JsonElement>> TurnosAsync(string desde = "2026-11-02", string hasta = "2026-11-08", string extra = "", HttpClient? cliente = null)
        => (await (cliente ?? C).GetFromJsonAsync<List<JsonElement>>($"/api/turnos?desde={desde}&hasta={hasta}{extra}", AyudaSesion.Json))!;

    /// <summary>Id de la n-ésima necesidad del primer turno del tipo indicado en el día indicado.</summary>
    public async Task<Guid> NecesidadAsync(string fecha, Guid tipoTurno, int indice = 0)
    {
        var turno = (await TurnosAsync(fecha, fecha)).Single(t => t.GetProperty("tipoTurnoId").GetGuid() == tipoTurno);
        return turno.GetProperty("necesidades")[indice].GetProperty("id").GetGuid();
    }

    public Task<HttpResponseMessage> AsignarAsync(Guid necesidad, Guid profesional, HttpClient? cliente = null)
        => (cliente ?? C).PostAsJsonAsync($"/api/necesidades/{necesidad}/asignaciones", new { profesionalId = profesional });

    public async Task<Guid> AsignarOkAsync(Guid necesidad, Guid profesional)
    {
        var r = await AsignarAsync(necesidad, profesional);
        return (await Planif.LeerAsync<IdDto>(r, HttpStatusCode.Created)).Id;
    }

    /// <summary>Usuario con el rol indicado y su cliente autenticado.</summary>
    public async Task<(UsuarioDto usuario, HttpClient cliente)> UsuarioAsync(string rol)
    {
        var pwd = AyudaSesion.ContrasenaValida("u");
        var u = await Ayudas.CrearUsuarioAsync(Fabrica, Admin, rol, contrasena: pwd);
        return (u, (await AyudaSesion.LoginAsync(Fabrica.CreateClient(), u.Email, pwd)).Cliente(Fabrica));
    }
}

public sealed record CuadranteResp(Guid Id, JsonElement Json);
