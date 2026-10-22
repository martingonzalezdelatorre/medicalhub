using System.Net;
using System.Net.Http.Json;
using MedicalHub.Application.Comun;
using MedicalHub.Domain.Comun;
using MedicalHub.Domain.Identidad;
using MedicalHub.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalHub.Api.IntegrationTests;

[Collection(ColeccionApi.Nombre)]
public class AislamientoMultiempresaTests(FabricaApi fabrica)
{
    [Fact]
    public async Task Un_administrador_solo_ve_los_usuarios_de_su_sociedad()
    {
        var (_, _, _, adminA) = await Ayudas.CrearSociedadAsync(fabrica);
        var (_, _, _, adminB) = await Ayudas.CrearSociedadAsync(fabrica);
        var uA = await Ayudas.CrearUsuarioAsync(fabrica, adminA);

        var listaB = await adminB.Cliente(fabrica).GetFromJsonAsync<List<UsuarioDto>>("/api/usuarios", AyudaSesion.Json);

        Assert.DoesNotContain(listaB!, u => u.Id == uA.Id);
        Assert.DoesNotContain(listaB!, u => u.Id == adminA.UsuarioId);
        Assert.Contains(listaB!, u => u.Id == adminB.UsuarioId);
    }

    [Fact]
    public async Task Manipular_el_identificador_de_un_usuario_ajeno_devuelve_404_en_todas_las_operaciones()
    {
        var (_, _, _, adminA) = await Ayudas.CrearSociedadAsync(fabrica);
        var (_, _, _, adminB) = await Ayudas.CrearSociedadAsync(fabrica);
        var uA = await Ayudas.CrearUsuarioAsync(fabrica, adminA);
        var b = adminB.Cliente(fabrica);

        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/usuarios/{uA.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.PutAsJsonAsync($"/api/usuarios/{uA.Id}",
            new { nombreCompleto = "Hackeado", roles = new[] { "CoordinadorMedico" } })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsync($"/api/usuarios/{uA.Id}/desactivar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsync($"/api/usuarios/{uA.Id}/reiniciar-mfa", null)).StatusCode);

        // ...y el usuario de A sigue intacto
        var intacto = await adminA.Cliente(fabrica).GetFromJsonAsync<UsuarioDto>($"/api/usuarios/{uA.Id}", AyudaSesion.Json);
        Assert.True(intacto!.Activo);
        Assert.NotEqual("Hackeado", intacto.NombreCompleto);
    }

    [Fact]
    public async Task La_organizacion_enviada_en_el_cuerpo_se_ignora()
    {
        var (orgA, _, _, adminA) = await Ayudas.CrearSociedadAsync(fabrica);
        var (orgB, _, _, _) = await Ayudas.CrearSociedadAsync(fabrica);

        var r = await adminA.Cliente(fabrica).PostAsJsonAsync("/api/usuarios", new
        {
            email = $"x-{Guid.NewGuid():N}@sociedad.test", nombreCompleto = "Intruso", contrasena = AyudaSesion.ContrasenaValida("i"),
            roles = new[] { "ProfesionalSanitario" }, organizacionId = orgB
        });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var creado = await r.Content.ReadFromJsonAsync<UsuarioDto>(AyudaSesion.Json);

        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<MedicalHubDbContext>();
        var guardado = await db.Usuarios.IgnoreQueryFilters().SingleAsync(u => u.Id == creado!.Id);
        Assert.Equal(orgA, guardado.OrganizacionId);
    }

    [Fact]
    public async Task El_correo_de_otra_sociedad_no_puede_reutilizarse()
    {
        var (_, emailA, _, _) = await Ayudas.CrearSociedadAsync(fabrica);
        var (_, _, _, adminB) = await Ayudas.CrearSociedadAsync(fabrica);

        var r = await adminB.Cliente(fabrica).PostAsJsonAsync("/api/usuarios", new
        {
            email = emailA, nombreCompleto = "Duplicado", contrasena = AyudaSesion.ContrasenaValida("d"), roles = new[] { "ProfesionalSanitario" }
        });
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Un_profesional_no_puede_gestionar_usuarios()
    {
        var (_, _, _, admin) = await Ayudas.CrearSociedadAsync(fabrica);
        var contrasena = AyudaSesion.ContrasenaValida("prof");
        var prof = await Ayudas.CrearUsuarioAsync(fabrica, admin, "ProfesionalSanitario", contrasena: contrasena);
        var sesion = await AyudaSesion.LoginAsync(fabrica.CreateClient(), prof.Email, contrasena);

        Assert.Equal(HttpStatusCode.Forbidden, (await sesion.Cliente(fabrica).GetAsync("/api/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await sesion.Cliente(fabrica).GetAsync("/api/organizaciones")).StatusCode);
    }

    [Fact]
    public async Task Un_administrador_de_sociedad_no_puede_crear_ni_listar_organizaciones()
    {
        var (_, _, _, admin) = await Ayudas.CrearSociedadAsync(fabrica);
        var c = admin.Cliente(fabrica);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/organizaciones")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync("/api/organizaciones", new { })).StatusCode);
    }

    [Fact]
    public async Task El_administrador_de_plataforma_no_accede_a_los_usuarios_de_las_sociedades()
    {
        var (_, _, _, admin) = await Ayudas.CrearSociedadAsync(fabrica);
        await Ayudas.CrearUsuarioAsync(fabrica, admin);
        var plataforma = await fabrica.SesionPlataformaAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await plataforma.Cliente(fabrica).GetAsync("/api/usuarios")).StatusCode);
    }

    [Fact]
    public async Task No_se_puede_asignar_el_rol_de_plataforma_desde_una_sociedad()
    {
        var (_, _, _, admin) = await Ayudas.CrearSociedadAsync(fabrica);
        var r = await admin.Cliente(fabrica).PostAsJsonAsync("/api/usuarios", new
        {
            email = $"p-{Guid.NewGuid():N}@sociedad.test", nombreCompleto = "Escalada", contrasena = AyudaSesion.ContrasenaValida("e"),
            roles = new[] { "AdministradorPlataforma" }
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    // ── Defensa en profundidad en la capa de persistencia ──

    [Fact]
    public void Toda_entidad_de_organizacion_tiene_filtro_global_de_consulta()
    {
        using var db = NuevoContexto(null);
        var sinFiltro = db.Model.GetEntityTypes()
            .Where(t => typeof(IPerteneceAOrganizacion).IsAssignableFrom(t.ClrType))
            .Where(t => !t.GetDeclaredQueryFilters().Any())
            .Select(t => t.ClrType.Name).ToList();

        Assert.Empty(sinFiltro);
    }

    [Fact]
    public async Task Guardar_una_fila_de_otra_organizacion_es_rechazado_por_el_contexto()
    {
        var (orgA, _, _, _) = await Ayudas.CrearSociedadAsync(fabrica);
        var (orgB, _, _, _) = await Ayudas.CrearSociedadAsync(fabrica);

        using var db = NuevoContexto(orgA);
        db.Usuarios.Add(new Usuario(orgB, $"ajeno-{Guid.NewGuid():N}@x.test", "Ajeno", "hash", [Rol.ProfesionalSanitario], DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<ExcepcionProhibido>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Las_consultas_sin_sesion_no_devuelven_datos_de_ninguna_sociedad()
    {
        await Ayudas.CrearSociedadAsync(fabrica);
        using var db = NuevoContexto(null);
        Assert.DoesNotContain(await db.Usuarios.ToListAsync(), u => u.OrganizacionId != null);
    }

    private MedicalHubDbContext NuevoContexto(Guid? organizacionId)
        => new(fabrica.OpcionesDb(), new ContextoFalso(organizacionId), TimeProvider.System);

    private sealed class ContextoFalso(Guid? org) : IContextoActual
    {
        public bool EstaAutenticado => org is not null;
        public Guid? UsuarioId => org is null ? null : Guid.NewGuid();
        public Guid? OrganizacionId => org;
        public IReadOnlySet<Rol> Roles => new HashSet<Rol>();
        public bool MfaPendiente => false;
        public string? Ip => null;
        public string? IdCorrelacion => null;
    }
}
