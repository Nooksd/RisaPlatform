namespace Auth.Api.DTOs;

public sealed record LoginWithOAuthRequest(
    string IdToken,
    string? IpAddress = null,
    string? UserAgent = null);
