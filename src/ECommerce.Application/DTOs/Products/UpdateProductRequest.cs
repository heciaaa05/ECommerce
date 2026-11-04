namespace ECommerce.Application.DTOs.Products;

public record UpdateProductRequest(
    string Name,
    string? Description,
    string? Brand,
    decimal BasePrice,
    Guid CategoryId,
    bool IsActive
);
