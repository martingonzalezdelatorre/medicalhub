using MedicalHub.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalHub.Infrastructure;

public static class DependenciasInfraestructura
{
    public const string NombreCadenaConexion = "MedicalHub";

    public static IServiceCollection AddInfraestructura(this IServiceCollection servicios, IConfiguration configuracion)
    {
        var cadena = configuracion.GetConnectionString(NombreCadenaConexion)
            ?? throw new InvalidOperationException(
                $"Falta la cadena de conexión '{NombreCadenaConexion}'. Defínala con la variable de entorno ConnectionStrings__{NombreCadenaConexion}.");

        servicios.AddDbContext<MedicalHubDbContext>(o => o.UseSqlServer(cadena,
            sql => sql.EnableRetryOnFailure()));
        return servicios;
    }
}
