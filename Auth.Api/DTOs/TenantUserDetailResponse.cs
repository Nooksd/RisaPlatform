namespace Auth.Api.DTOs;

public sealed record TenantUserDetailResponse(
    Guid Id,
    Guid TenantId,
    string Email,
    string Name,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    Guid CreatedBy,
    Dictionary<string, int> ModuleAccesses
    );

