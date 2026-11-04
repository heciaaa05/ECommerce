namespace ECommerce.Application.DTOs.Products;

public record CreateVariantRequest(
    string Sku,
    string VariantName,
    decimal Price,
    int StockQuantity,
    Dictionary<string, string> Attributes
);

public record CreateProductRequest(
    string Name,
    string? Description,
    string? Brand,
    decimal BasePrice,
    Guid CategoryId,
    List<CreateVariantRequest> Variants,
    List<string> ImageUrls
);
