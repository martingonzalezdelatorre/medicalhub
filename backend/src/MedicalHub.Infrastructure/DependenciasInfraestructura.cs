using MedicalHub.Application.Comun;
using MedicalHub.Infrastructure.Persistencia;
using MedicalHub.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalHub.Infrastructure;

public static class DependenciasInfraestructura
{
    public const string NombreCadenaConexion = "Conn";

    public static IServiceCollection AddInfraestructura(
        this IServiceCollection servicios, IConfiguration configuracion, bool esDesarrollo)
    {
        var cadena = configuracion.GetConnectionString(NombreCadenaConexion)
            ?? throw new InvalidOperationException(
                $"Falta la cadena de conexión '{NombreCadenaConexion}'. Defínala con la variable de entorno ConnectionStrings__{NombreCadenaConexion}.");

        var opciones = new OpcionesSeguridad();
        configuracion.GetSection(OpcionesSeguridad.Seccion).Bind(opciones);
        if (string.IsNullOrWhiteSpace(opciones.ClaveJwt))
        {
            // Sin clave configurada solo se tolera en Desarrollo (clave efímera: al reiniciar, las sesiones caducan).
            if (!esDesarrollo)
                throw new InvalidOperationException("Falta Seguridad__ClaveJwt (≥ 32 caracteres) en el entorno.");
            opciones.ClaveJwt = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
        }
        EmisorTokens.ObtenerClave(opciones);

        servicios.AddSingleton(opciones);
        servicios.AddSingleton(TimeProvider.System);
        servicios.AddSingleton<IHasherContrasena, HasherContrasena>();
        servicios.AddSingleton<IEmisorTokens, EmisorTokens>();
        servicios.AddSingleton<IServicioTotp, ServicioTotp>();
        servicios.AddSingleton<IProtectorSecretos, ProtectorSecretos>();

        servicios.AddDbContext<MedicalHubDbContext>(o => o.UseSqlServer(cadena, sql => sql.EnableRetryOnFailure()));
        servicios.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<MedicalHubDbContext>());
        return servicios;
    }
}
