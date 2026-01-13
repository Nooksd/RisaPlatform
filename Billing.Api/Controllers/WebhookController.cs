using Billing.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace Billing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class WebhookController(
    IBillingService billingService,
    StripeSettings stripeSettings,
    ILogger<WebhookController> logger) : ControllerBase
{
    private readonly IBillingService _billingService = billingService;
    private readonly StripeSettings _stripeSettings = stripeSettings;
    private readonly ILogger<WebhookController> _logger = logger;

    /// <summary>
    /// Recebe webhooks do Stripe
    /// </summary>
    [HttpPost("stripe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StripeWebhook(CancellationToken ct)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(ct);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                signature,
                _stripeSettings.WebhookSecret);

            _logger.LogInformation(
                "Received Stripe webhook: Type={EventType}, Id={EventId}",
                stripeEvent.Type, stripeEvent.Id);

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                if (session != null)
                {
                    await _billingService.ProcessWebhookAsync(
                        session.Id,
                        session.PaymentStatus == "paid" ? "CONFIRMED" : "PENDING",
                        ct);
                }
            }
            else if (stripeEvent.Type == "checkout.session.expired")
            {
                var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                if (session != null)
                {
                    await _billingService.ProcessWebhookAsync(session.Id, "EXPIRED", ct);
                }
            }
            else if (stripeEvent.Type == "payment_intent.payment_failed")
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                if (paymentIntent != null)
                {
                    _logger.LogWarning("Payment failed: {PaymentIntentId}", paymentIntent.Id);
                }
            }

            return Ok(new { received = true });
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Invalid Stripe webhook signature");
            return BadRequest(new { error = "Invalid signature" });
        }
    }

    /// <summary>
    /// Health check do webhook
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
