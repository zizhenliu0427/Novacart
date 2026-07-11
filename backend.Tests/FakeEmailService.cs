using System.Threading.Tasks;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;

namespace Novacart.Api.Tests;

public class FakeEmailService : IEmailService
{
    public Task SendEmailAsync(string to, string subject, string body) => Task.CompletedTask;
    public Task SendOrderConfirmationAsync(string email, Order order) => Task.CompletedTask;
    public Task SendOrderStatusUpdateAsync(string email, Order order, string newStatus) => Task.CompletedTask;
}
