using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Novacart.Api.Models.Dtos.Support;
using Novacart.Api.Services;

namespace Novacart.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SupportController : ControllerBase
{
    private readonly IChatSupportService _chat;

    public SupportController(IChatSupportService chat) => _chat = chat;

    [HttpPost("chat")]
    [AllowAnonymous]
    [EnableRateLimiting("chat")]
    [ProducesResponseType(typeof(SendChatMessageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendChat([FromBody] SendChatMessageRequest request, CancellationToken cancellationToken)
        => Ok(await _chat.SendMessageAsync(TryGetUserId(), request, cancellationToken));

    [HttpGet("faq")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<SupportFaqItemDto>), StatusCodes.Status200OK)]
    public IActionResult GetFaq([FromQuery] string locale = "en")
        => Ok(_chat.GetFaq(locale));

    private Guid? TryGetUserId()
    {
        if (User.Identity?.IsAuthenticated != true) return null;
        var raw = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
