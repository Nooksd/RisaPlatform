using Auth.Api.DTOs;
using Auth.Api.Services;
using Auth.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    // ===== TENANT ACCOUNT =====

    [HttpPost("tenant/register")]
    public async Task<IActionResult> RegisterTenant([FromBody] RegisterTenantRequest request, CancellationToken ct)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authService.RegisterTenantAsync(request, ipAddress!, userAgent, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
    }

    [HttpPost("tenant/register/oauth")]
    public async Task<IActionResult> RegisterTenantWithOAuth([FromBody] RegisterTenantWithOAuthRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterTenantWithOAuthAsync(request, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
    }

    [HttpPost("tenant/login")]
    public async Task<IActionResult> LoginTenant([FromBody] LoginRequest request, CancellationToken ct)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginTenantAsync(request, ipAddress!, userAgent, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
    }

    [HttpPost("tenant/login/oauth")]
    public async Task<IActionResult> LoginTenantWithOAuth([FromBody] LoginWithOAuthRequest request, CancellationToken ct)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginTenantWithOAuthAsync(request, ipAddress!, userAgent, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
    }

    // ===== PUBLIC USER =====

    [HttpPost("{tenantId:guid}/public/register")]
    public async Task<IActionResult> RegisterPublicUser([FromRoute] Guid tenantId, [FromBody] RegisterPublicUserRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterPublicUserAsync(tenantId, request, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
    }

    [HttpPost("{tenantId:guid}/public/register/oauth")]
    public async Task<IActionResult> RegisterPublicUserWithOAuth([FromRoute] Guid tenantId, [FromBody] RegisterPublicUserWithOAuthRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterPublicUserWithOAuthAsync(tenantId, request, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
    }

    [HttpPost("{tenantId:guid}/public/login")]
    public async Task<IActionResult> LoginPublicUser([FromRoute] Guid tenantId, [FromBody] LoginPublicUserRequest request, CancellationToken ct)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginPublicUserAsync(tenantId, request, ipAddress!, userAgent, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
    }

    [HttpPost("{tenantId:guid}/public/login/oauth")]
    public async Task<IActionResult> LoginPublicUserWithOAuth([FromRoute] Guid tenantId, [FromBody] LoginPublicUserWithOAuthRequest request, CancellationToken ct)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginPublicUserWithOAuthAsync(tenantId, request, ipAddress!, userAgent, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
    }

    // ===== COMMON =====

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _authService.RefreshTokenAsync(request, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var accountType = Enum.Parse<AccountType>(User.FindFirstValue("account_type")!);

        var result = await _authService.LogoutAsync(userId, accountType, request.RefreshToken, ct);
        return result.IsSuccess
            ? Ok(new { message = "Logged out successfully" })
            : BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var name = User.FindFirstValue("name");
        var accountType = User.FindFirstValue("account_type");
        var tenantId = User.FindFirstValue("tenant_id");

        return Ok(new
        {
            id = userId,
            email,
            name,
            accountType,
            tenantId
        });
    }
}