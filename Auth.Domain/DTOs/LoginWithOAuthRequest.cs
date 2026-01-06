namespace Auth.Domain.DTOs;

public sealed record LoginWithOAuthRequest(
    string IdToken);
