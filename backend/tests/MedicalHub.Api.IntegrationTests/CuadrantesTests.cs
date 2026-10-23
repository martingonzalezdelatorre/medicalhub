using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalHub.Api.IntegrationTests;

[Collection(ColeccionApi.Nombre)]
public class CuadrantesTests(FabricaApi fabrica)
{
    [Fact]
    public async Task Un_cuadrante_semanal_se_genera_desde_los_requisitos_del_servicio()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var cuadrante = await e.CuadranteAsync();

        // 7 días × (Mañana + Noche) = 14 turnos; plazas: 7 × (2 + 1) = 21
        Assert.Equal(14, cuadrante.Json.GetProperty("totalTurnos").GetInt32());
        Assert.Equal(21, cuadrante.Json.GetProperty("plazasTotales").GetInt32());
        Assert.Equal(0, cuadrante.Json.GetProperty("plazasCubiertas").GetInt32());
        Assert.Equal("Borrador", cuadrante.Json.GetProperty("estado").GetString());

        // Los turnos existen sin ningún profesional asignado
        var turnos = await e.TurnosAsync();
        Assert.Equal(14, turnos.Count);
        Assert.All(turnos, t => Assert.True(t.GetProperty("sinCubrir").GetBoolean()));
    }

    [Fact]
    public async Task El_turno_nocturno_cruza_medianoche_y_se_muestra_en_hora_local_del_centro()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-02");

        var noche = (await e.TurnosAsync("2026-11-02", "2026-11-02")).Single(t => t.GetProperty("tipoTurnoId").GetGuid() == e.Noche);
        Assert.Equal("22:00", noche.GetProperty("inicioLocal").GetString());
        Assert.Equal("08:00", noche.GetProperty("finLocal").GetString());
        Assert.True(noche.GetProperty("cruzaMedianoche").GetBoolean());
        Assert.Equal(10, noche.GetProperty("duracionHoras").GetDouble());
        Assert.Equal("2026-11-02", noche.GetProperty("fecha").GetString()); // el turno pertenece al día en que empieza
    }

    [Theory]
    [InlineData("2026-10-24", 11)] // fin del horario de verano
    [InlineData("2026-03-28", 9)]  // inicio del horario de verano
    [InlineData("2026-07-10", 10)]
    public async Task La_noche_de_un_cambio_de_hora_tiene_la_duracion_real(string fecha, double horas)
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync(fecha, fecha);

        var noche = (await e.TurnosAsync(fecha, fecha)).Single(t => t.GetProperty("tipoTurnoId").GetGuid() == e.Noche);
        Assert.Equal(horas, noche.GetProperty("duracionHoras").GetDouble());
        Assert.Equal("22:00", noche.GetProperty("inicioLocal").GetString());
        Assert.Equal("08:00", noche.GetProperty("finLocal").GetString());
    }

    [Fact]
    public async Task Un_centro_en_canarias_calcula_el_horario_con_su_zona()
    {
        var e = await Escenario.CrearAsync(fabrica, "Atlantic/Canary");
        await e.CuadranteAsync("2026-07-10", "2026-07-10");
        var manana = (await e.TurnosAsync("2026-07-10", "2026-07-10")).Single(t => t.GetProperty("tipoTurnoId").GetGuid() == e.Manana);

        Assert.Equal("2026-07-10T07:00:00+00:00", manana.GetProperty("inicioUtc").GetDateTimeOffset().ToString("yyyy-MM-ddTHH:mm:sszzz")); // UTC+1 en verano
        Assert.Equal("08:00", manana.GetProperty("inicioLocal").GetString());
        Assert.Equal("Atlantic/Canary", manana.GetProperty("zonaHoraria").GetString());
    }

    [Fact]
    public async Task No_se_permiten_cuadrantes_solapados_del_mismo_servicio()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-08");

        var r = await e.C.PostAsJsonAsync("/api/cuadrantes", new { servicioId = e.Servicio, desde = "2026-11-08", hasta = "2026-11-14", generarTurnos = false });
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        await e.CuadranteAsync("2026-11-09", "2026-11-15", generar: false); // contiguo: sí
    }

    [Fact]
    public async Task El_periodo_maximo_es_de_93_dias_y_el_rango_de_consulta_de_100()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var r = await e.C.PostAsJsonAsync("/api/cuadrantes", new { servicioId = e.Servicio, desde = "2026-01-01", hasta = "2026-06-01", generarTurnos = false });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await e.C.GetAsync("/api/turnos?desde=2026-01-01&hasta=2026-06-01")).StatusCode);
    }

    [Fact]
    public async Task Se_pueden_anadir_y_quitar_turnos_en_un_borrador_con_necesidades_propias()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var cuadrante = await e.CuadranteAsync("2026-11-02", "2026-11-04", generar: false);
        Assert.Empty(await e.TurnosAsync("2026-11-02", "2026-11-04"));

        // Necesidades explícitas: 1 adjunto + 1 residente
        var r = await e.C.PostAsJsonAsync($"/api/cuadrantes/{cuadrante.Id}/turnos", new
        {
            fecha = "2026-11-03", tipoTurnoId = e.Manana,
            necesidades = new[] { new { numeroPlazas = 1, categoriaProfesionalId = (Guid?)e.Adjunto }, new { numeroPlazas = 1, categoriaProfesionalId = (Guid?)e.Residente } }
        });
        var turnoId = (await Planif.LeerAsync<IdDto>(r, HttpStatusCode.Created)).Id;
        var turno = (await e.TurnosAsync("2026-11-03", "2026-11-03")).Single();
        Assert.Equal(2, turno.GetProperty("necesidades").GetArrayLength());
        Assert.Equal(2, turno.GetProperty("plazasTotales").GetInt32());

        // sin necesidades en la petición: toma los requisitos del servicio (2 plazas de cualquier categoría)
        var r2 = await e.C.PostAsJsonAsync($"/api/cuadrantes/{cuadrante.Id}/turnos", new { fecha = "2026-11-04", tipoTurnoId = e.Manana });
        Assert.Equal(HttpStatusCode.Created, r2.StatusCode);
        Assert.Equal(2, (await e.TurnosAsync("2026-11-04", "2026-11-04")).Single().GetProperty("plazasTotales").GetInt32());

        // duplicado, fuera de periodo y tipo ajeno
        Assert.Equal(HttpStatusCode.Conflict, (await e.C.PostAsJsonAsync($"/api/cuadrantes/{cuadrante.Id}/turnos", new { fecha = "2026-11-03", tipoTurnoId = e.Manana })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await e.C.PostAsJsonAsync($"/api/cuadrantes/{cuadrante.Id}/turnos", new { fecha = "2026-12-01", tipoTurnoId = e.Manana })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await e.C.PostAsJsonAsync($"/api/cuadrantes/{cuadrante.Id}/turnos", new { fecha = "2026-11-02", tipoTurnoId = Guid.NewGuid() })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await e.C.DeleteAsync($"/api/turnos/{turnoId}")).StatusCode);
        Assert.Empty(await e.TurnosAsync("2026-11-03", "2026-11-03"));
    }

    [Fact]
    public async Task No_se_pueden_reducir_ni_quitar_necesidades_con_profesionales_asignados()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-02");
        var necesidad = await e.NecesidadAsync("2026-11-02", e.Manana);
        var p1 = await e.ProfesionalAsync();
        var p2 = await e.ProfesionalAsync();
        await e.AsignarOkAsync(necesidad, p1);
        await e.AsignarOkAsync(necesidad, p2);
        var turnoId = (await e.TurnosAsync("2026-11-02", "2026-11-02")).Single(t => t.GetProperty("tipoTurnoId").GetGuid() == e.Manana).GetProperty("id").GetGuid();

        var reducir = await e.C.PutAsJsonAsync($"/api/turnos/{turnoId}/necesidades", new { necesidades = new[] { new { numeroPlazas = 1, categoriaProfesionalId = (Guid?)null } } });
        Assert.Equal(HttpStatusCode.Conflict, reducir.StatusCode);
        var quitar = await e.C.PutAsJsonAsync($"/api/turnos/{turnoId}/necesidades", new { necesidades = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Conflict, quitar.StatusCode);
        var ampliar = await e.C.PutAsJsonAsync($"/api/turnos/{turnoId}/necesidades", new { necesidades = new[] { new { numeroPlazas = 3, categoriaProfesionalId = (Guid?)null } } });
        Assert.Equal(HttpStatusCode.NoContent, ampliar.StatusCode);
    }

    // ── Publicación y versiones ──

    [Fact]
    public async Task Publicar_guarda_una_version_y_bloquea_la_edicion_hasta_reabrir()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var cuadrante = await e.CuadranteAsync("2026-11-02", "2026-11-02");
        var prof = await e.ProfesionalAsync(nombre: "Marta");
        var necesidad = await e.NecesidadAsync("2026-11-02", e.Manana);
        await e.AsignarOkAsync(necesidad, prof);

        var validacion = await e.C.GetFromJsonAsync<JsonElement>($"/api/cuadrantes/{cuadrante.Id}/validacion", AyudaSesion.Json);
        Assert.True(validacion.GetProperty("puedePublicarse").GetBoolean());
        Assert.Contains(validacion.GetProperty("incidencias").EnumerateArray(), i => i.GetProperty("codigo").GetString() == "CoberturaMinima" && i.GetProperty("severidad").GetString() == "Advertencia");

        var publicado = await Planif.LeerAsync<JsonElement>(await e.C.PostAsync($"/api/cuadrantes/{cuadrante.Id}/publicar", null));
        Assert.Equal("Publicado", publicado.GetProperty("estado").GetString());
        Assert.Equal(1, publicado.GetProperty("version").GetInt32());

        // Edición bloqueada (409) en publicado
        Assert.Equal(HttpStatusCode.Conflict, (await e.AsignarAsync(necesidad, await e.ProfesionalAsync())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await e.C.PostAsJsonAsync($"/api/cuadrantes/{cuadrante.Id}/turnos", new { fecha = "2026-11-02", tipoTurnoId = e.Manana })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await e.C.PostAsync($"/api/cuadrantes/{cuadrante.Id}/publicar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await e.C.DeleteAsync($"/api/cuadrantes/{cuadrante.Id}")).StatusCode);

        // Versión 1 con la instantánea (incluye el nombre del profesional)
        var v1 = await e.C.GetFromJsonAsync<JsonElement>($"/api/cuadrantes/{cuadrante.Id}/versiones/1", AyudaSesion.Json);
        Assert.Contains("Marta", v1.GetProperty("contenidoJson").GetString());
        Assert.Equal(2, v1.GetProperty("resumen").GetProperty("turnos").GetInt32());

        // Reabrir, modificar y republicar → versión 2; la 1 se conserva intacta
        await e.C.PostAsync($"/api/cuadrantes/{cuadrante.Id}/reabrir", null);
        await e.AsignarOkAsync(necesidad, await e.ProfesionalAsync(nombre: "Otro"));
        var v2 = await Planif.LeerAsync<JsonElement>(await e.C.PostAsync($"/api/cuadrantes/{cuadrante.Id}/publicar", null));
        Assert.Equal(2, v2.GetProperty("version").GetInt32());

        var versiones = await e.C.GetFromJsonAsync<List<JsonElement>>($"/api/cuadrantes/{cuadrante.Id}/versiones", AyudaSesion.Json);
        Assert.Equal([2, 1], versiones!.Select(v => v.GetProperty("numero").GetInt32()));
        Assert.Equal(1, versiones![1].GetProperty("plazasCubiertas").GetInt32());
        Assert.Equal(2, versiones![0].GetProperty("plazasCubiertas").GetInt32());
        var v1Releida = await e.C.GetFromJsonAsync<JsonElement>($"/api/cuadrantes/{cuadrante.Id}/versiones/1", AyudaSesion.Json);
        Assert.DoesNotContain("Otro", v1Releida.GetProperty("contenidoJson").GetString());
    }

    [Fact]
    public async Task Un_cuadrante_sin_turnos_no_se_publica()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var cuadrante = await e.CuadranteAsync(generar: false);
        var r = await e.C.PostAsync($"/api/cuadrantes/{cuadrante.Id}/publicar", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
    }

    [Fact]
    public async Task Si_un_profesional_asignado_deja_de_ser_elegible_la_publicacion_se_bloquea()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var cuadrante = await e.CuadranteAsync("2026-11-02", "2026-11-02");
        var prof = await e.ProfesionalAsync();
        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-02", e.Manana), prof);

        await e.C.PostAsync($"/api/profesionales/{prof}/desactivar", null);

        var validacion = await e.C.GetFromJsonAsync<JsonElement>($"/api/cuadrantes/{cuadrante.Id}/validacion", AyudaSesion.Json);
        Assert.False(validacion.GetProperty("puedePublicarse").GetBoolean());
        Assert.Contains(validacion.GetProperty("incidencias").EnumerateArray(), i => i.GetProperty("codigo").GetString() == "ProfesionalInactivo" && i.GetProperty("severidad").GetString() == "Bloqueante");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await e.C.PostAsync($"/api/cuadrantes/{cuadrante.Id}/publicar", null)).StatusCode);
    }

    [Fact]
    public async Task Los_borradores_se_eliminan_y_los_publicados_se_archivan()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var borrador = await e.CuadranteAsync("2026-11-02", "2026-11-02");
        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-02", e.Manana), await e.ProfesionalAsync());
        Assert.Equal(HttpStatusCode.NoContent, (await e.C.DeleteAsync($"/api/cuadrantes/{borrador.Id}")).StatusCode);
        Assert.Empty(await e.TurnosAsync("2026-11-02", "2026-11-02"));
        Assert.Equal(HttpStatusCode.NotFound, (await e.C.GetAsync($"/api/cuadrantes/{borrador.Id}")).StatusCode);

        var publicado = await e.CuadranteAsync("2026-11-09", "2026-11-09");
        await e.C.PostAsync($"/api/cuadrantes/{publicado.Id}/publicar", null);
        var archivado = await Planif.LeerAsync<JsonElement>(await e.C.PostAsync($"/api/cuadrantes/{publicado.Id}/archivar", null));
        Assert.Equal("Archivado", archivado.GetProperty("estado").GetString());
    }

    // ── Plantillas ──

    [Fact]
    public async Task Una_plantilla_copia_la_estructura_sin_profesionales_a_otro_periodo()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var origen = await e.CuadranteAsync("2026-11-02", "2026-11-08");
        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-02", e.Manana), await e.ProfesionalAsync());

        var plantilla = await Planif.LeerAsync<JsonElement>(await e.C.PostAsJsonAsync($"/api/cuadrantes/{origen.Id}/plantilla", new { nombre = "Semana tipo" }), HttpStatusCode.Created);
        Assert.Equal(7, plantilla.GetProperty("diasPeriodo").GetInt32());
        Assert.Equal(14, plantilla.GetProperty("lineas").GetInt32());
        Assert.Equal(HttpStatusCode.Conflict, (await e.C.PostAsJsonAsync($"/api/cuadrantes/{origen.Id}/plantilla", new { nombre = "Semana tipo" })).StatusCode);

        var copia = await Planif.LeerAsync<JsonElement>(await e.C.PostAsJsonAsync("/api/cuadrantes/desde-plantilla",
            new { plantillaId = plantilla.GetProperty("id").GetGuid(), desde = "2026-11-16" }), HttpStatusCode.Created);
        Assert.Equal("2026-11-22", copia.GetProperty("hasta").GetString());
        Assert.Equal(14, copia.GetProperty("totalTurnos").GetInt32());
        Assert.Equal(21, copia.GetProperty("plazasTotales").GetInt32());
        Assert.Equal(0, copia.GetProperty("plazasCubiertas").GetInt32()); // sin profesionales

        // la misma plantilla en un periodo ocupado → conflicto
        Assert.Equal(HttpStatusCode.Conflict, (await e.C.PostAsJsonAsync("/api/cuadrantes/desde-plantilla",
            new { plantillaId = plantilla.GetProperty("id").GetGuid(), desde = "2026-11-20" })).StatusCode);

        var lista = await e.C.GetFromJsonAsync<List<JsonElement>>("/api/plantillas-cuadrante", AyudaSesion.Json);
        Assert.Contains(lista!, p => p.GetProperty("nombre").GetString() == "Semana tipo");
        Assert.Equal(HttpStatusCode.NoContent, (await e.C.DeleteAsync($"/api/plantillas-cuadrante/{plantilla.GetProperty("id").GetGuid()}")).StatusCode);
    }

    // ── Exportación ──

    [Fact]
    public async Task El_csv_incluye_las_plazas_sin_cubrir_neutraliza_formulas_y_queda_registrado()
    {
        var e = await Escenario.CrearAsync(fabrica);
        var cuadrante = await e.CuadranteAsync("2026-11-02", "2026-11-02");
        var peligroso = await e.ProfesionalAsync(nombre: "=HIPERVINCULO(\"x\")");
        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-02", e.Manana), peligroso);

        var r = await e.C.GetAsync($"/api/cuadrantes/{cuadrante.Id}/exportar.csv");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("text/csv", r.Content.Headers.ContentType!.MediaType);
        var bytes = await r.Content.ReadAsByteArrayAsync();
        Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]); // BOM UTF-8 para Excel
        var texto = Encoding.UTF8.GetString(bytes);

        Assert.StartsWith("﻿Fecha;Inicio;Fin;Centro;Servicio;Turno", texto);
        Assert.Contains("02/11/2026;08:00;15:00", texto);
        Assert.Contains("SIN CUBRIR", texto);
        Assert.Contains("'=HIPERVINCULO", texto);              // fórmula neutralizada
        Assert.DoesNotContain(";=HIPERVINCULO", texto);

        // Registro de la exportación
        var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<MedicalHub.Infrastructure.Persistencia.MedicalHubDbContext>();
        Assert.Contains(db.EventosSeguridad.Where(x => x.Tipo == MedicalHub.Domain.Seguridad.TipoEventoSeguridad.ExportacionDatos).ToList(),
            x => x.Detalle!.Contains(cuadrante.Id.ToString()) && x.UsuarioId == e.Admin.UsuarioId);
    }

    [Fact]
    public void La_celda_csv_escapa_separadores_comillas_y_formulas()
    {
        Assert.Equal("\"a;b\"", MedicalHub.Application.Planificacion.CuadrantesServicio.CeldaCsv("a;b"));
        Assert.Equal("\"di \"\"hola\"\"\"", MedicalHub.Application.Planificacion.CuadrantesServicio.CeldaCsv("di \"hola\""));
        foreach (var inicio in new[] { "=1+1", "+1", "-1", "@SUMA" })
            Assert.StartsWith("'", MedicalHub.Application.Planificacion.CuadrantesServicio.CeldaCsv(inicio));
        Assert.Equal("normal", MedicalHub.Application.Planificacion.CuadrantesServicio.CeldaCsv("normal"));
    }
}
