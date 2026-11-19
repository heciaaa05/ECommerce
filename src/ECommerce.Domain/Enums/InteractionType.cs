namespace ECommerce.Domain.Enums;

// Thứ tự giữ nguyên vì DB lưu dạng số. Trọng số dùng khi gợi ý: View < AddToCart < Purchase
public enum InteractionType
{
    View,
    AddToCart,
    Purchase
}
