namespace Auth.Domain.DTOs;

public sealed record LoginPublicUserWithOAuthRequest(
    string Module,
    Guid TenantId,
    string IdToken);
