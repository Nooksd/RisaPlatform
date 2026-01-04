namespace Auth.Api.DTOs;

public sealed record RefreshTokenInfo(
    Guid Id,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    string? IpAddress,
    string? UserAgent);
