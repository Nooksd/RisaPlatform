namespace Auth.Api.DTOs;

public sealed record LoginPublicUserRequest(
    string Module,
    string Email,
    string Password,
    string? IpAddress = null,
    string? UserAgent = null);