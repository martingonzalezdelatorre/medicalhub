using MedicalHub.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalHub.Api.IntegrationTests;

/// <summary>Levanta la API real contra una base SQL Server de pruebas (LocalDB por defecto).</summary>
public sealed class FabricaApi : WebApplicationFactory<Program>
{
    // Variable de entorno para usar otro servidor (CI): MEDICALHUB_TEST_DB
    private static readonly string CadenaConexion =
        Environment.GetEnvironmentVariable("MEDICALHUB_TEST_DB")
        ?? @"Server=(localdb)\MSSQLLocalDB;Database=MedicalHub_Test;Trusted_Connection=True;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:MedicalHub", CadenaConexion);
    }

    public void PrepararBaseDeDatos()
    {
        using var ambito = Services.CreateScope();
        ambito.ServiceProvider.GetRequiredService<MedicalHubDbContext>().Database.Migrate();
    }
}
