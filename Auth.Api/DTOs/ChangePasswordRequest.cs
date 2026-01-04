namespace Auth.Api.DTOs;

public sealed record ChangePasswordRequest(
    string NewPassword,
    string? CurrentPassword = null);