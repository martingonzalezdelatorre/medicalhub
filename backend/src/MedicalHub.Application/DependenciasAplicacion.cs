using MedicalHub.Application.Ausencias;
using MedicalHub.Application.Horas;
using MedicalHub.Application.Identidad;
using MedicalHub.Application.Notificaciones;
using MedicalHub.Application.Planificacion;
using MedicalHub.Application.Reglas;
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
        servicios.AddScoped<CatalogosServicio>();
        servicios.AddScoped<CentrosServicio>();
        servicios.AddScoped<ServiciosServicio>();
        servicios.AddScoped<TarifasServicio>();
        servicios.AddScoped<ProfesionalesServicio>();
        servicios.AddScoped<CalendarioServicio>();
        servicios.AddScoped<TurnosServicio>();
        servicios.AddScoped<AsignacionesServicio>();
        servicios.AddScoped<AutoAsignacionServicio>();
        servicios.AddScoped<CuadrantesServicio>();
        servicios.AddScoped<EvaluacionServicio>();
        servicios.AddScoped<ReglasServicio>();
        servicios.AddScoped<ExcepcionesServicio>();
        servicios.AddScoped<AusenciasServicio>();
        servicios.AddScoped<DisponibilidadServicio>();
        servicios.AddScoped<VacantesServicio>();
        servicios.AddScoped<OfertasServicio>();
        servicios.AddScoped<HorasServicio>();
        servicios.AddScoped<FichajesServicio>();
        servicios.AddScoped<InformesServicio>();
        servicios.AddScoped<DashboardServicio>();
        servicios.AddScoped<Retencion.RetencionServicio>();
        servicios.AddScoped<NotificacionesServicio>();
        return servicios;
    }
}
