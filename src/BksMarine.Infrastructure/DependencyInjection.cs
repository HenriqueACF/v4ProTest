using BksMarine.Core.Domain.Ports;
using BksMarine.Infrastructure.Auth;
using BksMarine.Infrastructure.Data;
using BksMarine.Infrastructure.Db;
using BksMarine.Infrastructure.Reports;
using BksMarine.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace BksMarine.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        JwtOptions jwt,
        SeedAdminOptions seed,
        bool seedDemoEnabled)
    {
        services.AddSingleton(jwt);
        services.AddSingleton(seed);
        services.AddSingleton<IUserRepository>(_ => new UserRepository(connectionString));
        services.AddSingleton<ILoginAttemptRepository>(_ => new LoginAttemptRepository(connectionString));
        services.AddSingleton<IRefreshTokenRepository>(_ => new RefreshTokenRepository(connectionString));
        services.AddSingleton<IPortRepository>(_ => new PortRepository(connectionString));
        services.AddSingleton<IBerthRepository>(_ => new BerthRepository(connectionString));
        services.AddSingleton<IShipRepository>(_ => new ShipRepository(connectionString));
        services.AddSingleton<IOperationRepository>(_ => new OperationRepository(connectionString));
        services.AddSingleton<IStorageClient>(_ => new LocalStorageClient(AppContext.BaseDirectory));
        services.AddSingleton<IReportGenerator>(_ => new QuestPdfReportGenerator(Path.Combine(AppContext.BaseDirectory, "uploads")));
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<DatabaseInitializer>(sp => new DatabaseInitializer(
            connectionString,
            sp.GetRequiredService<IPasswordHasher>(),
            seed,
            seedDemoEnabled));
        return services;
    }
}
