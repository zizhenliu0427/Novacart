using Microsoft.EntityFrameworkCore;
using Stripe;
using Novacart.Api.Data;
using Novacart.Api.Models.Entities;
using Novacart.Api.Models.Dtos.Payments;

namespace Novacart.Api.Services.Payments;

public interface IPaymentService
{
    Task<CheckoutResponseDto> ProcessCheckoutAsync(Guid userId, CheckoutRequest request, string gateway);
    Task HandleWebhookAsync(string gateway, string json, string signature);
}

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly IEnumerable<IPaymentStrategy> _strategies;
    private readonly IRedisCacheService _cache;
    private readonly IEmailService _email;
    private readonly ILogger<PaymentService> _logger;
    private readonly string _stripeWebhookSecret;

    public PaymentService(
        AppDbContext db,
        IEnumerable<IPaymentStrategy> strategies,
        IRedisCacheService cache,
        IConfiguration config,
        IEmailService email,
        ILogger<PaymentService> logger)
    {
        _db = db;
        _strategies = strategies;
        _cache = cache;
        _email = email;
        _logger = logger;
        _stripeWebhookSecret = config["Stripe:WebhookSecret"] ?? string.Empty;
    }

    public async Task<CheckoutResponseDto> ProcessCheckoutAsync(Guid userId, CheckoutRequest request, string gateway)
    {
        // 1. Retrieve cart
        var cart = await _db.Carts
            .Include(c => c.Items).ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null || !cart.Items.Any())
            throw new AppException("Your cart is empty.", StatusCodes.Status409Conflict);

        // 2. Validate stock for all items
        foreach (var item in cart.Items)
        {
            if (!item.Product.IsActive)
                throw new AppException($"Product '{item.Product.Name}' is no longer active.", StatusCodes.Status410Gone);

            if (item.Product.StockQuantity < item.Quantity)
                throw new AppException($"Product '{item.Product.Name}' has insufficient stock (Only {item.Product.StockQuantity} unit(s) available).", StatusCodes.Status422UnprocessableEntity);
        }

        // 3. Create Order & OrderItems (snapshotted)
        var subtotal = cart.Items.Sum(ci => ProductService.ResolvePrice(ci.Product) * ci.Quantity);
        var shipping = subtotal >= 100.00m ? 0.00m : 10.00m; // free shipping over $100
        var tax = Math.Round((subtotal + shipping) * 0.10m, 2); // 10% GST (inclusive for display, or exclusive. Let's make it exclusive)
        var total = subtotal + shipping + tax;

        var orderNumber = $"NC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant()}";

        var orderItems = cart.Items.Select(ci => new OrderItem
        {
            ProductId = ci.ProductId,
            ProductNameSnapshot = ci.Product.Name,
            PriceAtPurchase = ProductService.ResolvePrice(ci.Product),
            Quantity = ci.Quantity
        }).ToList();

        // Fetch selected shipping address & user info for snapshot
        var address = await _db.UserAddresses
            .FirstOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == userId)
            ?? throw AppException.NotFound("Shipping address");

        var user = await _db.Users.FindAsync(userId) 
            ?? throw AppException.NotFound("User");

        var order = new Order
        {
            UserId = userId,
            OrderNumber = orderNumber,
            Subtotal = subtotal,
            ShippingCost = shipping,
            Tax = tax,
            Total = total,
            Currency = "AUD",
            CurrentStatus = OrderStatuses.Pending,
            ShippingName = user.FullName,
            ShippingLine1 = address.Line1,
            ShippingLine2 = address.Line2,
            ShippingCity = address.City,
            ShippingState = address.State,
            ShippingPostcode = address.Postcode,
            ShippingCountry = address.Country,
            Items = orderItems
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // 4. Resolve payment gateway strategy
        var strategy = _strategies.FirstOrDefault(s => s.Code == gateway.ToLowerInvariant())
            ?? throw new AppException($"Payment gateway '{gateway}' is not supported.", StatusCodes.Status400BadRequest);

        // 5. Generate Session URL
        var sessionResult = await strategy.CreateCheckoutSessionAsync(order, request.SuccessUrl, request.CancelUrl);

        // 6. Record transaction details as pending
        var paymentMethod = await _db.PaymentMethods.FirstOrDefaultAsync(pm => pm.Code == strategy.Code)
            ?? throw new AppException($"Payment method '{strategy.Code}' registry entry not found.", StatusCodes.Status500InternalServerError);

        var payment = new Payment
        {
            OrderId = order.Id,
            PaymentMethodId = paymentMethod.Id,
            ProviderTransactionId = sessionResult.ProviderTransactionId,
            Amount = order.Total,
            Currency = order.Currency,
            Status = PaymentStatuses.Pending
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        return new CheckoutResponseDto
        {
            RedirectUrl = sessionResult.RedirectUrl,
            SessionId = sessionResult.ProviderTransactionId
        };
    }

    public async Task HandleWebhookAsync(string gateway, string json, string signature)
    {
        if (gateway.ToLowerInvariant() == "stripe")
        {
            await ProcessStripeWebhookAsync(json, signature);
        }
        else
        {
            throw new AppException($"Webhook handler for '{gateway}' is not supported.", StatusCodes.Status400BadRequest);
        }
    }

    private async Task ProcessStripeWebhookAsync(string json, string signature)
    {
        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signature, _stripeWebhookSecret);
        }
        catch (StripeException ex)
        {
            throw new AppException($"Stripe signature verification failed: {ex.Message}", StatusCodes.Status400BadRequest);
        }

        // 1. Log event idempotently to avoid duplicate handling
        var paymentMethod = await _db.PaymentMethods.FirstAsync(pm => pm.Code == "stripe");
        var webhookLog = new PaymentWebhook
        {
            PaymentMethodId = paymentMethod.Id,
            EventId = stripeEvent.Id,
            EventType = stripeEvent.Type,
            Payload = json,
            Processed = false,
            ReceivedAt = DateTime.UtcNow
        };

        _db.PaymentWebhooks.Add(webhookLog);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Unique constraint error on idx_payment_webhooks_event_id means duplicate event.
            // We exit silently (200 OK to Stripe so they don't keep retrying).
            return;
        }

        // 2. Handle relevant event
        if (stripeEvent.Type == Events.CheckoutSessionCompleted)
        {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            if (session != null && session.Metadata.TryGetValue("OrderId", out var orderIdStr))
            {
                if (Guid.TryParse(orderIdStr, out var orderId))
                {
                    // Update DB states
                    await ExecutePaymentCompletionAsync(orderId, session.Id, json, webhookLog);
                }
            }
        }
    }

    public async Task ExecutePaymentCompletionAsync(Guid orderId, string sessionId, string rawPayload, PaymentWebhook webhookLog)
    {
        // Use a database transaction to ensure atomicity
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var order = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.Items).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order is null || order.CurrentStatus != OrderStatuses.Pending)
                return; // already processed or invalid

            var payment = await _db.Payments
                .FirstOrDefaultAsync(p => p.OrderId == orderId && p.ProviderTransactionId == sessionId);

            if (payment is null)
                return;

            // Check stock bounds one final time
            bool stockAvailable = true;
            foreach (var item in order.Items)
            {
                if (item.Product.StockQuantity < item.Quantity)
                {
                    stockAvailable = false;
                    break;
                }
            }

            if (stockAvailable)
            {
                // Decrement stock
                foreach (var item in order.Items)
                {
                    item.Product.StockQuantity -= item.Quantity;
                }

                order.CurrentStatus = OrderStatuses.Paid;
                payment.Status = PaymentStatuses.Succeeded;

                // Clear user cart
                var cart = await _db.Carts
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.UserId == order.UserId);

                if (cart is not null)
                {
                    _db.CartItems.RemoveRange(cart.Items);
                    cart.Items.Clear();
                    cart.UpdatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                order.CurrentStatus = OrderStatuses.Cancelled;
                payment.Status = PaymentStatuses.Failed;
                webhookLog.ErrorMessage = "Stock exhausted prior to payment completion.";
            }

            payment.RawResponse = rawPayload;
            webhookLog.Processed = true;
            webhookLog.ProcessedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            // Send confirmation email
            if (stockAvailable && order.User != null)
            {
                try
                {
                    await _email.SendOrderConfirmationAsync(order.User.Email, order);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send order confirmation email.");
                }
            }

            // Invalidate caches so the user sees updated order list and product stock.
            await _cache.RemoveByPrefixAsync($"orders:user:{order.UserId}:");
            await _cache.RemoveByPrefixAsync("products:list:");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            webhookLog.ErrorMessage = $"Failed executing payment completion: {ex.Message}";
            await _db.SaveChangesAsync();
            throw;
        }
    }
}
