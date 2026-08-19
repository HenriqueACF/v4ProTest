using BksMarine.Application.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace BksMarine.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IAuthenticateUser, AuthenticateUser>();
        return services;
    }
}
