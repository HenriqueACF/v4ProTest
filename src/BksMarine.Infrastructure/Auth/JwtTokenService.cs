using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BksMarine.Core.Domain.Ports;
using BksMarine.Core.Domain.Profiles;
using BksMarine.Core.Domain.Users;
using Microsoft.IdentityModel.Tokens;

namespace BksMarine.Infrastructure.Auth;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "bks-marine";
    public string Audience { get; init; } = "bks-marine-api";
    public string SigningKey { get; init; } = "";
    public int ExpirationMinutes { get; init; } = 480;
}

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(JwtOptions options) => _options = options;

    public IssuedToken Issue(User user, Profile profile)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes);

        var claims = new[]
        {
            new Claim("userId", user.Id.ToString()),
            new Claim("email", user.Email.Value),
            new Claim("perfil", profile.Name.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var value = new JwtSecurityTokenHandler().WriteToken(token);
        return new IssuedToken(value, expiresAt);
    }
}
