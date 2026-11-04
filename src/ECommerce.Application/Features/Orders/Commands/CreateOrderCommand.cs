using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Orders.Commands;

public record CreateOrderCommand(Guid UserId, CreateOrderRequest Request) : IRequest<Guid>;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICartService _cartService;
    private readonly ICacheService _cache;

    public CreateOrderCommandHandler(IApplicationDbContext context, ICartService cartService, ICacheService cache)
    {
        _context = context;
        _cartService = cartService;
        _cache = cache;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var cart = await _cartService.GetCartAsync(request.UserId);

        if (cart.Items.Count == 0)
            throw new InvalidOperationException("Giỏ hàng đang trống, không thể tạo đơn hàng.");

        // Kiểm tra tồn kho lần cuối trước khi tạo đơn
        var variantIds = cart.Items.Select(i => i.ProductVariantId).ToList();
        var variants = await _context.ProductVariants
            .Where(v => variantIds.Contains(v.Id))
            .ToListAsync(ct);

        foreach (var item in cart.Items)
        {
            var variant = variants.FirstOrDefault(v => v.Id == item.ProductVariantId)
                ?? throw new KeyNotFoundException($"Sản phẩm {item.ProductName} không còn tồn tại.");

            if (variant.StockQuantity < item.Quantity)
                throw new InvalidOperationException($"Sản phẩm {item.ProductName} không đủ số lượng tồn kho.");
        }

        var subTotal = cart.Items.Sum(i => i.Price * i.Quantity);

        var order = new Order
        {
            UserId = request.UserId,
            Status = OrderStatus.Pending,
            SubTotal = subTotal,
            DiscountAmount = 0,
            TotalAmount = subTotal,
            ShippingAddress = request.Request.ShippingAddress,
            PaymentExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };

        foreach (var item in cart.Items)
        {
            order.Items.Add(new OrderItem
            {
                ProductVariantId = item.ProductVariantId,
                Quantity = item.Quantity,
                UnitPrice = item.Price
            });

            // Giữ chỗ tồn kho ngay khi tạo đơn; hoàn lại khi huỷ / hết hạn thanh toán
            var variant = variants.First(v => v.Id == item.ProductVariantId);
            variant.StockQuantity -= item.Quantity;
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(ct);

        await _cartService.ClearCartAsync(request.UserId);

        // Tồn kho đã trừ -> xoá cache sản phẩm để không hiển thị số lượng cũ
        await _cache.RemoveByPrefixAsync("products:");

        return order.Id;
    }
}
