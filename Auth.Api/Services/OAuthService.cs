using Auth.Domain.Interfaces.Services;
using Google.Apis.Auth;

namespace Auth.Api.Services;

public sealed class OAuthService(OAuthSettings settings, ILogger<OAuthService> logger) : IOAuthService
{
    private readonly OAuthSettings _settings = settings;
    private readonly ILogger<OAuthService> _logger = logger;

    public async Task<OAuthUserInfo?> ValidateGoogleTokenAsync(string idToken, CancellationToken ct = default)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _settings.GoogleClientId }
            });

            return new OAuthUserInfo(
                OAuthId: payload.Subject,
                Email: payload.Email,
                Name: payload.Name,
                EmailVerified: payload.EmailVerified);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Google token");
            return null;
        }
    }
}

public sealed class OAuthSettings
{
    public string GoogleClientId { get; init; } = default!;
}
