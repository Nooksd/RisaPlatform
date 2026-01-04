using Auth.Api.DTOs;
using Auth.Api.Services;
using Auth.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Auth.Api.Controllers;


[Authorize]
[ApiController]
[Route("api/tenant-users")]
public sealed class TenantUserController(ITenantUserService tenantUserService) : ControllerBase
{
    private readonly ITenantUserService _tenantUserService = tenantUserService;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantUserRequest request, CancellationToken ct)
    {
        var (userId, accountType) = GetCurrentUserInfo();

        var result = await _tenantUserService.CreateAsync(userId, accountType, request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetDetail), new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateTenantUserRequest request, CancellationToken ct)
    {
        var (userId, accountType) = GetCurrentUserInfo();

        var result = await _tenantUserService.UpdateAsync(id, userId, accountType, request, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
    }

    [HttpPost("{id:guid}/change-password")]
    public async Task<IActionResult> ChangePassword([FromRoute] Guid id, [FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var (userId, accountType) = GetCurrentUserInfo();

        var result = await _tenantUserService.ChangePasswordAsync(id, userId, accountType, request, ct);
        return result.IsSuccess
            ? Ok(new { message = "Password changed successfully" })
            : BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
    }

    [HttpPost("{id:guid}/revoke-tokens")]
    public async Task<IActionResult> RevokeTokens([FromRoute] Guid id, CancellationToken ct)
    {
        var (userId, accountType) = GetCurrentUserInfo();

        var result = await _tenantUserService.RevokeTokensAsync(id, userId, accountType, ct);
        return result.IsSuccess
            ? Ok(new { message = "Tokens revoked successfully" })
            : BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var (userId, accountType) = GetCurrentUserInfo();
        var tenantId = Guid.Parse(User.FindFirstValue("tenant_id")!);

        var result = await _tenantUserService.ListAsync(tenantId, userId, accountType, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail([FromRoute] Guid id, CancellationToken ct)
    {
        var (userId, accountType) = GetCurrentUserInfo();

        var result = await _tenantUserService.GetDetailAsync(id, userId, accountType, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
    }

    private (Guid UserId, AccountType AccountType) GetCurrentUserInfo()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var accountType = Enum.Parse<AccountType>(User.FindFirstValue("account_type")!);
        return (userId, accountType);
    }
}