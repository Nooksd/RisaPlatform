namespace Auth.Api.DTOs;

public sealed record LoginPublicUserWithOAuthRequest(
    string Module,
    string IdToken);
