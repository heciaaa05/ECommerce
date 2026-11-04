namespace ECommerce.Domain.Enums;

// 0 = Customer, 1 = Seller, 2 = Admin  (giữ nguyên thứ tự này vì DB lưu dạng số)
public enum UserRole
{
    Customer,
    Seller,
    Admin
}
