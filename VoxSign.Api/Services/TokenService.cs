using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using VoxSign.Api.Data;

namespace VoxSign.Api.Services;

public class JwtOptions
{
    public const string Section = "Jwt";

    public string Issuer { get; set; } = "voxsign-api";
    public string Audience { get; set; } = "voxsign-app";
    /// <summary>Clé de signature HMAC (32 caractères minimum). À fournir par la configuration en production.</summary>
    public string SigningKey { get; set; } = string.Empty;
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromDays(30);

    public SymmetricSecurityKey Key => new(Encoding.UTF8.GetBytes(SigningKey));
}

public class TokenService(IOptions<JwtOptions> options, TimeProvider clock)
{
    readonly JwtOptions _options = options.Value;

    public (string Token, DateTimeOffset ExpiresAt) Create(User user)
    {
        var now = clock.GetUtcNow();
        var expires = now + _options.Lifetime;
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
            ]),
            SigningCredentials = new SigningCredentials(_options.Key, SecurityAlgorithms.HmacSha256),
        });
        return (token, expires);
    }
}

public static class ClaimsPrincipalExtensions
{
    public static Guid UserId(this ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                   ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? throw new InvalidOperationException("Jeton sans identifiant utilisateur."));

    public static Guid? UserIdOrNull(this ClaimsPrincipal principal) =>
        principal.Identity?.IsAuthenticated == true ? principal.UserId() : null;
}
