using System.Text.Json;
using System.Text.RegularExpressions;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence;

/// <summary>
/// Dữ liệu mẫu gaming gear để test gợi ý sản phẩm (Tuần 5). Chỉ chạy khi chưa có sản phẩm nào,
/// nên bật nhiều lần cũng không bị trùng.
/// </summary>
public static class DbSeeder
{
    private record V(string Name, decimal Price, int Stock, Dictionary<string, string> Attrs);
    private record P(string Category, string Name, string Brand, decimal BasePrice, string Description, V[] Variants);

    public static async Task SeedDemoDataAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        if (await db.Products.AnyAsync(ct)) return;

        var categories = new Dictionary<string, Category>
        {
            ["Chuột gaming"] = new() { Name = "Chuột gaming", Slug = "chuot-gaming" },
            ["Bàn phím cơ"] = new() { Name = "Bàn phím cơ", Slug = "ban-phim-co" },
            ["Tai nghe gaming"] = new() { Name = "Tai nghe gaming", Slug = "tai-nghe-gaming" },
            ["Màn hình gaming"] = new() { Name = "Màn hình gaming", Slug = "man-hinh-gaming" },
            ["Ghế gaming"] = new() { Name = "Ghế gaming", Slug = "ghe-gaming" },
            ["Lót chuột"] = new() { Name = "Lót chuột", Slug = "lot-chuot" },
        };
        db.Categories.AddRange(categories.Values);

        foreach (var p in Catalog())
        {
            var product = new Product
            {
                Name = p.Name,
                Slug = Slugify(p.Name),
                Description = p.Description,
                Brand = p.Brand,
                BasePrice = p.BasePrice,
                IsActive = true,
                CategoryId = categories[p.Category].Id
            };

            var index = 1;
            foreach (var v in p.Variants)
            {
                product.Variants.Add(new ProductVariant
                {
                    Sku = $"{Slugify(p.Name).ToUpperInvariant()}-{index++}",
                    VariantName = v.Name,
                    Price = v.Price,
                    StockQuantity = v.Stock,
                    AttributesJson = JsonSerializer.Serialize(v.Attrs)
                });
            }

            db.Products.Add(product);
        }

        await db.SaveChangesAsync(ct);
    }

    private static string Slugify(string name)
        => Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');

    private static Dictionary<string, string> A(params (string k, string v)[] items)
        => items.ToDictionary(i => i.k, i => i.v);

    private static IEnumerable<P> Catalog() => new List<P>
    {
        // ---------- Chuột gaming ----------
        new("Chuột gaming", "Logitech G Pro X Superlight 2", "Logitech", 3490000,
            "Chuột gaming không dây siêu nhẹ cho esports FPS, cảm biến HERO 2 độ chính xác cao, pin lâu, polling rate 2000Hz.",
            new[] { new V("Đen", 3490000, 20, A(("color","black"),("weight","60g"),("connectivity","wireless"),("dpi","32000"))),
                    new V("Trắng", 3490000, 15, A(("color","white"),("weight","60g"),("connectivity","wireless"),("dpi","32000"))) }),
        new("Chuột gaming", "Razer DeathAdder V3 Pro", "Razer", 3990000,
            "Chuột không dây thiết kế công thái học cho esports FPS, cảm biến Focus Pro 30K, trọng lượng nhẹ.",
            new[] { new V("Đen", 3990000, 18, A(("color","black"),("weight","63g"),("connectivity","wireless"),("dpi","30000"))) }),
        new("Chuột gaming", "Razer Viper V3 Pro", "Razer", 3790000,
            "Chuột không dây siêu nhẹ đối xứng cho esports FPS, cảm biến Focus Pro 35K, polling rate 8000Hz.",
            new[] { new V("Đen", 3790000, 12, A(("color","black"),("weight","54g"),("connectivity","wireless"),("dpi","35000"))),
                    new V("Trắng", 3790000, 10, A(("color","white"),("weight","54g"),("connectivity","wireless"),("dpi","35000"))) }),
        new("Chuột gaming", "Logitech G502 Hero", "Logitech", 1190000,
            "Chuột gaming có dây nhiều nút lập trình, tạ điều chỉnh trọng lượng, cảm biến HERO 25K, phù hợp MMO và FPS.",
            new[] { new V("Đen", 1190000, 40, A(("color","black"),("weight","121g"),("connectivity","wired"),("dpi","25600"))) }),
        new("Chuột gaming", "SteelSeries Aerox 5 Wireless", "SteelSeries", 2890000,
            "Chuột không dây nhiều nút bên hông cho MMO, thiết kế tổ ong siêu nhẹ, kết nối Bluetooth và 2.4GHz.",
            new[] { new V("Đen", 2890000, 14, A(("color","black"),("weight","74g"),("connectivity","wireless"),("dpi","18000"))) }),
        new("Chuột gaming", "Glorious Model O 2 Wireless", "Glorious", 2490000,
            "Chuột không dây siêu nhẹ thiết kế lỗ tổ ong, cảm biến BAMF 2.0, dành cho FPS và gamer thích trải nghiệm nhẹ.",
            new[] { new V("Đen", 2490000, 16, A(("color","black"),("weight","68g"),("connectivity","wireless"),("dpi","26000"))),
                    new V("Trắng", 2490000, 9, A(("color","white"),("weight","68g"),("connectivity","wireless"),("dpi","26000"))) }),

        // ---------- Bàn phím cơ ----------
        new("Bàn phím cơ", "Keychron K8 Pro", "Keychron", 2690000,
            "Bàn phím cơ không dây layout TKL, hot-swap switch, tương thích Windows và macOS, kết nối Bluetooth hoặc có dây.",
            new[] { new V("Red Switch", 2690000, 25, A(("switch","Red"),("layout","TKL"),("connectivity","wireless"),("hotswap","yes"))),
                    new V("Brown Switch", 2690000, 22, A(("switch","Brown"),("layout","TKL"),("connectivity","wireless"),("hotswap","yes"))) }),
        new("Bàn phím cơ", "Logitech G Pro X TKL Lightspeed", "Logitech", 4290000,
            "Bàn phím cơ không dây TKL cho esports, switch GX, độ trễ thấp, thiết kế gọn cho người chơi FPS.",
            new[] { new V("GX Red", 4290000, 10, A(("switch","Red"),("layout","TKL"),("connectivity","wireless"),("hotswap","no"))) }),
        new("Bàn phím cơ", "Razer BlackWidow V4 Pro", "Razer", 5990000,
            "Bàn phím cơ fullsize cao cấp có dây, switch Razer Green, núm xoay đa năng, đèn RGB Chroma.",
            new[] { new V("Green Switch", 5990000, 8, A(("switch","Green"),("layout","Fullsize"),("connectivity","wired"),("hotswap","no"))) }),
        new("Bàn phím cơ", "SteelSeries Apex Pro TKL", "SteelSeries", 4890000,
            "Bàn phím cơ TKL với switch OmniPoint điều chỉnh được điểm kích hoạt, phản hồi cực nhanh cho esports.",
            new[] { new V("OmniPoint", 4890000, 11, A(("switch","OmniPoint"),("layout","TKL"),("connectivity","wired"),("hotswap","no"))) }),
        new("Bàn phím cơ", "AKKO 3068B Plus", "AKKO", 1890000,
            "Bàn phím cơ layout 65% không dây giá tốt, hot-swap, keycap PBT, kết nối Bluetooth, 2.4GHz và có dây.",
            new[] { new V("Blue Switch", 1890000, 30, A(("switch","Blue"),("layout","65%"),("connectivity","wireless"),("hotswap","yes"))),
                    new V("Red Switch", 1890000, 28, A(("switch","Red"),("layout","65%"),("connectivity","wireless"),("hotswap","yes"))) }),
        new("Bàn phím cơ", "Corsair K70 RGB Pro", "Corsair", 3290000,
            "Bàn phím cơ fullsize có dây, switch Cherry MX, khung nhôm bền, đèn RGB, chế độ thi đấu.",
            new[] { new V("Cherry MX Red", 3290000, 15, A(("switch","Red"),("layout","Fullsize"),("connectivity","wired"),("hotswap","no"))) }),

        // ---------- Tai nghe gaming ----------
        new("Tai nghe gaming", "HyperX Cloud II Wireless", "HyperX", 2790000,
            "Tai nghe gaming không dây âm thanh vòm 7.1, pin trên 30 giờ, micro tháo rời, đệm tai mút nhớ đệm êm.",
            new[] { new V("Đỏ đen", 2790000, 20, A(("color","red"),("connectivity","wireless"),("surround","7.1"))) }),
        new("Tai nghe gaming", "SteelSeries Arctis Nova Pro Wireless", "SteelSeries", 8990000,
            "Tai nghe gaming không dây cao cấp, chống ồn chủ động, pin đôi thay nóng, kèm hộp điều khiển GameDAC.",
            new[] { new V("Đen", 8990000, 6, A(("color","black"),("connectivity","wireless"),("anc","yes"))) }),
        new("Tai nghe gaming", "Razer BlackShark V2 Pro", "Razer", 4490000,
            "Tai nghe không dây cho esports, driver TriForce 50mm, micro HyperClear, âm thanh vòm THX, nhẹ và thoải mái.",
            new[] { new V("Đen", 4490000, 13, A(("color","black"),("connectivity","wireless"),("surround","7.1"))) }),
        new("Tai nghe gaming", "Logitech G Pro X 2 Lightspeed", "Logitech", 5290000,
            "Tai nghe gaming không dây driver graphene, micro rời, kết nối Lightspeed, Bluetooth và có dây.",
            new[] { new V("Đen", 5290000, 9, A(("color","black"),("connectivity","wireless"),("surround","7.1"))),
                    new V("Trắng", 5290000, 7, A(("color","white"),("connectivity","wireless"),("surround","7.1"))) }),
        new("Tai nghe gaming", "Corsair HS80 RGB Wireless", "Corsair", 3590000,
            "Tai nghe không dây âm thanh Dolby Atmos, micro đa hướng, đệm tai mút nhớ, đèn RGB.",
            new[] { new V("Đen", 3590000, 12, A(("color","black"),("connectivity","wireless"),("surround","7.1"))) }),
        new("Tai nghe gaming", "HyperX Cloud Stinger 2", "HyperX", 990000,
            "Tai nghe gaming có dây giá rẻ, nhẹ, micro xoay tắt tiếng, phù hợp người mới và phòng net.",
            new[] { new V("Đen", 990000, 35, A(("color","black"),("connectivity","wired"),("surround","stereo"))) }),

        // ---------- Màn hình gaming ----------
        new("Màn hình gaming", "ASUS ROG Swift PG27AQN", "ASUS", 17990000,
            "Màn hình gaming 27 inch 2K 360Hz tấm nền IPS, phản hồi 1ms, G-Sync, dành cho esports chuyên nghiệp.",
            new[] { new V("27 inch 2K 360Hz", 17990000, 5, A(("size","27"),("resolution","2560x1440"),("refresh","360Hz"),("panel","IPS"))) }),
        new("Màn hình gaming", "LG UltraGear 27GP850-B", "LG", 8490000,
            "Màn hình gaming 27 inch 2K 165Hz Nano IPS, phản hồi 1ms, hỗ trợ G-Sync và FreeSync.",
            new[] { new V("27 inch 2K 165Hz", 8490000, 10, A(("size","27"),("resolution","2560x1440"),("refresh","165Hz"),("panel","IPS"))) }),
        new("Màn hình gaming", "Samsung Odyssey G5 32", "Samsung", 6990000,
            "Màn hình cong 32 inch 2K 165Hz tấm nền VA, độ cong 1000R, FreeSync Premium, trải nghiệm đắm chìm.",
            new[] { new V("32 inch 2K 165Hz", 6990000, 12, A(("size","32"),("resolution","2560x1440"),("refresh","165Hz"),("panel","VA"))) }),
        new("Màn hình gaming", "AOC 24G2SP", "AOC", 3490000,
            "Màn hình gaming 24 inch Full HD 165Hz tấm nền IPS, phản hồi 1ms, giá tốt cho người mới chơi FPS.",
            new[] { new V("24 inch FHD 165Hz", 3490000, 25, A(("size","24"),("resolution","1920x1080"),("refresh","165Hz"),("panel","IPS"))) }),
        new("Màn hình gaming", "Dell Alienware AW2725DF", "Dell", 16990000,
            "Màn hình QD-OLED 27 inch 2K 360Hz, phản hồi 0.03ms, màu sắc rực rỡ, độ tương phản vô cực.",
            new[] { new V("27 inch 2K 360Hz OLED", 16990000, 4, A(("size","27"),("resolution","2560x1440"),("refresh","360Hz"),("panel","OLED"))) }),

        // ---------- Ghế gaming ----------
        new("Ghế gaming", "Secretlab Titan Evo", "Secretlab", 13990000,
            "Ghế gaming cao cấp, tựa lưng ngả 165 độ, đệm lumbar tích hợp, bọc da Neo hybrid hoặc vải SoftWeave.",
            new[] { new V("Size R", 13990000, 5, A(("size","regular"),("material","Neo Hybrid"))),
                    new V("Size L", 14490000, 4, A(("size","large"),("material","Neo Hybrid"))) }),
        new("Ghế gaming", "DXRacer Formula Series", "DXRacer", 6490000,
            "Ghế gaming kiểu đua, khung thép, tựa lưng ngả được, tay vịn 4D, bọc da PU bền đẹp.",
            new[] { new V("Đen đỏ", 6490000, 8, A(("color","red"),("material","PU"))) }),
        new("Ghế gaming", "E-Dra Hercules EGC203", "E-Dra", 3290000,
            "Ghế gaming giá tốt cho học sinh sinh viên, tựa lưng ngả, gối đầu và gối lưng đi kèm, chân xoay chắc chắn.",
            new[] { new V("Đen xanh", 3290000, 20, A(("color","blue"),("material","PU"))),
                    new V("Đen đỏ", 3290000, 18, A(("color","red"),("material","PU"))) }),
        new("Ghế gaming", "Razer Iskur V2", "Razer", 12490000,
            "Ghế gaming công thái học hỗ trợ lưng tối ưu, đệm lumbar tích hợp, tựa đầu điều chỉnh, bọc da tổng hợp.",
            new[] { new V("Đen xanh lá", 12490000, 4, A(("color","green"),("material","PU"))) }),

        // ---------- Lót chuột ----------
        new("Lót chuột", "SteelSeries QcK Heavy XXL", "SteelSeries", 890000,
            "Lót chuột cỡ lớn bề mặt vải mịn, đế cao su dày, tối ưu độ kiểm soát cho FPS và MMO.",
            new[] { new V("XXL 900x400", 890000, 40, A(("size","900x400"),("surface","cloth"))) }),
        new("Lót chuột", "Razer Gigantus V2 Large", "Razer", 590000,
            "Lót chuột bề mặt vải siêu mịn, đế cao su chống trượt, tối ưu cho cảm biến quang và laser.",
            new[] { new V("L 450x400", 590000, 45, A(("size","450x400"),("surface","cloth"))) }),
        new("Lót chuột", "Logitech G640", "Logitech", 790000,
            "Lót chuột vải cỡ lớn cho DPI thấp, bề mặt đồng nhất, đế cao su ổn định, dành cho FPS.",
            new[] { new V("L 460x400", 790000, 30, A(("size","460x400"),("surface","cloth"))) }),
        new("Lót chuột", "Artisan Hayate Otsu", "Artisan", 1290000,
            "Lót chuột cao cấp Nhật Bản, bề mặt cân bằng giữa tốc độ và kiểm soát, được dân FPS chuyên nghiệp ưa chuộng.",
            new[] { new V("M Soft", 1290000, 10, A(("size","420x330"),("surface","cloth"))),
                    new V("L Soft", 1490000, 8, A(("size","490x420"),("surface","cloth"))) }),
        new("Lót chuột", "Corsair MM700 RGB Extended", "Corsair", 1190000,
            "Lót chuột cỡ lớn có đèn RGB viền, bề mặt vải mịn, tích hợp cổng USB, hợp với bàn phím và chuột Corsair.",
            new[] { new V("Extended 930x400", 1190000, 14, A(("size","930x400"),("surface","cloth"))) }),
    };
}
