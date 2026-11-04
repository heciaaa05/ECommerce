using ECommerce.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Products.Commands;

public record DeleteProductCommand(Guid Id) : IRequest<Unit>;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;

    public DeleteProductCommandHandler(IApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Unit> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw new KeyNotFoundException("Sản phẩm không tồn tại.");

        // Soft delete: giữ lịch sử cho các OrderItem đã tham chiếu tới sản phẩm này
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        await _cache.RemoveAsync($"products:{request.Id}");
        await _cache.RemoveByPrefixAsync("products:list");

        return Unit.Value;
    }
}
