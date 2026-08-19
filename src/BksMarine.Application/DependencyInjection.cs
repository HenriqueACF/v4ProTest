using BksMarine.Application.Auth;
using BksMarine.Application.Locations;
using Microsoft.Extensions.DependencyInjection;

namespace BksMarine.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IAuthenticateUser, AuthenticateUser>();

        services.AddSingleton<ICreatePort, CreatePort>();
        services.AddSingleton<IUpdatePort, UpdatePort>();
        services.AddSingleton<IDeactivatePort, DeactivatePort>();
        services.AddSingleton<IListPorts, ListPorts>();

        services.AddSingleton<ICreateBerth, CreateBerth>();
        services.AddSingleton<IUpdateBerth, UpdateBerth>();
        services.AddSingleton<IDeactivateBerth, DeactivateBerth>();
        services.AddSingleton<IListBerthsByPort, ListBerthsByPort>();

        return services;
    }
}
