namespace Auth.Domain.DTOs;

public sealed record LoginRequest(
    string Email,
    string Password);