namespace Auth.Api.DTOs;

public sealed record LoginRequest(
    string Email,
    string Password,
    string? IpAddress = null,
    string? UserAgent = null);