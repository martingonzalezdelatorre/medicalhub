using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MedicalHub.Application.Comun;
using MedicalHub.Application.Retencion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalHub.Api.IntegrationTests;

[Collection(ColeccionApi.Nombre)]
public class FichajesTests(FabricaApi fabrica)
{
    // Puerta del Sol (Madrid)
    private const double Lat = 40.41683, Lon = -3.70379;

    private sealed class RelojAdelantado(TimeSpan desfase) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow + desfase;
    }

    /// <summary>Profesional con un turno «día completo» de hoy (hora de Madrid), de modo que cualquier momento de la prueba cae dentro.</summary>
    private async Task<(Escenario e, Guid prof, HttpClient cliente, Guid asignacion)> CasoAsync(bool conAsignacion = true)
    {
        var e = await Escenario.CrearAsync(fabrica);
        var hoy = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid")).Date;
        var dia = (await Planif.LeerAsync<IdDto>(await e.C.PostAsJsonAsync($"/api/servicios/{e.Servicio}/tipos-turno",
            new { nombre = "Dia completo", horaInicio = "00:00", horaFin = "23:59", esGuardia = false }), HttpStatusCode.Created)).Id;
        var req = await e.C.PutAsJsonAsync($"/api/servicios/{e.Servicio}/requisitos",
            new { requisitos = new object[] { new { tipoTurnoId = dia, numeroProfesionales = 1, categoriaProfesionalId = (Guid?)null } } });
        Assert.Equal(HttpStatusCode.OK, req.StatusCode);
        var f = $"{hoy:yyyy-MM-dd}";
        await e.CuadranteAsync(f, f);
        var (prof, cliente, _, _) = await e.ProfesionalConUsuarioAsync(nombre: "Fichador");
        var asig = conAsignacion ? await e.AsignarOkAsync(await e.NecesidadAsync(f, dia), prof) : Guid.Empty;
        return (e, prof, cliente, asig);
    }

    private static Task<HttpResponseMessage> Entrada(HttpClient c, double? lat = Lat, double? lon = Lon, double? precision = 10)
        => c.PostAsJsonAsync("/api/fichajes/entrada", new { latitud = lat, longitud = lon, precisionMetros = precision });

    private static Task<HttpResponseMessage> Salida(HttpClient c, double? lat = Lat, double? lon = Lon)
        => c.PostAsJsonAsync("/api/fichajes/salida", new { latitud = lat, longitud = lon, precisionMetros = 10.0 });

    private static string Hoy() => $"{DateTime.UtcNow.AddDays(-2):yyyy-MM-dd}";
    private static string Manana() => $"{DateTime.UtcNow.AddDays(2):yyyy-MM-dd}";

    [Fact]
    public async Task El_ciclo_completo_entrada_pausa_reanudar_salida_se_vuelca_a_las_horas_del_profesional()
    {
        var (e, _, cliente, asig) = await CasoAsync();
        var estado = await cliente.GetFromJsonAsync<JsonElement>("/api/fichajes/estado", AyudaSesion.Json);
        Assert.Equal(JsonValueKind.Null, estado.GetProperty("actual").ValueKind);
        Assert.Equal(asig, estado.GetProperty("fichable").GetProperty("asignacionId").GetGuid());

        var entrada = await Planif.LeerAsync<JsonElement>(await Entrada(cliente));
        Assert.Equal("EnCurso", entrada.GetProperty("estado").GetString());
        Assert.False(entrada.GetProperty("centroConGeovalla").GetBoolean());
        Assert.Equal(JsonValueKind.Null, entrada.GetProperty("entradaFueraDeZona").ValueKind);       // el centro no tiene geovalla

        Assert.Equal("EnPausa", (await Planif.LeerAsync<JsonElement>(await cliente.PostAsync("/api/fichajes/pausa", null))).GetProperty("estado").GetString());
        Assert.Equal("EnCurso", (await Planif.LeerAsync<JsonElement>(await cliente.PostAsync("/api/fichajes/reanudar", null))).GetProperty("estado").GetString());
        var salida = await Planif.LeerAsync<JsonElement>(await Salida(cliente));
        Assert.Equal("Cerrado", salida.GetProperty("estado").GetString());
        Assert.Single(salida.GetProperty("pausas").EnumerateArray());

        // El fichaje cerrado aparece en «mis horas» sin cambios en los informes
        var hoy = $"{DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}";
        var horas = await cliente.GetFromJsonAsync<List<JsonElement>>($"/api/mis-horas?desde={hoy}&hasta={Manana()}", AyudaSesion.Json);
        var linea = Assert.Single(horas!, l => l.GetProperty("asignacionId").GetGuid() == asig);
        Assert.Equal(JsonValueKind.String, linea.GetProperty("registroId").ValueKind);
        Assert.Equal(linea.GetProperty("realesMin").GetInt32() + linea.GetProperty("ajustesMin").GetInt32(), linea.GetProperty("efectivasMin").GetInt32());

        var mios = await cliente.GetFromJsonAsync<List<JsonElement>>($"/api/mis-fichajes?desde={hoy}&hasta={Manana()}", AyudaSesion.Json);
        Assert.Single(mios!);

        // Una asignación con horas registradas ya no admite otro fichaje
        Assert.Equal(HttpStatusCode.Conflict, (await Entrada(cliente)).StatusCode);
        _ = e;
    }

    [Fact]
    public async Task Solo_puede_haber_una_jornada_abierta_y_las_operaciones_exigen_el_estado_correcto()
    {
        var (_, _, cliente, _) = await CasoAsync();
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsync("/api/fichajes/pausa", null)).StatusCode);       // sin jornada
        Assert.Equal(HttpStatusCode.Conflict, (await Salida(cliente)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Entrada(cliente)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Entrada(cliente)).StatusCode);                                      // ya abierta
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await cliente.PostAsync("/api/fichajes/reanudar", null)).StatusCode);   // sin pausa
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync("/api/fichajes/pausa", null)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await cliente.PostAsync("/api/fichajes/pausa", null)).StatusCode);      // ya en pausa
        Assert.Equal(HttpStatusCode.OK, (await Salida(cliente)).StatusCode);                                             // se puede salir desde una pausa
    }

    [Fact]
    public async Task Sin_turno_asignado_no_se_puede_fichar_y_la_ubicacion_se_valida()
    {
        var (_, _, cliente, _) = await CasoAsync(conAsignacion: false);
        Assert.Equal(HttpStatusCode.Conflict, (await Entrada(cliente)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Entrada(cliente, Lat, null)).StatusCode);       // media ubicación
        Assert.Equal(HttpStatusCode.BadRequest, (await Entrada(cliente, 123, Lon)).StatusCode);        // latitud imposible
    }

    [Fact]
    public async Task Con_geovalla_el_fichaje_fuera_de_zona_se_admite_pero_queda_marcado_para_la_coordinacion()
    {
        var (e, prof, cliente, _) = await CasoAsync();
        // Solo la administración de la sociedad define la geovalla
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PutAsJsonAsync($"/api/centros/{e.Centro}/geovalla", new { latitud = Lat, longitud = Lon, radioMetros = 150 })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await e.C.PutAsJsonAsync($"/api/centros/{e.Centro}/geovalla", new { latitud = Lat, longitud = Lon, radioMetros = 5 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await e.C.PutAsJsonAsync($"/api/centros/{e.Centro}/geovalla", new { latitud = Lat, longitud = (double?)null, radioMetros = 150 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await e.C.PutAsJsonAsync($"/api/centros/{e.Centro}/geovalla", new { latitud = Lat, longitud = Lon, radioMetros = 150 })).StatusCode);

        var entrada = await Planif.LeerAsync<JsonElement>(await Entrada(cliente, Lat + 0.01, Lon));           // ≈ 1,1 km
        Assert.True(entrada.GetProperty("centroConGeovalla").GetBoolean());
        Assert.True(entrada.GetProperty("entradaFueraDeZona").GetBoolean());
        var salida = await Planif.LeerAsync<JsonElement>(await Salida(cliente, Lat + 0.0005, Lon));           // ≈ 56 m
        Assert.False(salida.GetProperty("salidaFueraDeZona").GetBoolean());

        var desde = Hoy();
        var lista = await e.C.GetFromJsonAsync<List<JsonElement>>($"/api/fichajes/fuera-de-zona?desde={desde}&hasta={Manana()}", AyudaSesion.Json);
        var fila = Assert.Single(lista!);
        Assert.Equal(prof, fila.GetProperty("profesionalId").GetGuid());
        Assert.True(fila.GetProperty("entradaFueraDeZona").GetBoolean());

        // El profesional no ve ese listado; otra sociedad tampoco ve estos fichajes
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync($"/api/fichajes/fuera-de-zona?desde={desde}&hasta={Manana()}")).StatusCode);
        var otra = await Escenario.CrearAsync(fabrica);
        Assert.Empty(await otra.C.GetFromJsonAsync<List<JsonElement>>($"/api/fichajes/fuera-de-zona?desde={desde}&hasta={Manana()}", AyudaSesion.Json) ?? []);

        // Quitar la geovalla (todo a null)
        Assert.Equal(HttpStatusCode.OK, (await e.C.PutAsJsonAsync($"/api/centros/{e.Centro}/geovalla", new { latitud = (double?)null, longitud = (double?)null, radioMetros = (int?)null })).StatusCode);
    }

    [Fact]
    public async Task Fichar_sin_ubicacion_se_admite_y_queda_como_sin_ubicacion()
    {
        var (_, _, cliente, _) = await CasoAsync();
        var entrada = await Planif.LeerAsync<JsonElement>(await Entrada(cliente, null, null, null));
        Assert.True(entrada.GetProperty("entradaSinUbicacion").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await Salida(cliente, null, null)).StatusCode);
    }

    [Fact]
    public async Task Solo_un_profesional_con_ficha_puede_fichar_y_hay_que_estar_autenticado()
    {
        var (e, _, _, _) = await CasoAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await Entrada(e.C)).StatusCode);                               // el administrador no tiene ficha profesional
        Assert.Equal(HttpStatusCode.Unauthorized, (await fabrica.CreateClient().PostAsync("/api/fichajes/pausa", null)).StatusCode);
    }

    [Fact]
    public async Task La_retencion_anonimiza_las_coordenadas_y_conserva_el_fichaje_y_sus_horas()
    {
        var (_, _, cliente, _) = await CasoAsync();
        await Entrada(cliente);
        await Salida(cliente);

        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<IAppDbContext>();
        var antes = await db.Fichajes.IgnoreQueryFilters().CountAsync(f => f.EntradaLatitud != null);
        Assert.True(antes > 0);

        var hoy = await new RetencionServicio(db, TimeProvider.System).DepurarAsync(new OpcionesRetencion(), CancellationToken.None);
        Assert.Equal(0, hoy.UbicacionesFichaje);
        var desactivado = await new RetencionServicio(db, new RelojAdelantado(TimeSpan.FromDays(4000))).DepurarAsync(
            new OpcionesRetencion { NotificacionesLeidasDias = 0, NotificacionesDias = 0, EventosSeguridadDias = 0, TokensRefrescoDias = 0, AuditoriaDias = 0, UbicacionesFichajeDias = 0 },
            CancellationToken.None);
        Assert.Equal(0, desactivado.Total);

        var futuro = await new RetencionServicio(db, new RelojAdelantado(TimeSpan.FromDays(200))).DepurarAsync(
            new OpcionesRetencion { NotificacionesLeidasDias = 0, NotificacionesDias = 0, EventosSeguridadDias = 0, TokensRefrescoDias = 0, AuditoriaDias = 0 }, CancellationToken.None);
        Assert.True(futuro.UbicacionesFichaje >= antes);
        Assert.Equal(0, await db.Fichajes.IgnoreQueryFilters().CountAsync(f => f.EntradaLatitud != null || f.SalidaLatitud != null));
        Assert.True(await db.Fichajes.IgnoreQueryFilters().AnyAsync());
    }
}
