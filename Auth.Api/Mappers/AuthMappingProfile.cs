using Auth.Api.DTOs;
using Auth.Domain.Entities;
using AutoMapper;

namespace Auth.Api.Mappers;

public sealed class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        // TenantAccount → UserInfo
        CreateMap<TenantAccount, UserInfo>()
            .ConstructUsing(src => new UserInfo(
                src.Id,
                src.Email.Value,
                src.Name,
                "TenantOwner",
                src.TenantId,
                GetFullModuleAccess()));

        // TenantUser → UserInfo
        CreateMap<TenantUser, UserInfo>()
            .ConstructUsing(src => new UserInfo(
                src.Id,
                src.Email.Value,
                src.Name,
                "TenantUser",
                src.TenantId,
                src.ModuleAccesses.ToDictionary(ma => ma.Module, ma => ma.AccessLevel)));

        // TenantUser → TenantUserResponse
        CreateMap<TenantUser, TenantUserResponse>()
            .ConstructUsing(src => new TenantUserResponse(
                src.Id,
                src.TenantId,
                src.Email.Value,
                src.Name,
                src.IsActive,
                src.CreatedAt,
                src.LastLoginAt,
                src.CreatedBy,
                src.ModuleAccesses.ToDictionary(ma => ma.Module, ma => ma.AccessLevel)));

        // TenantUser → TenantUserDetailResponse
        CreateMap<TenantUser, TenantUserDetailResponse>()
            .ConstructUsing(src => new TenantUserDetailResponse(
                src.Id,
                src.TenantId,
                src.Email.Value,
                src.Name,
                src.IsActive,
                src.CreatedAt,
                src.LastLoginAt,
                src.CreatedBy,
                src.ModuleAccesses.ToDictionary(ma => ma.Module, ma => ma.AccessLevel),
                src.RefreshTokens
                    .Where(rt => !rt.IsRevoked && !rt.IsExpired)
                    .Select(rt => new RefreshTokenInfo(
                        rt.Id,
                        rt.CreatedAt,
                        rt.ExpiresAt,
                        rt.IpAddress,
                        rt.UserAgent))
                    .ToList()));

        // PublicUser → UserInfo
        CreateMap<PublicUser, UserInfo>()
            .ConstructUsing(src => new UserInfo(
                src.Id,
                src.Email.Value,
                src.Name,
                "PublicUser",
                src.TenantId,
                new Dictionary<string, int> { { src.Module, 1 } })); // Public users only have view access to their module

        // RefreshToken → RefreshTokenInfo
        CreateMap<RefreshToken, RefreshTokenInfo>()
            .ConstructUsing(src => new RefreshTokenInfo(
                src.Id,
                src.CreatedAt,
                src.ExpiresAt,
                src.IpAddress,
                src.UserAgent));
    }

    private static Dictionary<string, int> GetFullModuleAccess()
    {
        return Enum.GetNames<Domain.Enums.SystemModule>()
            .ToDictionary(m => m, _ => 3);
    }
}