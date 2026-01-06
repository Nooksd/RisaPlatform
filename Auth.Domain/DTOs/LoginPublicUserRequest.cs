namespace Auth.Domain.DTOs;

public sealed record LoginPublicUserRequest(
    string Module,
    string Email,
    string Password);