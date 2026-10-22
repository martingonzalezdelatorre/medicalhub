using MedicalHub.Application.Identidad;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalHub.Application;

public static class DependenciasAplicacion
{
    public static IServiceCollection AddAplicacion(this IServiceCollection servicios)
    {
        servicios.AddScoped<AutenticacionServicio>();
        servicios.AddScoped<UsuariosServicio>();
        servicios.AddScoped<OrganizacionesServicio>();
        servicios.AddScoped<InicializadorPlataforma>();
        return servicios;
    }
}
