namespace Auth.Domain.DTOs;

public sealed record RegisterPublicUserWithOAuthRequest(
    string Module,
    string IdToken);
