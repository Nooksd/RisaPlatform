namespace Auth.Domain.DTOs;

public sealed record RegisterPublicUserWithOAuthRequest(
    string Module,
    Guid TenantId,
    string IdToken);
