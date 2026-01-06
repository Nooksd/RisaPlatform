namespace Auth.Domain.DTOs;

public sealed class CreateTenantRequest
{
    public string Domain { get; set; } = default!;
    public string Name { get; set; } = default!;
}