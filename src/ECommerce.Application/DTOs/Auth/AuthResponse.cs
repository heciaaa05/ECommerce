namespace ECommerce.Application.DTOs.Auth;

public record AuthResponse(
    Guid UserId,
    string Email,
    string FullName,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt
);
