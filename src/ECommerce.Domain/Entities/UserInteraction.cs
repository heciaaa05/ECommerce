using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

// Hành vi người dùng với sản phẩm (xem, thêm giỏ, mua) - dữ liệu đầu vào cho gợi ý hybrid
public class UserInteraction : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;

    public InteractionType Type { get; set; }
}
