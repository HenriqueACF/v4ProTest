namespace BksMarine.Application.Auth;

public sealed class AuthThrottleOptions
{
    public int MaxFailures { get; init; } = 5;
    public int WindowMinutes { get; init; } = 15;
    public int RefreshLifetimeDays { get; init; } = 14;
}
