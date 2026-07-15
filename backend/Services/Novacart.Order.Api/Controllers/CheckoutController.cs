using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novacart.Api.Models.Dtos.Payments;
using Novacart.Api.Services;
using Novacart.Api.Services.Payments;

namespace Novacart.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CheckoutController : ControllerBase
{
    private readonly IPaymentService _payment;

    public CheckoutController(IPaymentService payment) => _payment = payment;

    private Guid GetUserId()
    {
        var raw = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : throw new UnauthorizedAccessException();
    }

    /// <summary>Create a checkout session and get the redirection URL.</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(CheckoutResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateCheckout([FromBody] CheckoutRequest request)
    {
        var result = await _payment.ProcessCheckoutAsync(GetUserId(), request, "stripe");
        return Ok(result);
    }

    /// <summary>Webhook listener for Stripe checkout events.</summary>
    /// <remarks>
    /// Signature/processing failures are surfaced via the global exception handler;
    /// Stripe receives a non-2xx and retries the event.
    /// </remarks>
    [HttpPost("webhook/stripe")]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();
        await _payment.HandleWebhookAsync("stripe", json, signature);
        return Ok();
    }
}
