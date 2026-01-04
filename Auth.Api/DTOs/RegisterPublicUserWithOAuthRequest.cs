namespace Auth.Api.DTOs;

public sealed record RegisterPublicUserWithOAuthRequest(
    string Module,
    string IdToken);
