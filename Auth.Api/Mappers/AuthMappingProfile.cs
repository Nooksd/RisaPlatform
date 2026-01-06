using Auth.Domain.DTOs;
using Auth.Domain.Entities;
using AutoMapper;

namespace Auth.Api.Mappers;

public sealed class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        CreateMap<TenantAccount, UserInfo>()
            .ConstructUsing(src => new UserInfo(
                src.Id,
                src.Email.Value,
                src.Name,
                null,
                "TenantOwner",
                src.Tenants.Select(tnt => tnt.Id).ToList(),
                GetFullModuleAccess()))
            .ForAllMembers(opt => opt.Ignore());

        CreateMap<TenantUser, UserInfo>()
            .ConstructUsing(src => new UserInfo(
                src.Id,
                src.Email.Value,
                src.Name,
                src.Username,
                "TenantUser",
                new List<Guid> { src.TenantId },
                src.ModuleAccesses.ToDictionary(ma => ma.Module, ma => ma.AccessLevel)))
            .ForAllMembers(opt => opt.Ignore());

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
                src.ModuleAccesses.ToDictionary(ma => ma.Module, ma => ma.AccessLevel)))
            .ForAllMembers(opt => opt.Ignore());

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
                src.ModuleAccesses.ToDictionary(ma => ma.Module, ma => ma.AccessLevel)))
            .ForAllMembers(opt => opt.Ignore());


        CreateMap<PublicUser, UserInfo>()
            .ConstructUsing(src => new UserInfo(
                src.Id,
                src.Email.Value,
                src.Name,
                null,
                "PublicUser",
                new List<Guid> { src.TenantId },
                new Dictionary<string, int> { { src.Module, 0 } }))
            .ForAllMembers(opt => opt.Ignore());

        CreateMap<RefreshToken, RefreshTokenInfo>()
            .ConstructUsing(src => new RefreshTokenInfo(
                src.Id,
                src.CreatedAt,
                src.ExpiresAt,
                src.IpAddress,
                src.UserAgent))
            .ForAllMembers(opt => opt.Ignore());

        CreateMap<Tenant, TenantResponse>()
            .ConstructUsing(src => new TenantResponse(
                src.Id,
                src.Domain.Value,
                src.Name,
                src.IsActive,
                src.CreatedBy,
                src.CreatedAt,
                src.UpdatedAt,
                src.Users.Count))
            .ForAllMembers(opt => opt.Ignore());

        CreateMap<Tenant, TenantDetailResponse>()
            .ConstructUsing(src => new TenantDetailResponse(
                src.Id,
                src.Domain.Value,
                src.Name,
                src.IsActive,
                src.CreatedBy,
                src.CreatedAt,
                src.UpdatedAt,
                src.Users.Count))
            .ForAllMembers(opt => opt.Ignore());

        CreateMap<Tenant, TenantDomainResponse>()
            .ConstructUsing(src => new TenantDomainResponse(
                src.Id,
                src.Domain.Value))
            .ForAllMembers(opt => opt.Ignore());
    }

    private static Dictionary<string, int> GetFullModuleAccess()
    {
        return Enum.GetNames<Domain.Enums.SystemModule>()
            .ToDictionary(m => m, _ => 3);
    }
}