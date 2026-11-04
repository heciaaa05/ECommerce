namespace ECommerce.Domain.Entities;

public class ProductImage : BaseEntity
{
    public string Url { get; set; } = default!;
    public bool IsPrimary { get; set; } = false;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;
}
