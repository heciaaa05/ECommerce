using ECommerce.Application.DTOs.Products;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Features.Products.Queries;

internal static class ProductMapper
{
    public static ProductDto ToDto(Product product) => new(
        product.Id,
        product.Name,
        product.Slug,
        product.Description,
        product.Brand,
        product.BasePrice,
        product.IsActive,
        product.CategoryId,
        product.Category.Name,
        product.Variants.Select(v => new ProductVariantDto(
            v.Id, v.Sku, v.VariantName, v.Price, v.StockQuantity, v.AttributesJson
        )).ToList(),
        product.Images.Select(i => i.Url).ToList()
    );
}
