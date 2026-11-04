namespace ECommerce.Application.DTOs.Cart;

public record CartItemDto(
    Guid ProductVariantId,
    string ProductName,
    string VariantName,
    decimal Price,
    int Quantity,
    string? ImageUrl
);

public record CartDto(
    List<CartItemDto> Items,
    decimal TotalAmount
);
