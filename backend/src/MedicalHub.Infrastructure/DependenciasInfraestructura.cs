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

        // Consultas divididas: las entidades con colecciones propias (Servicio, Profesional…) se cargan con una consulta por colección en lugar de
        // un LEFT JOIN con ORDER BY. Así SQL Server no reserva memoria para ordenar filas con columnas nvarchar(max) (en máquinas justas de memoria,
        // esa reserva llegaba a esperar más de 20 s: RESOURCE_SEMAPHORE) y se evita el producto cartesiano entre colecciones.
        servicios.AddDbContext<MedicalHubDbContext>(o => o.UseSqlServer(cadena, sql =>
        {
            sql.EnableRetryOnFailure();
            sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        }));
        servicios.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<MedicalHubDbContext>());
        return servicios;
    }
}
