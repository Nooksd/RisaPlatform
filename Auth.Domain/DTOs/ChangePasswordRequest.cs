namespace Auth.Domain.DTOs;

public sealed record ChangePasswordRequest(
    string NewPassword,
    string? CurrentPassword = null);