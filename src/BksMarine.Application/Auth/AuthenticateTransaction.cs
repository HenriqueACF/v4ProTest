namespace BksMarine.Application.Auth;

public sealed record AuthenticateTransaction(string Email, string PlainPassword);
