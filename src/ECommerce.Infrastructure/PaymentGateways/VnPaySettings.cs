namespace ECommerce.Infrastructure.PaymentGateways;

public class VnPaySettings
{
    public string TmnCode { get; set; } = default!;
    public string HashSecret { get; set; } = default!;
    public string BaseUrl { get; set; } = default!;
    public string ReturnUrl { get; set; } = default!;
    public string Version { get; set; } = "2.1.0";
    public string CurrCode { get; set; } = "VND";
    public string Locale { get; set; } = "vn";
}
