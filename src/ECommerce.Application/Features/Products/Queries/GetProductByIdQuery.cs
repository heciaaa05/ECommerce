using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Products.Queries;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;

public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;

    public GetProductByIdHandler(IApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken ct)
    {
        var cacheKey = $"products:{request.Id}";
        var cached = await _cache.GetAsync<ProductDto>(cacheKey);
        if (cached is not null) return cached;

        var product = await _context.Products
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw new KeyNotFoundException("Sản phẩm không tồn tại.");

        var dto = ProductMapper.ToDto(product);

        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(30));
        return dto;
    }
}
