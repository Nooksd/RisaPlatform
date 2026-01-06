namespace Auth.Domain.DTOs;

public sealed record LoginPublicUserWithOAuthRequest(
    string Module,
    string IdToken);
