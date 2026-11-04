using System.Text.Json;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ECommerce.Infrastructure.Caching;

// Phải dùng record (có property), KHÔNG dùng tuple: System.Text.Json chỉ serialize property,
// ValueTuple là field nên sẽ bị serialize thành {} và giỏ hàng "biến mất" mà không có lỗi nào.
public record CartItemRecord(Guid VariantId, int Quantity);

public class CartService : ICartService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IApplicationDbContext _context;
    private static readonly TimeSpan CartExpiry = TimeSpan.FromDays(7);

    public CartService(IConnectionMultiplexer redis, IApplicationDbContext context)
    {
        _redis = redis;
        _context = context;
    }

    private static string CartKey(Guid userId) => $"cart:{userId}";

    private async Task<List<CartItemRecord>> LoadAsync(Guid userId)
    {
        var db = _redis.GetDatabase();
        var raw = await db.StringGetAsync(CartKey(userId));

        return raw.HasValue
            ? JsonSerializer.Deserialize<List<CartItemRecord>>((string)raw!) ?? new List<CartItemRecord>()
            : new List<CartItemRecord>();
    }

    private async Task SaveAsync(Guid userId, List<CartItemRecord> items)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync(CartKey(userId), JsonSerializer.Serialize(items), CartExpiry);
    }

    public async Task<CartDto> GetCartAsync(Guid userId)
    {
        var rawItems = await LoadAsync(userId);

        if (rawItems.Count == 0)
            return new CartDto(new List<CartItemDto>(), 0);

        var variantIds = rawItems.Select(i => i.VariantId).ToList();

        var variants = await _context.ProductVariants
            .Include(v => v.Product)
            .ThenInclude(p => p.Images)
            .Where(v => variantIds.Contains(v.Id))
            .ToListAsync();

        var items = rawItems
            .Select(ri =>
            {
                var v = variants.FirstOrDefault(x => x.Id == ri.VariantId);
                if (v is null) return null;

                return new CartItemDto(
                    v.Id,
                    v.Product.Name,
                    v.VariantName,
                    v.Price,
                    ri.Quantity,
                    v.Product.Images.FirstOrDefault(i => i.IsPrimary)?.Url
                );
            })
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();

        var total = items.Sum(i => i.Price * i.Quantity);

        return new CartDto(items, total);
    }

    public async Task AddItemAsync(Guid userId, Guid productVariantId, int quantity)
    {
        var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Id == productVariantId)
            ?? throw new KeyNotFoundException("Sản phẩm không tồn tại.");

        var items = await LoadAsync(userId);

        var existingIndex = items.FindIndex(i => i.VariantId == productVariantId);
        var newQuantity = quantity + (existingIndex >= 0 ? items[existingIndex].Quantity : 0);

        if (variant.StockQuantity < newQuantity)
            throw new InvalidOperationException("Số lượng tồn kho không đủ.");

        if (existingIndex >= 0)
            items[existingIndex] = items[existingIndex] with { Quantity = newQuantity };
        else
            items.Add(new CartItemRecord(productVariantId, quantity));

        await SaveAsync(userId, items);
    }

    public async Task UpdateItemAsync(Guid userId, Guid productVariantId, int quantity)
    {
        var items = await LoadAsync(userId);
        var index = items.FindIndex(i => i.VariantId == productVariantId);
        if (index < 0) return;

        if (quantity <= 0)
            items.RemoveAt(index);
        else
            items[index] = items[index] with { Quantity = quantity };

        await SaveAsync(userId, items);
    }

    public async Task RemoveItemAsync(Guid userId, Guid productVariantId)
    {
        var items = await LoadAsync(userId);
        items.RemoveAll(i => i.VariantId == productVariantId);
        await SaveAsync(userId, items);
    }

    public async Task ClearCartAsync(Guid userId)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(CartKey(userId));
    }
}
