namespace ECommerce.Application.DTOs.Orders;

// Không nhận danh sách item từ client: đơn hàng luôn được tạo từ Cart hiện tại của user (Redis)
public record CreateOrderRequest(
    string ShippingAddress
);
