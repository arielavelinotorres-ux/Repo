using HomeClass.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HomeClass.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<ICursoService, CursoService>();
        services.AddScoped<ICalificacionService, CalificacionService>();
        services.AddScoped<IAsistenciaService, AsistenciaService>();
        services.AddScoped<IRiesgoService, RiesgoService>();
        services.AddScoped<IPerfilService, PerfilService>();
        services.AddScoped<IPortalEstudianteService, PortalEstudianteService>();
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }
}
