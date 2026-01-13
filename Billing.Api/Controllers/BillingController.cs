using Billing.Api.Services;
using Billing.Domain.DTOs.Requests;
using Billing.Domain.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class BillingController(IBillingService billingService, ILogger<BillingController> logger) : ControllerBase
{
    private readonly IBillingService _billingService = billingService;
    private readonly ILogger<BillingController> _logger = logger;

    /// <summary>
    /// Obtém informações de billing de um tenant
    /// </summary>
    [HttpGet("tenant/{tenantId:guid}")]
    [ProducesResponseType(typeof(TenantBillingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantBilling(Guid tenantId, CancellationToken ct)
    {
        var billing = await _billingService.GetTenantBillingAsync(tenantId, ct);

        if (billing is null)
            return NotFound(new { message = "TenantBilling não encontrado" });

        return Ok(billing);
    }

    /// <summary>
    /// Cria informações de billing para um tenant
    /// </summary>
    [HttpPost("tenant")]
    [ProducesResponseType(typeof(TenantBillingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTenantBilling([FromBody] CreateTenantBillingRequest request, CancellationToken ct)
    {
        var billing = await _billingService.CreateTenantBillingAsync(request, ct);
        return CreatedAtAction(nameof(GetTenantBilling), new { tenantId = billing.TenantId }, billing);
    }

    /// <summary>
    /// Atualiza informações de billing de um tenant
    /// </summary>
    [HttpPut("tenant/{tenantId:guid}")]
    [ProducesResponseType(typeof(TenantBillingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBillingInfo(Guid tenantId, [FromBody] UpdateBillingInfoRequest request, CancellationToken ct)
    {
        var billing = await _billingService.UpdateBillingInfoAsync(tenantId, request, ct);
        return Ok(billing);
    }

    /// <summary>
    /// Solicita grace period (5 dias, 1x por ciclo de atraso)
    /// </summary>
    [HttpPost("tenant/{tenantId:guid}/grace-period")]
    [ProducesResponseType(typeof(GracePeriodResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestGracePeriod(Guid tenantId, CancellationToken ct)
    {
        var result = await _billingService.RequestGracePeriodAsync(tenantId, ct);

        if (!result.Granted)
            return BadRequest(new ApiErrorResponse(result.Message!, "GRACE_PERIOD_DENIED", null));

        return Ok(result);
    }
}
