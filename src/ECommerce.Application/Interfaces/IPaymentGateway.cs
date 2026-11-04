using ECommerce.Application.DTOs.Payments;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces;

// Abstraction cho cổng thanh toán: thêm Momo/Stripe sau này chỉ cần viết thêm 1 class implement
public interface IPaymentGateway
{
    string CreatePaymentUrl(Order order, string ipAddress);
    PaymentCallbackResult VerifyCallback(IDictionary<string, string> queryParams);
}
