using ECommerce.Application.DTOs.Cart;

namespace ECommerce.Application.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(Guid userId);
    Task AddItemAsync(Guid userId, Guid productVariantId, int quantity);
    Task UpdateItemAsync(Guid userId, Guid productVariantId, int quantity);
    Task RemoveItemAsync(Guid userId, Guid productVariantId);
    Task ClearCartAsync(Guid userId);
}
