using ECommerce.Application.Interfaces;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Orders.Commands;

public record CancelOrderCommand(Guid UserId, Guid OrderId) : IRequest<Unit>;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;

    public CancelOrderCommandHandler(IApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Unit> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == request.UserId, ct)
            ?? throw new KeyNotFoundException("Đơn hàng không tồn tại.");

        if (order.Status is OrderStatus.Completed or OrderStatus.Shipping)
            throw new InvalidOperationException("Không thể huỷ đơn hàng đang giao hoặc đã hoàn tất.");

        if (order.Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Đơn hàng đã được huỷ trước đó.");

        // Hoàn trả tồn kho
        var variantIds = order.Items.Select(i => i.ProductVariantId).ToList();
        var variants = await _context.ProductVariants
            .Where(v => variantIds.Contains(v.Id))
            .ToListAsync(ct);

        foreach (var item in order.Items)
        {
            var variant = variants.FirstOrDefault(v => v.Id == item.ProductVariantId);
            if (variant is not null)
                variant.StockQuantity += item.Quantity;
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        // Tồn kho đã đổi -> xoá cache sản phẩm để không hiển thị số lượng cũ
        await _cache.RemoveByPrefixAsync("products:");
        return Unit.Value;
    }
}
