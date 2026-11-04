namespace ECommerce.Application.DTOs.Cart;

public record AddToCartRequest(
    Guid ProductVariantId,
    int Quantity
);

public record UpdateCartItemRequest(
    Guid ProductVariantId,
    int Quantity
);
