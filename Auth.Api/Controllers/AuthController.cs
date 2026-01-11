using Auth.Api.Settings;
using Auth.Domain.DTOs;
using Auth.Domain.Enums;
using Auth.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Json;

namespace Auth.Api.Controllers;

[ApiController]
public sealed class AuthController(
    IAuthService authService,
    IOptions<CookieSettings> cookieSettings) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly CookieSettings _cookieSettings = cookieSettings.Value;

    // ===== TENANT ACCOUNT =====

    [HttpPost("register/tenant-account")]
    public async Task<IActionResult> RegisterTenant([FromBody] RegisterTenantRequest request, CancellationToken ct)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authService.RegisterTenantAsync(request, ipAddress!, userAgent, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
        }

        SetAuthCookies(result.Value!);
        return Ok(result.Value);
    }

    [HttpPost("register/oauth/tenant-account")]
    public async Task<IActionResult> RegisterTenantWithOAuth([FromBody] RegisterTenantWithOAuthRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterTenantWithOAuthAsync(request, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
        }

        SetAuthCookies(result.Value!);
        return Ok(result.Value);
    }

    [HttpPost("login/tenant-account")]
    public async Task<IActionResult> LoginTenant([FromBody] LoginRequest request, CancellationToken ct)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginTenantAsync(request, ipAddress!, userAgent, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
        }

        SetAuthCookies(result.Value!);
        return Ok(result.Value);
    }

    [HttpPost("login/oauth/tenant-account")]
    public async Task<IActionResult> LoginTenantWithOAuth([FromBody] LoginWithOAuthRequest request, CancellationToken ct)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginTenantWithOAuthAsync(request, ipAddress!, userAgent, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
        }

        SetAuthCookies(result.Value!);
        return Ok(result.Value);
    }

    // ===== TENANT USER =====

    [HttpPost("login/tenant-user")]
    public async Task<IActionResult> LoginTenantUser([FromBody] TenantUserLoginRequest request, CancellationToken ct)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginTenantUserAsync(request, ipAddress!, userAgent, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
        }

        SetAuthCookies(result.Value!);
        return Ok(result.Value);
    }

    // ===== PUBLIC USER =====

    [HttpPost("register/public-user")]
    public async Task<IActionResult> RegisterPublicUser([FromBody] RegisterPublicUserRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterPublicUserAsync(request.TenantId, request, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
        }

        SetAuthCookies(result.Value!);
        return Ok(result.Value);
    }

    [HttpPost("register/oauth/public-user")]
    public async Task<IActionResult> RegisterPublicUserWithOAuth([FromBody] RegisterPublicUserWithOAuthRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterPublicUserWithOAuthAsync(request.TenantId, request, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
        }

        SetAuthCookies(result.Value!);
        return Ok(result.Value);
    }

    [HttpPost("login/public-user")]
    public async Task<IActionResult> LoginPublicUser([FromBody] LoginPublicUserRequest request, CancellationToken ct)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginPublicUserAsync(request.TenantId, request, ipAddress!, userAgent, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
        }

        SetAuthCookies(result.Value!);
        return Ok(result.Value);
    }

    [HttpPost("login/oauth/public-user")]
    public async Task<IActionResult> LoginPublicUserWithOAuth([FromBody] LoginPublicUserWithOAuthRequest request, CancellationToken ct)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginPublicUserWithOAuthAsync(request.TenantId, request, ipAddress!, userAgent, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
        }

        SetAuthCookies(result.Value!);
        return Ok(result.Value);
    }

    // ===== COMMON =====

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[_cookieSettings.RefreshTokenCookieName];

        if (string.IsNullOrEmpty(refreshToken))
        {
            var request = await HttpContext.Request.ReadFromJsonAsync<RefreshTokenRequest>();
            refreshToken = request?.RefreshToken;
        }

        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(new { error = "REFRESH_TOKEN_MISSING", message = "Refresh token is required" });
        }

        var result = await _authService.RefreshTokenAsync(new RefreshTokenRequest(refreshToken), ct);

        if (!result.IsSuccess)
        {
            ClearAuthCookies();
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
        }

        SetAuthCookies(result.Value!);
        return Ok(result.Value);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var accountType = Enum.Parse<AccountType>(User.FindFirstValue("account_type")!);

        var refreshToken = Request.Cookies[_cookieSettings.RefreshTokenCookieName];

        if (string.IsNullOrEmpty(refreshToken))
        {
            var request = await HttpContext.Request.ReadFromJsonAsync<RefreshTokenRequest>(cancellationToken: ct);
            refreshToken = request?.RefreshToken;
        }

        if (string.IsNullOrEmpty(refreshToken))
        {
            ClearAuthCookies();
            return Ok(new { message = "Logged out successfully" });
        }

        var result = await _authService.LogoutAsync(userId, accountType, refreshToken, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
        }

        ClearAuthCookies();
        return Ok(new { message = "Logged out successfully" });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var name = User.FindFirstValue("name");
        var accountType = User.FindFirstValue("account_type");
        var tenantIdsClaim = User.FindFirstValue("tenant_ids");
        var moduleAccess = JsonSerializer.Deserialize<Dictionary<string, int>>(User.FindFirstValue("module_accesses")!);
        var tenantIds = tenantIdsClaim?
                .Split([','], StringSplitOptions.RemoveEmptyEntries)
                .Select(s =>
                {
                    if (Guid.TryParse(s.Trim(), out var id))
                        return (Guid?)id;
                    return null;
                })
                .Where(g => g.HasValue)
                .Select(g => g!.Value)
                .ToList() ?? [];

        return Ok(new
        {
            id = userId,
            email,
            name,
            accountType,
            tenantIds,
            moduleAccess
        });
    }

    private void SetAuthCookies(AuthResponse authResponse)
    {
        Response.Cookies.Append(
            _cookieSettings.AccessTokenCookieName,
            authResponse.AccessToken,
            _cookieSettings.GetAccessTokenCookieOptions());

        Response.Cookies.Append(
            _cookieSettings.RefreshTokenCookieName,
            authResponse.RefreshToken,
            _cookieSettings.GetRefreshTokenCookieOptions());
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete(_cookieSettings.AccessTokenCookieName);
        Response.Cookies.Delete(_cookieSettings.RefreshTokenCookieName);
    }
}