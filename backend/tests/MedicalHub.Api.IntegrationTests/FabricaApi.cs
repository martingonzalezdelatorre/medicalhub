using MedicalHub.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalHub.Api.IntegrationTests;

/// <summary>
/// Levanta la API real contra una base SQL Server EFÍMERA (una por ejecución) que se elimina al terminar.
/// Servidor por defecto: LocalDB. Para otro servidor, defina MEDICALHUB_TEST_DB (cadena de conexión base).
/// </summary>
public sealed class FabricaApi : WebApplicationFactory<Program>
{
    public const string EmailPlataforma = "plataforma@pruebas.medicalhub.local";
    public const string ContrasenaPlataforma = "Prueba-Clinica-Segura-2026!";

    private readonly string _cadenaConexion;
    private AyudaSesion? _plataforma;
    private readonly SemaphoreSlim _bloqueo = new(1, 1);

    public FabricaApi()
    {
        var baseCadena = Environment.GetEnvironmentVariable("MEDICALHUB_TEST_DB")
            ?? @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True";
        var b = new SqlConnectionStringBuilder(baseCadena) { InitialCatalog = $"MedicalHub_Test_{Guid.NewGuid():N}" };
        _cadenaConexion = b.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:MedicalHub", _cadenaConexion);
        builder.UseSetting("Base:MigrarAlIniciar", "true");
        builder.UseSetting("Bootstrap:AdministradorPlataforma:Email", EmailPlataforma);
        builder.UseSetting("Bootstrap:AdministradorPlataforma:Contrasena", ContrasenaPlataforma);
        // Las pruebas hacen muchos inicios de sesión desde la misma IP.
        builder.UseSetting("Seguridad:LimiteLoginPorMinuto", "10000");
    }

    /// <summary>Sesión completa (con MFA activado) del administrador de plataforma, creada una sola vez.</summary>
    public async Task<AyudaSesion> SesionPlataformaAsync()
    {
        await _bloqueo.WaitAsync();
        try
        {
            if (_plataforma is not null) return _plataforma;
            var cliente = CreateClient();
            var login = await AyudaSesion.LoginAsync(cliente, EmailPlataforma, ContrasenaPlataforma);
            return _plataforma = await login.ActivarMfaAsync(cliente);
        }
        finally { _bloqueo.Release(); }
    }

    public DbContextOptions<MedicalHubDbContext> OpcionesDb()
        => new DbContextOptionsBuilder<MedicalHubDbContext>().UseSqlServer(_cadenaConexion).Options;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        try
        {
            // Con el host ya detenido, se elimina la base efímera desde master cerrando cualquier conexión residual.
            SqlConnection.ClearAllPools();
            var b = new SqlConnectionStringBuilder(_cadenaConexion);
            var nombre = b.InitialCatalog.Replace("]", "]]");
            b.InitialCatalog = "master";
            using var c = new SqlConnection(b.ConnectionString);
            c.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = $"ALTER DATABASE [{nombre}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{nombre}];";
            cmd.ExecuteNonQuery();
        }
        catch { /* una base efímera huérfana no debe romper la ejecución de pruebas */ }
    }
}

[CollectionDefinition(Nombre)]
public sealed class ColeccionApi : ICollectionFixture<FabricaApi>
{
    public const string Nombre = "api";
}
