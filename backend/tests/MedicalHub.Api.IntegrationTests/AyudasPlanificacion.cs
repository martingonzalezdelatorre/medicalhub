using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MedicalHub.Api.IntegrationTests;

public sealed record IdDto(Guid Id);

/// <summary>Atajos para montar datos de planificación en las pruebas de integración.</summary>
public static class Planif
{
    public static string Unico(string prefijo) => $"{prefijo}-{Guid.NewGuid():N}"[..Math.Min(40, prefijo.Length + 9)];

    public static async Task<T> LeerAsync<T>(HttpResponseMessage r, HttpStatusCode esperado = HttpStatusCode.OK)
    {
        Assert.Equal(esperado, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<T>(AyudaSesion.Json))!;
    }

    public static async Task<Guid> CrearCatalogoAsync(HttpClient c, string ruta, string? nombre = null)
        => (await LeerAsync<IdDto>(await c.PostAsJsonAsync($"/api/{ruta}", new { nombre = nombre ?? Unico("cat"), descripcion = (string?)null }),
            HttpStatusCode.Created)).Id;

    public static async Task<Guid> CrearCentroAsync(HttpClient c, string zona = "Europe/Madrid")
        => (await LeerAsync<IdDto>(await c.PostAsJsonAsync("/api/centros",
            new { nombre = Unico("Hospital"), tipo = "Hospital", direccion = "Calle Mayor 1", zonaHoraria = zona }), HttpStatusCode.Created)).Id;

    public static async Task<Guid> CrearServicioAsync(HttpClient c, Guid centroId, Guid[]? categorias = null, Guid[]? competencias = null,
        string? configuracionReglas = null)
        => (await LeerAsync<IdDto>(await c.PostAsJsonAsync("/api/servicios", new
        {
            centroId,
            nombre = Unico("Servicio"),
            descripcion = "Quirófano",
            coordinadorUsuarioId = (Guid?)null,
            configuracionReglas,
            categoriasAdmitidasIds = categorias ?? [],
            competenciasRequeridasIds = competencias ?? []
        }), HttpStatusCode.Created)).Id;

    public static object Profesional(Guid categoriaId, Guid[]? especialidades = null, object[]? competencias = null, Guid[]? servicios = null,
        string? colegiado = null, Guid? usuarioId = null, string? nombre = null)
        => new
        {
            usuarioId,
            nombre = nombre ?? "Ana",
            apellidos = Unico("García"),
            email = "contacto@sociedad.test",
            telefono = "600111222",
            numeroColegiado = colegiado,
            categoriaId,
            especialidadesIds = especialidades ?? [],
            competencias = competencias ?? [],
            serviciosIds = servicios ?? []
        };

    public static async Task<Guid> CrearProfesionalAsync(HttpClient c, object solicitud)
        => (await LeerAsync<IdDto>(await c.PostAsJsonAsync("/api/profesionales", solicitud), HttpStatusCode.Created)).Id;
}
