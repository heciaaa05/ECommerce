namespace ECommerce.Application.DTOs.Products;

public record ProductDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? Brand,
    decimal BasePrice,
    bool IsActive,
    Guid CategoryId,
    string CategoryName,
    List<ProductVariantDto> Variants,
    List<string> ImageUrls
);
