namespace Auth.Domain.Interfaces.Services;

public interface IOAuthService
{
    Task<OAuthUserInfo?> ValidateGoogleTokenAsync(string idToken, CancellationToken ct = default);
}

public record OAuthUserInfo(
    string OAuthId,
    string Email,
    string Name,
    bool EmailVerified);
