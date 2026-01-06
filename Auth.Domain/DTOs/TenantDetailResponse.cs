using System.Text.Json.Serialization;

namespace Auth.Domain.DTOs;

public sealed class TenantDetailResponse
{
    public Guid Id { get; set; }
    public string Domain { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int UserCount { get; set; }

    [JsonConstructor]
    public TenantDetailResponse(
        Guid id,
        string domain,
        string name,
        bool isActive,
        Guid createdBy,
        DateTime createdAt,
        DateTime? updatedAt,
        int userCount)
    {
        Id = id;
        Domain = domain;
        Name = name;
        IsActive = isActive;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        UserCount = userCount;
    }
}