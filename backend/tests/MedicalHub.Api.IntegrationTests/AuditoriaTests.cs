using System.Net.Http.Json;
using MedicalHub.Domain.Seguridad;
using MedicalHub.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalHub.Api.IntegrationTests;

[Collection(ColeccionApi.Nombre)]
public class AuditoriaTests(FabricaApi fabrica)
{
    [Fact]
    public async Task Crear_un_usuario_queda_auditado_sin_exponer_la_contrasena()
    {
        var (orgId, _, _, admin) = await Ayudas.CrearSociedadAsync(fabrica);
        var u = await Ayudas.CrearUsuarioAsync(fabrica, admin);

        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<MedicalHubDbContext>();
        var registro = await db.RegistrosAuditoria.IgnoreQueryFilters()
            .SingleAsync(a => a.Entidad == "Usuario" && a.EntidadId == u.Id.ToString() && a.Accion == "Alta");

        Assert.Equal(orgId, registro.OrganizacionId);
        Assert.Equal(admin.UsuarioId, registro.UsuarioId);
        Assert.NotNull(registro.IdCorrelacion);
        Assert.Contains("(protegido)", registro.Cambios);
        Assert.DoesNotContain("AQAAAA", registro.Cambios); // prefijo típico de un hash de ASP.NET Identity
    }

    [Fact]
    public async Task Los_intentos_de_acceso_quedan_como_eventos_de_seguridad()
    {
        var (_, email, _, _) = await Ayudas.CrearSociedadAsync(fabrica);
        await AyudaSesion.LoginCrudoAsync(fabrica.CreateClient(), email, "Mala-contrasena-1!");

        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<MedicalHubDbContext>();
        var tipos = await db.EventosSeguridad.Where(e => e.Email == email).Select(e => e.Tipo).ToListAsync();

        Assert.Contains(TipoEventoSeguridad.InicioSesionCorrecto, tipos);
        Assert.Contains(TipoEventoSeguridad.InicioSesionFallido, tipos);
    }

    [Fact]
    public async Task Los_cambios_de_roles_se_auditan()
    {
        var (_, _, _, admin) = await Ayudas.CrearSociedadAsync(fabrica);
        var u = await Ayudas.CrearUsuarioAsync(fabrica, admin);
        var r = await admin.Cliente(fabrica).PutAsJsonAsync($"/api/usuarios/{u.Id}",
            new { nombreCompleto = u.NombreCompleto, roles = new[] { "CoordinadorMedico" } });
        Assert.True(r.IsSuccessStatusCode);

        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<MedicalHubDbContext>();
        var cambios = await db.RegistrosAuditoria.IgnoreQueryFilters()
            .Where(a => a.Entidad == "UsuarioRol" && a.EntidadId == u.Id.ToString()).Select(a => a.Accion).ToListAsync();

        Assert.Contains("Baja", cambios);
        Assert.Contains("Alta", cambios);
    }
}

