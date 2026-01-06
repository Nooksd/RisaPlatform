namespace Auth.Domain.DTOs;

public sealed record RegisterTenantRequest(
    string Email,
    string Password,
    string Name);
