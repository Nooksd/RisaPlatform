namespace Auth.Domain.DTOs;

public sealed record RegisterPublicUserRequest(
    string Module,
    string Email,
    string Password,
    string Name);

