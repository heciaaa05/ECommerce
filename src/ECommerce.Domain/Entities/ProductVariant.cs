namespace ECommerce.Domain.Entities;

public class ProductVariant : BaseEntity
{
    public string Sku { get; set; } = default!;
    public string VariantName { get; set; } = default!;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }

    // Thuộc tính linh hoạt theo loại gaming gear, lưu dạng JSON
    // vd bàn phím: {"switch":"Red","layout":"TKL","connectivity":"Wired"}
    // vd chuột:   {"dpi":"16000","sensor":"PixArt 3389","weight":"63g"}
    public string AttributesJson { get; set; } = "{}";

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;
}
