using Microsoft.EntityFrameworkCore;
using Stripe;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Novacart.Api.Data;
using Novacart.Api.Models.Entities;
using Novacart.Api.Models.Dtos.Payments;
using Novacart.Api.Factories;
using Novacart.Api.Messaging;
using Novacart.Api.Models.Dtos.Stock;
using Novacart.Api.Services;
using Novacart.Api.Services.Catalog;
using Novacart.Api.Services.Stock;

namespace Novacart.Api.Services.Payments;

public interface IPaymentService
{
    Task<CheckoutResponseDto> ProcessCheckoutAsync(Guid userId, CheckoutRequest request, string gateway);
    Task HandleWebhookAsync(string gateway, string json, string signature);
}

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly IPaymentStrategyFactory _strategyFactory;
    private readonly IOrderFactory _orderFactory;
    private readonly IPricingService _pricing;
    private readonly IRedisCacheService _cache;
    private readonly IEmailQueue _emailQueue;
    private readonly ILogger<PaymentService> _logger;
    private readonly IServiceProvider _services;
    private readonly IProductCatalogStore _catalog;
    private readonly IStockHoldGateway _stockHold;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _stripeWebhookSecret;

    public PaymentService(
        AppDbContext db,
        IPaymentStrategyFactory strategyFactory,
        IOrderFactory orderFactory,
        IPricingService pricing,
        IRedisCacheService cache,
        IConfiguration config,
        IEmailQueue emailQueue,
        ILogger<PaymentService> logger,
        IServiceProvider services,
        IProductCatalogStore catalog,
        IStockHoldGateway stockHold,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _strategyFactory = strategyFactory;
        _orderFactory = orderFactory;
        _pricing = pricing;
        _cache = cache;
        _emailQueue = emailQueue;
        _logger = logger;
        _services = services;
        _catalog = catalog;
        _stockHold = stockHold;
        _httpContextAccessor = httpContextAccessor;
        _stripeWebhookSecret = config["Stripe:WebhookSecret"] ?? string.Empty;
    }

    public async Task<CheckoutResponseDto> ProcessCheckoutAsync(Guid userId, CheckoutRequest request, string gateway)
    {
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null || !cart.Items.Any())
            throw new AppException("Your cart is empty.", StatusCodes.Status409Conflict);

        var products = new Dictionary<Guid, Models.Entities.Product>();
        foreach (var item in cart.Items)
        {
            var product = await _catalog.FindProductAsync(item.ProductId)
                ?? throw AppException.NotFound("Product");
            products[item.ProductId] = product;

            if (!product.IsActive)
                throw new AppException($"Product '{product.Name}' is no longer active.", StatusCodes.Status410Gone);

            if (product.StockQuantity < item.Quantity)
                throw new AppException($"Product '{product.Name}' has insufficient stock (Only {product.StockQuantity} unit(s) available).", StatusCodes.Status422UnprocessableEntity);
        }

        var address = await _db.UserAddresses
            .FirstOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == userId)
            ?? throw AppException.NotFound("Shipping address");

        var user = await ResolveCheckoutUserAsync(userId);

        var productIds = cart.Items.Select(ci => ci.ProductId).Distinct().ToList();
        var categoryIds = products.Values
            .Where(p => p.CategoryId.HasValue)
            .Select(p => p.CategoryId!.Value)
            .Distinct()
            .ToList();
        var activeRules = await _catalog.LoadActiveRulesForProductsAsync(productIds, categoryIds);

        var order = _orderFactory.CreateFromCart(cart, user, address, activeRules, products);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        var holdLines = cart.Items
            .Select(i => new StockHoldLine(i.ProductId, i.Quantity))
            .ToList();

        var holdResult = await _stockHold.TryHoldForOrderAsync(order.Id, holdLines);
        if (!holdResult.Success)
        {
            _db.Orders.Remove(order);
            await _db.SaveChangesAsync();
            throw new AppException(
                holdResult.Error ?? "Unable to reserve stock for checkout.",
                StatusCodes.Status409Conflict);
        }

        // 4. Resolve payment gateway strategy via PaymentStrategyFactory (P3-3: Factory Pattern)
        var strategy = _strategyFactory.Create(gateway);

        // 5. Generate Session URL
        PaymentSessionResult sessionResult;
        try
        {
            sessionResult = await strategy.CreateCheckoutSessionAsync(order, request.SuccessUrl, request.CancelUrl);
        }
        catch
        {
            await _stockHold.ReleaseForOrderAsync(order.Id);
            throw;
        }

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
        Stripe.Event stripeEvent;
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
        else if (stripeEvent.Type == Events.CheckoutSessionExpired)
        {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            if (session?.Metadata.TryGetValue("OrderId", out var orderIdStr) == true &&
                Guid.TryParse(orderIdStr, out var orderId))
            {
                await _stockHold.ReleaseForOrderAsync(orderId);
                webhookLog.Processed = true;
                webhookLog.ProcessedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
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
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order is null || order.CurrentStatus != OrderStatuses.Pending)
                return;

            var payment = await _db.Payments
                .FirstOrDefaultAsync(p => p.OrderId == orderId && p.ProviderTransactionId == sessionId);

            if (payment is null)
                return;

            var publishEndpoint = _services.GetService<MassTransit.IPublishEndpoint>();
            var distributed = publishEndpoint is not null;

            if (distributed)
            {
                payment.Status = PaymentStatuses.Succeeded;
                payment.RawResponse = rawPayload;
                webhookLog.Processed = true;
                webhookLog.ProcessedAt = DateTime.UtcNow;

                var lines = order.Items
                    .Select(i => new PaymentStockLineItem(i.ProductId, i.Quantity))
                    .ToList();

                await publishEndpoint!.Publish(new PaymentCompleted(
                    order.Id,
                    order.OrderNumber,
                    order.UserId,
                    webhookLog.EventId,
                    order.CustomerEmail,
                    lines));

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return;
            }

            order = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.Items).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order is null || order.CurrentStatus != OrderStatuses.Pending)
                return;

            // Monolith path: single DB transaction (stock + cart + email queue).
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

            // Queue confirmation email (non-blocking — sent by the background worker)
            if (stockAvailable && order.User != null)
            {
                try
                {
                    await _emailQueue.EnqueueAsync(new EmailMessage
                    {
                        Kind = EmailKind.OrderConfirmation,
                        Recipient = order.User.Email,
                        OrderNumber = order.OrderNumber,
                        OrderTotal = order.Total,
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to enqueue order confirmation email.");
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

    private async Task<User> ResolveCheckoutUserAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is not null)
            return user;

        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            return new User
            {
                Id = userId,
                Email = principal.FindFirstValue(ClaimTypes.Email)
                    ?? principal.FindFirstValue(JwtRegisteredClaimNames.Email)
                    ?? string.Empty,
                FullName = principal.FindFirstValue(ClaimTypes.Name) ?? "Customer",
            };
        }

        throw AppException.NotFound("User");
    }
}

