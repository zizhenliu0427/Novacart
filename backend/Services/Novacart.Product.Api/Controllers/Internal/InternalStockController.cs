using Microsoft.AspNetCore.Mvc;
using Novacart.Api.Models.Dtos.Stock;
using Novacart.Api.Services.Stock;

namespace Novacart.Api.Controllers.Internal;

[ApiController]
[Route("api/internal/stock")]
[ApiExplorerSettings(IgnoreApi = true)]
public class InternalStockController(IStockHoldService holds) : ControllerBase
{
    [HttpPost("hold")]
    [ProducesResponseType(typeof(StockHoldResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<StockHoldResponse>> Hold(
        [FromBody] StockHoldRequest request,
        CancellationToken cancellationToken)
    {
        var outcome = await holds.TryHoldForOrderAsync(
            request.OrderId,
            request.Lines,
            cancellationToken);

        return outcome switch
        {
            StockHoldOutcome.Held or StockHoldOutcome.AlreadyHeld => Ok(new StockHoldResponse(true)),
            StockHoldOutcome.InsufficientStock => Ok(new StockHoldResponse(false, "Insufficient stock for one or more items.")),
            StockHoldOutcome.LockNotAcquired => Ok(new StockHoldResponse(false, "Inventory is busy; please retry.")),
            _ => Ok(new StockHoldResponse(false, "Unable to reserve stock.")),
        };
    }

    [HttpPost("release")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Release(
        [FromBody] StockReleaseRequest request,
        CancellationToken cancellationToken)
    {
        await holds.ReleaseForOrderAsync(request.OrderId, cancellationToken);
        return NoContent();
    }
}
