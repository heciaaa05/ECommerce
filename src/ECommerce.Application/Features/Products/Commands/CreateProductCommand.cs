using System.Text.Json;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Products.Commands;

public record CreateProductCommand(CreateProductRequest Request) : IRequest<Guid>;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;

    public CreateProductCommandHandler(IApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var req = request.Request;

        var categoryExists = await _context.Categories.AnyAsync(c => c.Id == req.CategoryId, ct);
        if (!categoryExists)
            throw new KeyNotFoundException("Category không tồn tại.");

        var product = new Product
        {
            Name = req.Name,
            Slug = GenerateSlug(req.Name),
            Description = req.Description,
            Brand = req.Brand,
            BasePrice = req.BasePrice,
            CategoryId = req.CategoryId,
            IsActive = true
        };

        foreach (var v in req.Variants ?? new List<CreateVariantRequest>())
        {
            product.Variants.Add(new ProductVariant
            {
                Sku = v.Sku,
                VariantName = v.VariantName,
                Price = v.Price,
                StockQuantity = v.StockQuantity,
                AttributesJson = JsonSerializer.Serialize(v.Attributes ?? new Dictionary<string, string>())
            });
        }

        foreach (var url in req.ImageUrls ?? new List<string>())
        {
            product.Images.Add(new ProductImage
            {
                Url = url,
                IsPrimary = product.Images.Count == 0
            });
        }

        _context.Products.Add(product);
        await _context.SaveChangesAsync(ct);

        await _cache.RemoveByPrefixAsync("products:list");

        return product.Id;
    }

    // Thêm hậu tố ngẫu nhiên để tránh trùng slug khi 2 sản phẩm cùng tên
    private static string GenerateSlug(string name)
        => name.ToLower().Replace(" ", "-").Replace("đ", "d")
           + "-" + Guid.NewGuid().ToString("N")[..6];
}
