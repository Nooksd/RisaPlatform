namespace Auth.Domain.Interfaces.Services;

public interface ITokenGenerator
{
    string GenerateAccessToken(TokenClaims claims);
    string GenerateRefreshToken();
    TokenClaims? ValidateAccessToken(string token);
}

public record TokenClaims(
    Guid UserId,
    string AccountType,
    List<Guid> TenantIds,
    string Email,
    string Name,
    Dictionary<string, int> ModuleAccesses);