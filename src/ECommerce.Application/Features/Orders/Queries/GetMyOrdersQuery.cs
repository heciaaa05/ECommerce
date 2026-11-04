using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Orders.Queries;

public record GetMyOrdersQuery(Guid UserId) : IRequest<List<OrderDto>>;

public class GetMyOrdersHandler : IRequestHandler<GetMyOrdersQuery, List<OrderDto>>
{
    private readonly IApplicationDbContext _context;

    public GetMyOrdersHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<OrderDto>> Handle(GetMyOrdersQuery request, CancellationToken ct)
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.ProductVariant)
            .ThenInclude(v => v.Product)
            .Where(o => o.UserId == request.UserId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        return orders.Select(GetOrderByIdHandler.MapToDto).ToList();
    }
}
