namespace ECommerce.Application.DTOs.Products;

public record ProductVariantDto(
    Guid Id,
    string Sku,
    string VariantName,
    decimal Price,
    int StockQuantity,
    string AttributesJson
);
