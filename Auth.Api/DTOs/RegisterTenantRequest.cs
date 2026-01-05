namespace Auth.Api.DTOs;

public sealed record RegisterTenantRequest(
    string Email,
    string Password,
    string Name);
