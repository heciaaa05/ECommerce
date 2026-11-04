using System.Security.Claims;

namespace ECommerce.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    // userId luôn lấy từ JWT, không nhận từ client -> user A không thể thao tác trên cart/order của user B.
    // Thử cả NameIdentifier lẫn "sub" vì tên claim phụ thuộc vào cấu hình claim mapping của JwtBearer.
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue("sub")
                    ?? throw new UnauthorizedAccessException("Không xác định được user.");

        return Guid.Parse(value);
    }
}
