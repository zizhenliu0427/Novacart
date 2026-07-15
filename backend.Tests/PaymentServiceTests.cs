using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Novacart.Api.Services;
using Novacart.Api.Services.Payments;
using Novacart.Api.Factories;
using Novacart.Api.Models.Entities;
using Novacart.Api.Models.Dtos.Payments;
using Novacart.Api.Models.Dtos.Cart;
using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Microsoft.Extensions.Logging.Abstractions;

namespace Novacart.Api.Tests;

/// <summary>
/// Stub of IPaymentStrategy to avoid network dependency on Stripe.
/// </summary>
public class FakePaymentStrategy : IPaymentStrategy
{
    public string Code => "stripe";

    public Task<PaymentSessionResult> CreateCheckoutSessionAsync(Order order, string successUrl, string cancelUrl)
    {
        return Task.FromResult(new PaymentSessionResult
        {
            ProviderTransactionId = "cs_test_dummy_session_id",
            RedirectUrl = "https://checkout.stripe.com/pay/cs_test_dummy"
        });
    }
}

/// <summary>
/// Unit tests for PaymentService — covers checkout validation, order generation, pricing freeze,
/// and idempotent transaction completion (stock reduction, cart clearing).
/// </summary>
public class PaymentServiceTests
{
    private readonly IConfiguration _config;
    private readonly IPaymentStrategyFactory _strategyFactory;
    private readonly IOrderFactory _orderFactory;

    public PaymentServiceTests()
    {
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Stripe:WebhookSecret", "whsec_test_secret" }
            })
            .Build();

        var strategies = new List<IPaymentStrategy> { new FakePaymentStrategy() };
        _strategyFactory = new PaymentStrategyFactory(strategies);
        _orderFactory = new OrderFactory(new PricingService());
    }

    [Fact]
    public async Task ProcessCheckoutAsync_CreatesOrderAndPayment_WhenCartIsValid()
    {
        using var db = TestDbFactory.Create();
        var cartSvc = new CartService(db, new PricingService());
        var paymentSvc = new PaymentService(db, _strategyFactory, _orderFactory, new PricingService(), new NullRedisCacheService(), _config, new FakeEmailService(), NullLogger<PaymentService>.Instance);

        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var product = await TestDbFactory.GetFirstProductAsync(db);

        // Add 2 headphones to cart
        await cartSvc.AddItemAsync(userId, new AddCartItemRequest { ProductId = product.Id, Quantity = 2 });

        var addressId = await SeedAddressAsync(db, userId);

        var request = new CheckoutRequest
        {
            SuccessUrl = "http://success",
            CancelUrl = "http://cancel",
            AddressId = addressId
        };

        var response = await paymentSvc.ProcessCheckoutAsync(userId, request, "stripe");

        // Verify redirect info
        response.Should().NotBeNull();
        response.SessionId.Should().Be("cs_test_dummy_session_id");
        response.RedirectUrl.Should().Be("https://checkout.stripe.com/pay/cs_test_dummy");

        // Verify order saved in DB
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.UserId == userId);
        order.Should().NotBeNull();
        order!.CurrentStatus.Should().Be(OrderStatuses.Pending);
        order.Subtotal.Should().Be(product.Price * 2);
        order.Total.Should().BeGreaterThan(order.Subtotal); // should include tax/shipping

        order.Items.Should().HaveCount(1);
        var item = order.Items.First();
        item.ProductId.Should().Be(product.Id);
        item.ProductNameSnapshot.Should().Be(product.Name);
        item.PriceAtPurchase.Should().Be(product.Price);
        item.Quantity.Should().Be(2);

        // Verify transaction saved in DB
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == order.Id);
        payment.Should().NotBeNull();
        payment!.Status.Should().Be(PaymentStatuses.Pending);
        payment.ProviderTransactionId.Should().Be("cs_test_dummy_session_id");
    }

    [Fact]
    public async Task ProcessCheckoutAsync_ThrowsAppException_WhenCartIsEmpty()
    {
        using var db = TestDbFactory.Create();
        var paymentSvc = new PaymentService(db, _strategyFactory, _orderFactory, new PricingService(), new NullRedisCacheService(), _config, new FakeEmailService(), NullLogger<PaymentService>.Instance);
        var userId = await TestDbFactory.SeedTestUserAsync(db);

        var addressId = await SeedAddressAsync(db, userId);
        var request = new CheckoutRequest { SuccessUrl = "http://s", CancelUrl = "http://c", AddressId = addressId };

        var act = () => paymentSvc.ProcessCheckoutAsync(userId, request, "stripe");

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 409);
    }

    [Fact]
    public async Task ExecutePaymentCompletionAsync_UpdatesStatuses_DecrementsStock_AndClearsCart()
    {
        using var db = TestDbFactory.Create();
        var cartSvc = new CartService(db, new PricingService());
        var paymentSvc = new PaymentService(db, _strategyFactory, _orderFactory, new PricingService(), new NullRedisCacheService(), _config, new FakeEmailService(), NullLogger<PaymentService>.Instance);

        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var product = await TestDbFactory.GetFirstProductAsync(db);
        product.StockQuantity = 10;
        await db.SaveChangesAsync();

        // 1. Setup Cart
        await cartSvc.AddItemAsync(userId, new AddCartItemRequest { ProductId = product.Id, Quantity = 3 });

        var addressId = await SeedAddressAsync(db, userId);
        // 2. Perform Checkout (creates pending order and payment)
        var checkout = await paymentSvc.ProcessCheckoutAsync(userId, new CheckoutRequest { SuccessUrl = "http://s", CancelUrl = "http://c", AddressId = addressId }, "stripe");
        var order = await db.Orders.FirstAsync(o => o.UserId == userId);

        // Verify stock is not decremented yet during checkout creation
        var freshProduct = await db.Products.FirstAsync(p => p.Id == product.Id);
        freshProduct.StockQuantity.Should().Be(10);

        // 3. Complete payment webhook transaction
        var webhookLog = new PaymentWebhook { EventId = "evt_test", EventType = "checkout.session.completed", Payload = "{}" };
        db.PaymentWebhooks.Add(webhookLog);
        await db.SaveChangesAsync();

        await paymentSvc.ExecutePaymentCompletionAsync(order.Id, checkout.SessionId, "{\"paid\": true}", webhookLog);

        // 4. Verify DB transitions
        var paidOrder = await db.Orders.FirstAsync(o => o.Id == order.Id);
        paidOrder.CurrentStatus.Should().Be(OrderStatuses.Paid);

        var paidPayment = await db.Payments.FirstAsync(p => p.OrderId == order.Id);
        paidPayment.Status.Should().Be(PaymentStatuses.Succeeded);
        paidPayment.RawResponse.Should().Be("{\"paid\": true}");

        // Stock must be decremented
        var updatedProduct = await db.Products.FirstAsync(p => p.Id == product.Id);
        updatedProduct.StockQuantity.Should().Be(7); // 10 - 3 = 7

        // Cart must be cleared
        var cart = await db.Carts.Include(c => c.Items).FirstAsync(c => c.UserId == userId);
        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecutePaymentCompletionAsync_CancelsOrder_WhenStockExhaustedPriorToCompletion()
    {
        using var db = TestDbFactory.Create();
        var cartSvc = new CartService(db, new PricingService());
        var paymentSvc = new PaymentService(db, _strategyFactory, _orderFactory, new PricingService(), new NullRedisCacheService(), _config, new FakeEmailService(), NullLogger<PaymentService>.Instance);

        var userId = await TestDbFactory.SeedTestUserAsync(db);
        var product = await TestDbFactory.GetFirstProductAsync(db);
        product.StockQuantity = 5;
        await db.SaveChangesAsync();

        // 1. Setup Cart with 4 items
        await cartSvc.AddItemAsync(userId, new AddCartItemRequest { ProductId = product.Id, Quantity = 4 });

        var addressId = await SeedAddressAsync(db, userId);
        // 2. Perform Checkout
        var checkout = await paymentSvc.ProcessCheckoutAsync(userId, new CheckoutRequest { SuccessUrl = "http://s", CancelUrl = "http://c", AddressId = addressId }, "stripe");
        var order = await db.Orders.FirstAsync(o => o.UserId == userId);

        // 3. Parallel purchase exhausts stock in the meantime (e.g. stock goes to 2)
        var dbProduct = await db.Products.FirstAsync(p => p.Id == product.Id);
        dbProduct.StockQuantity = 2;
        await db.SaveChangesAsync();

        // 4. complete payment webhook transaction (should fail stock check)
        var webhookLog = new PaymentWebhook { EventId = "evt_test_fail", EventType = "checkout.session.completed", Payload = "{}" };
        db.PaymentWebhooks.Add(webhookLog);
        await db.SaveChangesAsync();

        await paymentSvc.ExecutePaymentCompletionAsync(order.Id, checkout.SessionId, "{}", webhookLog);

        // 5. Verify order is cancelled, payment failed, stock unchanged, and cart NOT cleared (user can try again or edit cart)
        var failedOrder = await db.Orders.FirstAsync(o => o.Id == order.Id);
        failedOrder.CurrentStatus.Should().Be(OrderStatuses.Cancelled);

        var failedPayment = await db.Payments.FirstAsync(p => p.OrderId == order.Id);
        failedPayment.Status.Should().Be(PaymentStatuses.Failed);

        var finalProduct = await db.Products.FirstAsync(p => p.Id == product.Id);
        finalProduct.StockQuantity.Should().Be(2);

        var cart = await db.Carts.Include(c => c.Items).FirstAsync(c => c.UserId == userId);
        cart.Items.Should().NotBeEmpty();
    }

    private static async Task<Guid> SeedAddressAsync(AppDbContext db, Guid userId)
    {
        var addr = new UserAddress
        {
            UserId = userId,
            Label = "Home",
            Line1 = "123 Main St",
            City = "Sydney",
            State = "NSW",
            Postcode = "2000",
            Country = "Australia",
            IsDefaultShipping = true
        };
        db.UserAddresses.Add(addr);
        await db.SaveChangesAsync();
        return addr.Id;
    }
}
