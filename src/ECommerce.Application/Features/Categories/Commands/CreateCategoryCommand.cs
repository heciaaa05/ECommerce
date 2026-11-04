using ECommerce.Application.DTOs.Categories;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Features.Categories.Commands;

public record CreateCategoryCommand(CreateCategoryRequest Request) : IRequest<Guid>;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;

    public CreateCategoryCommandHandler(IApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var req = request.Request;

        var category = new Category
        {
            Name = req.Name,
            Slug = req.Name.ToLower().Replace(" ", "-").Replace("đ", "d"),
            ParentCategoryId = req.ParentCategoryId
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(ct);

        await _cache.RemoveAsync("categories:all");

        return category.Id;
    }
}
