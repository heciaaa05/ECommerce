using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Orders.Queries;

public record GetOrderByIdQuery(Guid UserId, Guid OrderId) : IRequest<OrderDto>;

public class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IApplicationDbContext _context;

    public GetOrderByIdHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.ProductVariant)
            .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == request.UserId, ct)
            ?? throw new KeyNotFoundException("Đơn hàng không tồn tại.");

        return MapToDto(order);
    }

    public static OrderDto MapToDto(Order order) => new(
        order.Id,
        order.Status.ToString(),
        order.SubTotal,
        order.DiscountAmount,
        order.VoucherCode,
        order.TotalAmount,
        order.ShippingAddress,
        order.CreatedAt,
        order.Items.Select(i => new OrderItemDto(
            i.ProductVariantId,
            i.ProductVariant.Product.Name,
            i.ProductVariant.VariantName,
            i.Quantity,
            i.UnitPrice
        )).ToList()
    );
}
