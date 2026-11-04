namespace ECommerce.Application.DTOs.Orders;

public record OrderItemDto(
    Guid ProductVariantId,
    string ProductName,
    string VariantName,
    int Quantity,
    decimal UnitPrice
);

public record OrderDto(
    Guid Id,
    string Status,
    decimal SubTotal,
    decimal DiscountAmount,
    string? VoucherCode,
    decimal TotalAmount,
    string ShippingAddress,
    DateTime CreatedAt,
    List<OrderItemDto> Items
);
