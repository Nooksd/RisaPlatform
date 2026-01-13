using Billing.Api.Services;
using Billing.Domain.DTOs.Requests;
using Billing.Domain.DTOs.Responses;
using Billing.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PaymentsController(
    IBillingService billingService,
    IPaymentRepository paymentRepository,
    ILogger<PaymentsController> logger) : ControllerBase
{
    private readonly IBillingService _billingService = billingService;
    private readonly IPaymentRepository _paymentRepository = paymentRepository;
    private readonly ILogger<PaymentsController> _logger = logger;

    /// <summary>
    /// Cria um novo pagamento (PIX, Boleto ou Cartão)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request, CancellationToken ct)
    {
        var payment = await _billingService.CreatePaymentAsync(request, ct);
        return CreatedAtAction(nameof(GetPayment), new { paymentId = payment.Id }, payment);
    }

    /// <summary>
    /// Obtém um pagamento por ID
    /// </summary>
    [HttpGet("{paymentId:guid}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayment(Guid paymentId, CancellationToken ct)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, ct);

        if (payment is null)
            return NotFound(new { message = "Pagamento não encontrado" });

        return Ok(new PaymentResponse(
            payment.Id,
            payment.GatewayPaymentId,
            payment.Method,
            payment.Status,
            payment.Amount,
            payment.CreatedAt,
            payment.DueDate,
            payment.ConfirmedAt,
            payment.PixCopyPaste,
            payment.PixQrCode,
            payment.PixExpiresAt,
            payment.BoletoUrl,
            payment.BoletoBarcode,
            payment.CardLastFour,
            payment.CardBrand));
    }

    /// <summary>
    /// Lista pagamentos de um tenant
    /// </summary>
    [HttpGet("tenant/{tenantId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<PaymentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenantPayments(Guid tenantId, CancellationToken ct)
    {
        var tenantBillingRepo = HttpContext.RequestServices.GetRequiredService<ITenantBillingRepository>();
        var billing = await tenantBillingRepo.GetByTenantIdAsync(tenantId, ct);

        if (billing is null)
            return Ok(Array.Empty<PaymentResponse>());

        var payments = await _paymentRepository.GetByTenantBillingIdAsync(billing.Id, ct);

        var response = payments.Select(p => new PaymentResponse(
            p.Id,
            p.GatewayPaymentId,
            p.Method,
            p.Status,
            p.Amount,
            p.CreatedAt,
            p.DueDate,
            p.ConfirmedAt,
            p.PixCopyPaste,
            p.PixQrCode,
            p.PixExpiresAt,
            p.BoletoUrl,
            p.BoletoBarcode,
            p.CardLastFour,
            p.CardBrand));

        return Ok(response);
    }
}
