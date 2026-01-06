using Auth.Domain.Interfaces.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Auth.Api.Services;

public sealed class TokenGenerator(JwtSettings settings) : ITokenGenerator
{
    private readonly JwtSettings _settings = settings;

    public string GenerateAccessToken(TokenClaims claims)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_settings.SecretKey);

        var claimsList = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, claims.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, claims.Email),
            new(JwtRegisteredClaimNames.Name, claims.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("account_type", claims.AccountType),
            new("tenant_ids", String.Join(",", claims.TenantIds)),
            new("module_accesses", JsonSerializer.Serialize(claims.ModuleAccesses))
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claimsList),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public TokenClaims? ValidateAccessToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_settings.SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = true,
                ValidAudience = _settings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

            var userId = Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
            var accountType = principal.FindFirstValue("account_type")!;
            var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email)!;
            var name = principal.FindFirstValue(JwtRegisteredClaimNames.Name)!;
            var moduleAccessesJson = principal.FindFirstValue("module_accesses")!;
            var moduleAccesses = JsonSerializer.Deserialize<Dictionary<string, int>>(moduleAccessesJson)!;
            var tenantIds = principal.FindFirstValue("tenant_ids")!
                    .Split([','], StringSplitOptions.RemoveEmptyEntries)
                    .Select(s =>
                    {
                        if (Guid.TryParse(s.Trim(), out var id))
                            return (Guid?)id;

                        return null;
                    })
                    .Where(g => g.HasValue)
                    .Select(g => g!.Value)
                    .ToList() ?? [];

            return new TokenClaims(userId, accountType, tenantIds, email, name, moduleAccesses);
        }
        catch
        {
            return null;
        }
    }
}

public sealed class JwtSettings
{
    public string SecretKey { get; init; } = default!;
    public string Issuer { get; init; } = default!;
    public string Audience { get; init; } = default!;
}