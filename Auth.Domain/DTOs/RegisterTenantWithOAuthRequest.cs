namespace Auth.Domain.DTOs;

public sealed record RegisterTenantWithOAuthRequest(
    string IdToken);
