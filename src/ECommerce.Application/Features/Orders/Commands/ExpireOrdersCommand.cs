using ECommerce.Application.Interfaces;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Orders.Commands;

// Huỷ các đơn Pending đã quá hạn thanh toán và hoàn trả tồn kho. Trả về số đơn đã huỷ.
public record ExpireOrdersCommand : IRequest<int>;

public class ExpireOrdersCommandHandler : IRequestHandler<ExpireOrdersCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;

    public ExpireOrdersCommandHandler(IApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<int> Handle(ExpireOrdersCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var expiredOrders = await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.Status == OrderStatus.Pending
                        && o.PaymentExpiresAt != null
                        && o.PaymentExpiresAt < now)
            .ToListAsync(ct);

        if (expiredOrders.Count == 0) return 0;

        var variantIds = expiredOrders.SelectMany(o => o.Items).Select(i => i.ProductVariantId).Distinct().ToList();
        var variants = await _context.ProductVariants
            .Where(v => variantIds.Contains(v.Id))
            .ToListAsync(ct);

        foreach (var order in expiredOrders)
        {
            foreach (var item in order.Items)
            {
                var variant = variants.FirstOrDefault(v => v.Id == item.ProductVariantId);
                if (variant is not null)
                    variant.StockQuantity += item.Quantity;
            }

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(ct);

        if (expiredOrders.Count > 0)
            await _cache.RemoveByPrefixAsync("products:");

        return expiredOrders.Count;
    }
}
