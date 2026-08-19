using BksMarine.Core.Domain.Ports;
using BksMarine.Infrastructure.Auth;
using BksMarine.Infrastructure.Data;
using BksMarine.Infrastructure.Db;
using Microsoft.Extensions.DependencyInjection;

namespace BksMarine.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        JwtOptions jwt,
        SeedAdminOptions seed)
    {
        services.AddSingleton(jwt);
        services.AddSingleton(seed);
        services.AddSingleton<IUserRepository>(_ => new UserRepository(connectionString));
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<DatabaseInitializer>(sp => new DatabaseInitializer(
            connectionString,
            sp.GetRequiredService<IPasswordHasher>(),
            seed));
        return services;
    }
}
