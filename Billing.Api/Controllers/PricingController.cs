using Billing.Domain.DTOs.Requests;
using Billing.Domain.DTOs.Responses;
using Billing.Domain.Enums;
using Billing.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PricingController(IPricingService pricingService, ILogger<PricingController> logger) : ControllerBase
{
    private readonly IPricingService _pricingService = pricingService;
    private readonly ILogger<PricingController> _logger = logger;

    /// <summary>
    /// Calcula o preço de uma assinatura sem criar pagamento
    /// </summary>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(PricingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CalculatePrice([FromBody] CalculatePriceRequest request, CancellationToken ct)
    {
        var result = await _pricingService.CalculatePriceAsync(
            new PricingRequest(request.ModuleCodes, request.UserCount, request.Duration),
            ct);

        if (!result.Success)
        {
            return BadRequest(new ApiErrorResponse(result.ErrorMessage!, "PRICING_ERROR", null));
        }

        var response = new PricingResponse(
            result.SubtotalBeforeDiscounts,
            result.QuantityDiscountTotal,
            result.DurationDiscountTotal,
            result.FinalTotal,
            request.Duration.GetDiscountPercentage(),
            result.ModuleDetails.Select(d => new ModulePricingResponse(
                d.ModuleCode,
                d.ModuleName,
                d.PricePerUser,
                d.QuantityDiscountPercentage,
                d.PricePerUserAfterQuantityDiscount,
                d.TotalBeforeTimeDiscount)));

        return Ok(response);
    }

    /// <summary>
    /// Retorna informações sobre os descontos disponíveis
    /// </summary>
    [HttpGet("discounts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetDiscountInfo()
    {
        return Ok(new
        {
            DurationDiscounts = new[]
            {
                new { Duration = "Monthly", Months = 1, DiscountPercentage = 0.0m, Description = "Mensal - Sem desconto" },
                new { Duration = "Quarterly", Months = 3, DiscountPercentage = 0.25m, Description = "Trimestral - 25% de desconto" },
                new { Duration = "Yearly", Months = 12, DiscountPercentage = 0.40m, Description = "Anual - 40% de desconto" }
            },
            QuantityDiscounts = new[]
            {
                new { MinUsers = 10, DiscountPercentage = 0.05m, Description = "10+ usuários - 5% de desconto" },
                new { MinUsers = 25, DiscountPercentage = 0.10m, Description = "25+ usuários - 10% de desconto" },
                new { MinUsers = 50, DiscountPercentage = 0.15m, Description = "50+ usuários - 15% de desconto" },
                new { MinUsers = 100, DiscountPercentage = 0.20m, Description = "100+ usuários - 20% de desconto" }
            }
        });
    }
}
