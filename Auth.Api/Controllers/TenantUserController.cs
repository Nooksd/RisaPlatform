using Auth.Domain.DTOs;
using Auth.Domain.Enums;
using Auth.Domain.Interfaces.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Auth.Api.Controllers;


[Authorize]
[ApiController]
[Route("tenant-users")]
public sealed class TenantUserController(ITenantUserService tenantUserService, IMapper mapper) : ControllerBase
{
    private readonly ITenantUserService _tenantUserService = tenantUserService;
    private readonly IMapper _mapper = mapper;

    [HttpPost]
    public async Task<ActionResult<TenantUserResponse>> Create([FromBody] CreateTenantUserRequest request, CancellationToken ct)
    {
        var (userId, accountType) = GetCurrentUserInfo();

        var result = await _tenantUserService.CreateAsync(userId, accountType, request, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
        }

        var response = _mapper.Map<TenantUserResponse>(result.Value!);

        return CreatedAtAction(nameof(GetDetail), new { id = response!.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TenantUserResponse>> Update([FromRoute] Guid id, [FromBody] UpdateTenantUserRequest request, CancellationToken ct)
    {
        var (userId, accountType) = GetCurrentUserInfo();

        var result = await _tenantUserService.UpdateAsync(id, userId, accountType, request, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
        }

        var response = _mapper.Map<TenantUserResponse>(result.Value!);

        return Ok(response);
    }

    [HttpPost("{id:guid}/change-password")]
    public async Task<IActionResult> ChangePassword([FromRoute] Guid id, [FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var (userId, accountType) = GetCurrentUserInfo();

        var result = await _tenantUserService.ChangePasswordAsync(id, userId, accountType, request.NewPassword, request.CurrentPassword, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
        }

        return Ok(new { message = "Password changed successfully" });
    }

    [HttpPost("{id:guid}/revoke-token")]
    public async Task<IActionResult> RevokeTokens([FromRoute] Guid id, CancellationToken ct)
    {
        var (userId, accountType) = GetCurrentUserInfo();

        var result = await _tenantUserService.RevokeAccessAsync(id, userId, accountType, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
        }

        return Ok(new { message = "Tokens revoked successfully" });
    }

    [HttpGet("list/{tenantId:Guid}")]
    public async Task<ActionResult<TenantUserResponse>> List(Guid tenantId, CancellationToken ct)
    {
        var (userId, accountType) = GetCurrentUserInfo();

        var result = await _tenantUserService.ListAsync(tenantId, userId, accountType, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
        }

        var response = _mapper.Map<IEnumerable<TenantUserResponse>>(result.Value!);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantUserResponse>> GetDetail([FromRoute] Guid id, CancellationToken ct)
    {
        var (userId, accountType) = GetCurrentUserInfo();

        var result = await _tenantUserService.GetDetailAsync(id, userId, accountType, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
        }

        var response = _mapper.Map<TenantUserResponse>(result.Value!);

        return Ok(response);
    }

    private (Guid UserId, AccountType AccountType) GetCurrentUserInfo()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var accountType = Enum.Parse<AccountType>(User.FindFirstValue("account_type")!);
        return (userId, accountType);
    }
}