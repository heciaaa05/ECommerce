using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence;

/// <summary>
/// Sinh dữ liệu hành vi GIẢ để thử gợi ý hybrid: 60 user demo thuộc 4 nhóm sở thích (persona).
/// Mỗi persona hay xem/mua cùng một "giỏ" sản phẩm xuyên danh mục (chuột + bàn phím + màn hình...),
/// nhờ đó có tín hiệu "người mua A cũng mua B" mà gợi ý theo nội dung không thấy được.
/// Dữ liệu cố định theo seed nên chạy lại ra kết quả giống nhau. Chỉ chạy khi chưa có hành vi nào.
/// </summary>
public static class DemoInteractionSeeder
{
    public const string DemoPassword = "Demo@12345";   // mật khẩu chung của user demo (demo01@example.com ...)
    private const int UsersPerPersona = 15;

    private static readonly (string Name, string[] Basket)[] Personas =
    {
        ("FPS esports", new[]
        {
            "Logitech G Pro X Superlight 2", "Razer Viper V3 Pro", "Logitech G Pro X TKL Lightspeed",
            "SteelSeries Apex Pro TKL", "Razer BlackShark V2 Pro", "ASUS ROG Swift PG27AQN",
            "Artisan Hayate Otsu", "Logitech G640"
        }),
        ("Sinh viên tiết kiệm", new[]
        {
            "Logitech G502 Hero", "AKKO 3068B Plus", "HyperX Cloud Stinger 2",
            "AOC 24G2SP", "Razer Gigantus V2 Large", "E-Dra Hercules EGC203"
        }),
        ("MMO và thoải mái", new[]
        {
            "SteelSeries Aerox 5 Wireless", "Corsair K70 RGB Pro", "Corsair HS80 RGB Wireless",
            "Samsung Odyssey G5 32", "Secretlab Titan Evo", "Corsair MM700 RGB Extended",
            "SteelSeries QcK Heavy XXL"
        }),
        ("Góc máy cao cấp", new[]
        {
            "Razer DeathAdder V3 Pro", "Razer BlackWidow V4 Pro", "SteelSeries Arctis Nova Pro Wireless",
            "Dell Alienware AW2725DF", "Secretlab Titan Evo", "Razer Iskur V2",
            "Glorious Model O 2 Wireless", "Keychron K8 Pro"
        }),
    };

    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        if (await db.UserInteractions.AnyAsync(ct)) return;

        var products = await db.Products.ToDictionaryAsync(p => p.Name, p => p.Id, ct);
        if (products.Count == 0) return;
        var allProductIds = products.Values.ToList();

        var rnd = new Random(42);
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword, workFactor: 10); // dùng chung 1 hash cho nhanh
        var now = DateTime.UtcNow;

        var users = new List<User>();
        var interactions = new List<UserInteraction>();
        var n = 1;

        foreach (var (_, basket) in Personas)
        {
            var basketIds = basket.Where(products.ContainsKey).Select(name => products[name]).ToList();

            for (var i = 0; i < UsersPerPersona; i++, n++)
            {
                var user = new User
                {
                    Email = $"demo{n:00}@example.com",
                    PasswordHash = passwordHash,
                    FullName = $"Demo User {n:00}",
                    Role = UserRole.Customer
                };
                users.Add(user);

                void Add(Guid productId, InteractionType type) => interactions.Add(new UserInteraction
                {
                    UserId = user.Id,
                    ProductId = productId,
                    Type = type,
                    CreatedAt = now.AddDays(-rnd.Next(1, 60)).AddMinutes(-rnd.Next(0, 1440))
                });

                foreach (var productId in basketIds)
                {
                    if (rnd.NextDouble() > 0.80) continue;          // không phải ai cũng xem hết giỏ

                    for (var v = rnd.Next(1, 4); v > 0; v--) Add(productId, InteractionType.View);

                    if (rnd.NextDouble() > 0.45) continue;          // một phần bỏ vào giỏ
                    Add(productId, InteractionType.AddToCart);

                    if (rnd.NextDouble() > 0.70) continue;          // một phần mua thật
                    Add(productId, InteractionType.Purchase);
                }

                // Nhiễu: vài lượt xem ngẫu nhiên ngoài sở thích
                for (var k = rnd.Next(1, 4); k > 0; k--)
                    Add(allProductIds[rnd.Next(allProductIds.Count)], InteractionType.View);
            }
        }

        db.Users.AddRange(users);
        db.UserInteractions.AddRange(interactions);
        await db.SaveChangesAsync(ct);
    }
}
