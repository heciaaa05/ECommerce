using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public class Order : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public string? VoucherCode { get; set; }        // placeholder cho voucher sau này
    public decimal TotalAmount { get; set; }

    public string ShippingAddress { get; set; } = default!;

    // Hạn thanh toán: quá hạn mà vẫn Pending thì OrderExpirationJob sẽ huỷ đơn và hoàn tồn kho
    public DateTime? PaymentExpiresAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public Payment? Payment { get; set; }
}
