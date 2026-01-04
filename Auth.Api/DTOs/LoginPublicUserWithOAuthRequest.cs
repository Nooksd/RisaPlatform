namespace Auth.Api.DTOs;

public sealed record LoginPublicUserWithOAuthRequest(
    string Module,
    string IdToken,
    string? IpAddress = null,
    string? UserAgent = null);
