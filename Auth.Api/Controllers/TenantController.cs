using Auth.Domain.DTOs;
using Auth.Domain.Enums;
using Auth.Domain.Interfaces.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/tenants")]
public sealed class TenantController(ITenantService tenantService, IMapper mapper) : ControllerBase
{
    private readonly ITenantService _tenantService = tenantService;
    private readonly IMapper _mapper = mapper;

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<TenantResponse>> Create([FromBody] CreateTenantRequest request, CancellationToken ct)
    {
        var ownerId = GetCurrentUserId();
        var accountType = GetCurrentAccountType();

        if (accountType != AccountType.TenantOwner)
            return Forbid();

        var result = await _tenantService.CreateAsync(ownerId, request, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
        }

        var response = _mapper.Map<TenantResponse>(result.Value!);

        return CreatedAtAction(nameof(GetDetail), new { id = response.Id }, response);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TenantResponse>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateTenantRequest request,
        CancellationToken ct)
    {
        var ownerId = GetCurrentUserId();
        var accountType = GetCurrentAccountType();

        if (accountType != AccountType.TenantOwner)
            return Forbid();

        var result = await _tenantService.UpdateAsync(id, ownerId, request, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
        }

        var response = _mapper.Map<TenantResponse>(result.Value!);

        return Ok(response);
    }

    [HttpGet("domain/{domain}")]
    public async Task<ActionResult<TenantDomainResponse>> GetByDomain(
        [FromRoute] string domain,
        CancellationToken ct)
    {
        var result = await _tenantService.GetByDomainAsync(domain, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
        }

        var response = _mapper.Map<TenantDomainResponse>(result.Value!);

        return Ok(response);
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TenantResponse>>> List(CancellationToken ct)
    {
        var ownerId = GetCurrentUserId();
        var accountType = GetCurrentAccountType();

        if (accountType != AccountType.TenantOwner)
            return Forbid();

        var result = await _tenantService.ListByOwnerAsync(ownerId, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
        }

        var response = _mapper.Map<IEnumerable<TenantResponse>>(result.Value!);

        return Ok(response);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantDetailResponse>> GetDetail(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var requesterId = GetCurrentUserId();

        var result = await _tenantService.GetByIdAsync(id, requesterId, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.Error!.Code, message = result.Error.Message });
        }

        var response = _mapper.Map<TenantDetailResponse>(result.Value!);

        return Ok(response);
    }

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    private AccountType GetCurrentAccountType()
    {
        return Enum.Parse<AccountType>(User.FindFirstValue("account_type")!);
    }
}