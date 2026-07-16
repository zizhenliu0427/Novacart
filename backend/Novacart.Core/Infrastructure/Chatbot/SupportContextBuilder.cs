using System.Text;
using Microsoft.EntityFrameworkCore;
using Novacart.Api.Infrastructure.Sharding;
using Novacart.Api.Models.Dtos.Orders;

namespace Novacart.Api.Infrastructure.Chatbot;

public interface ISupportContextBuilder
{
    Task<string> BuildSystemPromptAsync(string locale, Guid? userId, CancellationToken cancellationToken = default);
}

public sealed class SupportContextBuilder : ISupportContextBuilder
{
    private readonly ISupportFaqStore _faq;
    private readonly IShardedOrderDb _shardedDb;

    public SupportContextBuilder(ISupportFaqStore faq, IShardedOrderDb shardedDb)
    {
        _faq = faq;
        _shardedDb = shardedDb;
    }

    public async Task<string> BuildSystemPromptAsync(string locale, Guid? userId, CancellationToken cancellationToken = default)
    {
        var isZh = locale.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
        var sb = new StringBuilder();

        if (isZh)
        {
            sb.AppendLine("你是 Novacart 在线客服助手。仅根据下方 FAQ 与用户订单摘要回答。");
            sb.AppendLine("无法确认的信息请建议用户联系人工客服，不要编造订单状态或物流单号。");
            sb.AppendLine("回答简洁、友好，使用简体中文。");
        }
        else
        {
            sb.AppendLine("You are the Novacart customer support assistant. Answer only using the FAQ and order summary below.");
            sb.AppendLine("If you cannot confirm something, suggest contacting human support. Never invent order status or tracking numbers.");
            sb.AppendLine("Keep replies concise and friendly. Use Australian English spelling.");
        }

        sb.AppendLine();
        sb.AppendLine(isZh ? "## 常见问题" : "## FAQ");
        foreach (var entry in _faq.GetFaq(locale))
        {
            sb.AppendLine($"- Q: {entry.Question}");
            sb.AppendLine($"  A: {entry.Answer}");
        }

        if (userId is { } uid)
        {
            sb.AppendLine();
            sb.AppendLine(isZh ? "## 用户最近订单（仅摘要）" : "## Recent orders (summary only)");
            var recent = await LoadRecentOrdersAsync(uid, cancellationToken);
            if (recent.Count == 0)
            {
                sb.AppendLine(isZh ? "（该用户暂无订单）" : "(No orders for this user)");
            }
            else
            {
                foreach (var order in recent)
                    AppendOrderSummary(sb, order, isZh);
            }
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine(isZh
                ? "用户未登录 — 不要回答具体订单问题，可引导登录或查看 FAQ。"
                : "User is not signed in — do not answer order-specific questions; suggest sign-in or FAQ.");
        }

        return sb.ToString();
    }

    private async Task<IReadOnlyList<OrderDto>> LoadRecentOrdersAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _shardedDb.ExecuteForUserAsync(userId, async db =>
        {
            return await db.Orders
                .AsNoTracking()
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(3)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    Total = o.Total,
                    Currency = o.Currency,
                    CurrentStatus = o.CurrentStatus,
                    CreatedAt = o.CreatedAt,
                })
                .ToListAsync(cancellationToken);
        });
    }

    private static void AppendOrderSummary(StringBuilder sb, OrderDto order, bool isZh)
    {
        var suffix = order.OrderNumber.Length > 4
            ? order.OrderNumber[^4..]
            : order.OrderNumber;
        if (isZh)
        {
            sb.AppendLine($"- 订单 …{suffix} · 状态 {order.CurrentStatus} · {order.Total:F2} {order.Currency} · {order.CreatedAt:yyyy-MM-dd}");
        }
        else
        {
            sb.AppendLine($"- Order …{suffix} · status {order.CurrentStatus} · {order.Total:F2} {order.Currency} · {order.CreatedAt:yyyy-MM-dd}");
        }
    }
}
