using System.Net;
using System.Security.Cryptography;
using System.Text;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using Microsoft.Extensions.Options;

// Tên namespace là PaymentGateways (không phải "Payment") để không đụng với entity Payment của Domain
namespace ECommerce.Infrastructure.PaymentGateways;

public class VnPayGateway : IPaymentGateway
{
    private readonly VnPaySettings _settings;

    public VnPayGateway(IOptions<VnPaySettings> settings)
    {
        _settings = settings.Value;
    }

    public string CreatePaymentUrl(Order order, string ipAddress)
    {
        var now = DateTime.Now; // VNPay yêu cầu giờ local (GMT+7), không phải UTC
        var expire = now.AddMinutes(15);

        var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _settings.Version,
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _settings.TmnCode,
            ["vnp_Amount"] = ((long)(order.TotalAmount * 100)).ToString(),
            ["vnp_CurrCode"] = _settings.CurrCode,
            ["vnp_TxnRef"] = order.Id.ToString("N"),
            ["vnp_OrderInfo"] = $"Thanh toan don hang {order.Id.ToString("N")[..8]}",
            ["vnp_OrderType"] = "other",
            ["vnp_Locale"] = _settings.Locale,
            ["vnp_ReturnUrl"] = _settings.ReturnUrl,
            ["vnp_IpAddr"] = ipAddress,
            ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss"),
            ["vnp_ExpireDate"] = expire.ToString("yyyyMMddHHmmss")
        };

        var signData = BuildSignData(vnpParams);
        var secureHash = HmacSha512(_settings.HashSecret, signData);

        // Chuỗi dùng để ký chính là query string gửi đi (đã URL-encode), cộng thêm vnp_SecureHash ở cuối
        return $"{_settings.BaseUrl}?{signData}&vnp_SecureHash={secureHash}";
    }

    public PaymentCallbackResult VerifyCallback(IDictionary<string, string> queryParams)
    {
        var receivedHash = GetOrDefault(queryParams, "vnp_SecureHash");

        var dataToVerify = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in queryParams)
        {
            // Chỉ lấy các tham số vnp_*, bỏ chính chữ ký
            if (!kv.Key.StartsWith("vnp_")) continue;
            if (kv.Key is "vnp_SecureHash" or "vnp_SecureHashType") continue;
            if (string.IsNullOrEmpty(kv.Value)) continue;
            dataToVerify[kv.Key] = kv.Value;
        }

        var signData = BuildSignData(dataToVerify);
        var computedHash = HmacSha512(_settings.HashSecret, signData);

        var isValid = !string.IsNullOrEmpty(receivedHash)
                      && string.Equals(computedHash, receivedHash, StringComparison.OrdinalIgnoreCase);

        var responseCode = GetOrDefault(queryParams, "vnp_ResponseCode");
        var transactionStatus = GetOrDefault(queryParams, "vnp_TransactionStatus");
        var isSuccess = responseCode == "00" && transactionStatus == "00";

        var txnRef = GetOrDefault(queryParams, "vnp_TxnRef");
        var amount = long.TryParse(GetOrDefault(queryParams, "vnp_Amount", "0"), out var amt) ? amt : 0;

        return new PaymentCallbackResult(isValid, isSuccess, txnRef, responseCode, amount);
    }

    private static string BuildSignData(SortedDictionary<string, string> data)
        => string.Join("&", data
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}"));

    private static string HmacSha512(string secret, string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }

    // IDictionary không có GetValueOrDefault (chỉ IReadOnlyDictionary mới có) nên dùng helper này
    private static string GetOrDefault(IDictionary<string, string> dict, string key, string defaultValue = "")
        => dict.TryGetValue(key, out var value) ? value : defaultValue;
}
