using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Products.Queries;

public record GetRecommendationsQuery(Guid ProductId, int Top = 5) : IRequest<List<RecommendationDto>>;

public class GetRecommendationsHandler : IRequestHandler<GetRecommendationsQuery, List<RecommendationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IRecommendationService _recommender;
    private readonly ICacheService _cache;

    public GetRecommendationsHandler(
        IApplicationDbContext context, IRecommendationService recommender, ICacheService cache)
    {
        _context = context;
        _recommender = recommender;
        _cache = cache;
    }

    public async Task<List<RecommendationDto>> Handle(GetRecommendationsQuery request, CancellationToken ct)
    {
        var top = Math.Clamp(request.Top, 1, 20);

        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive, ct)
            ?? throw new KeyNotFoundException("Sản phẩm không tồn tại.");

        // Key bắt đầu bằng "products:" nên tự bị xoá khi tồn kho/sản phẩm thay đổi
        var cacheKey = $"products:recs:{request.ProductId}:{top}";
        var cached = await _cache.GetAsync<List<RecommendationDto>>(cacheKey);
        if (cached is not null) return cached;

        var similar = await _recommender.GetSimilarAsync(request.ProductId, top, ct);

        // Service gợi ý lỗi hoặc chưa có dữ liệu -> dự phòng: sản phẩm mới nhất cùng danh mục
        if (similar is null || similar.Count == 0)
            return await FallbackAsync(product.Id, product.CategoryId, top, ct);

        var ids = similar.Select(s => s.ProductId).ToList();
        var products = await _context.Products
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Where(p => ids.Contains(p.Id) && p.IsActive)
            .ToListAsync(ct);

        var byId = products.ToDictionary(p => p.Id);
        var result = new List<RecommendationDto>();
        foreach (var s in similar)
        {
            // Bỏ qua sản phẩm vừa bị xoá/ẩn mà service gợi ý chưa kịp biết
            if (byId.TryGetValue(s.ProductId, out var p))
                result.Add(new RecommendationDto(ProductMapper.ToDto(p), s.Score, s.Reasons));
        }

        if (result.Count == 0)
            return await FallbackAsync(product.Id, product.CategoryId, top, ct);

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
        return result;
    }

    private async Task<List<RecommendationDto>> FallbackAsync(Guid productId, Guid categoryId, int top, CancellationToken ct)
    {
        var products = await _context.Products
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.CategoryId == categoryId && p.Id != productId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(top)
            .ToListAsync(ct);

        // Không cache kết quả dự phòng để khi service gợi ý sống lại thì dùng được ngay
        return products
            .Select(p => new RecommendationDto(ProductMapper.ToDto(p), 0, new List<string> { "fallback_same_category" }))
            .ToList();
    }
}
