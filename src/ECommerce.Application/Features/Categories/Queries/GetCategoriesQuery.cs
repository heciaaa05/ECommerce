using ECommerce.Application.DTOs.Categories;
using ECommerce.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Categories.Queries;

public record GetCategoriesQuery : IRequest<List<CategoryDto>>;

public class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    public const string CacheKey = "categories:all";

    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;

    public GetCategoriesHandler(IApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        var cached = await _cache.GetAsync<List<CategoryDto>>(CacheKey);
        if (cached is not null) return cached;

        var categories = await _context.Categories
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.ParentCategoryId))
            .ToListAsync(ct);

        await _cache.SetAsync(CacheKey, categories, TimeSpan.FromHours(1));
        return categories;
    }
}
