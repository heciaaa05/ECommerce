using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Products.Queries;

public record GetProductsQuery(
    Guid? CategoryId,
    string? Brand,
    string? Search,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<ProductDto>>;

public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);

public class GetProductsHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;

    public GetProductsHandler(IApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        // Key bắt đầu bằng "products:list" để Create/Update/Delete xoá được cả nhóm
        var cacheKey = $"products:list:{request.CategoryId}:{request.Brand?.Trim().ToLowerInvariant()}:{request.Search?.Trim().ToLowerInvariant()}:{page}:{pageSize}";
        var cached = await _cache.GetAsync<PagedResult<ProductDto>>(cacheKey);
        if (cached is not null) return cached;

        var query = _context.Products
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .AsQueryable();

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(request.Brand))
            query = query.Where(p => p.Brand == request.Brand);

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(p => p.Name.Contains(request.Search));

        var totalCount = await query.CountAsync(ct);

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = products.Select(ProductMapper.ToDto).ToList();

        var result = new PagedResult<ProductDto>(items, totalCount, page, pageSize);
        await _cache.SetAsync(cacheKey, result, CacheTtl);
        return result;
    }
}
