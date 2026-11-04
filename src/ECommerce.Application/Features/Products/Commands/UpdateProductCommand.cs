using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Products.Commands;

public record UpdateProductCommand(Guid Id, UpdateProductRequest Request) : IRequest<Unit>;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;

    public UpdateProductCommandHandler(IApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Unit> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw new KeyNotFoundException("Sản phẩm không tồn tại.");

        var req = request.Request;
        product.Name = req.Name;
        product.Description = req.Description;
        product.Brand = req.Brand;
        product.BasePrice = req.BasePrice;
        product.CategoryId = req.CategoryId;
        product.IsActive = req.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        await _cache.RemoveAsync($"products:{request.Id}");
        await _cache.RemoveByPrefixAsync("products:list");

        return Unit.Value;
    }
}
