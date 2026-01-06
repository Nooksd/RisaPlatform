namespace Auth.Domain.DTOs;

public sealed class UpdateTenantRequest
{
    public string? Domain { get; set; }
    public string? Name { get; set; }
}