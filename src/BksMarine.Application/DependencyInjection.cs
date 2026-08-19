using BksMarine.Application.Auth;
using BksMarine.Application.Employees;
using BksMarine.Application.Locations;
using BksMarine.Application.Operations;
using BksMarine.Application.Reports;
using Microsoft.Extensions.DependencyInjection;

namespace BksMarine.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(new AuthThrottleOptions());
        services.AddSingleton<IAuthenticateUser, AuthenticateUser>();
        services.AddSingleton<IRefreshSession, RefreshSession>();
        services.AddSingleton<ILogoutSession, LogoutSession>();
        services.AddSingleton<IResetPassword, ResetPassword>();

        services.AddSingleton<ICreateShip, CreateShip>();
        services.AddSingleton<IUpdateShip, UpdateShip>();
        services.AddSingleton<IDeactivateShip, DeactivateShip>();
        services.AddSingleton<IListShips, ListShips>();
        services.AddSingleton<IRegisterOperation, RegisterOperation>();
        services.AddSingleton<IListOperations, ListOperations>();
        services.AddSingleton<IGetOperationDetail, GetOperationDetail>();
        services.AddSingleton<IMarkTransmitted, MarkTransmitted>();
        services.AddSingleton<IGenerateOperationReport, GenerateOperationReport>();

        services.AddSingleton<ICreateEmployee, CreateEmployee>();
        services.AddSingleton<IUpdateEmployee, UpdateEmployee>();
        services.AddSingleton<IDeactivateEmployee, DeactivateEmployee>();
        services.AddSingleton<IListEmployees, ListEmployees>();
        services.AddSingleton<IListProfiles, ListProfiles>();

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
