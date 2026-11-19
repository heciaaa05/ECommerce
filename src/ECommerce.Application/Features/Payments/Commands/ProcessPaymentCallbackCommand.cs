using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Payments.Commands;

// Chỉ IPN (server-to-server) mới được dùng để cập nhật trạng thái đơn; Return URL chỉ để hiển thị UX.
public record ProcessPaymentCallbackCommand(IDictionary<string, string> QueryParams)
    : IRequest<(bool Success, string RspCode, string Message)>;

public class ProcessPaymentCallbackCommandHandler
    : IRequestHandler<ProcessPaymentCallbackCommand, (bool Success, string RspCode, string Message)>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentGateway _paymentGateway;

    public ProcessPaymentCallbackCommandHandler(IApplicationDbContext context, IPaymentGateway paymentGateway)
    {
        _context = context;
        _paymentGateway = paymentGateway;
    }

    public async Task<(bool Success, string RspCode, string Message)> Handle(
        ProcessPaymentCallbackCommand request, CancellationToken ct)
    {
        var result = _paymentGateway.VerifyCallback(request.QueryParams);

        // 1. Xác thực chữ ký: chống giả mạo callback
        if (!result.IsValidSignature)
            return (false, "97", "Invalid signature");

        // TxnRef được tạo từ Order.Id (định dạng "N" - không có dấu gạch ngang)
        if (!Guid.TryParseExact(result.TxnRef, "N", out var orderId))
            return (false, "01", "Order not found");

        var order = await _context.Orders
            .Include(o => o.Payment)
            .Include(o => o.Items).ThenInclude(i => i.ProductVariant)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null || order.Payment is null)
            return (false, "01", "Order not found");

        // 2. Idempotency: IPN có thể bị VNPay gọi lặp, không xử lý lại đơn đã xác nhận
        if (order.Payment.Status == PaymentStatus.Success)
            return (true, "02", "Order already confirmed");

        // Đơn đã bị huỷ (quá hạn 15 phút, tồn kho đã hoàn) thì không được chuyển sang Paid
        if (order.Status != OrderStatus.Pending)
            return (false, "99", "Order is not pending");

        // 3. Đối chiếu số tiền với DB, không tin tuyệt đối số tiền trong callback
        var expectedAmount = (long)(order.TotalAmount * 100);
        if (result.Amount != expectedAmount)
            return (false, "04", "Invalid amount");

        if (result.IsSuccess)
        {
            order.Payment.Status = PaymentStatus.Success;
            order.Payment.PaidAt = DateTime.UtcNow;
            order.Payment.TransactionRef = result.TxnRef;
            order.Status = OrderStatus.Paid;
            order.UpdatedAt = DateTime.UtcNow;

            // Ghi nhận hành vi "mua" cho gợi ý (mỗi sản phẩm một lần dù mua nhiều biến thể)
            foreach (var productId in order.Items.Select(i => i.ProductVariant.ProductId).Distinct())
            {
                _context.UserInteractions.Add(new UserInteraction
                {
                    UserId = order.UserId,
                    ProductId = productId,
                    Type = InteractionType.Purchase
                });
            }
        }
        else
        {
            order.Payment.Status = PaymentStatus.Failed;
            order.Payment.TransactionRef = result.TxnRef;
        }

        await _context.SaveChangesAsync(ct);
        return (true, "00", "Confirm Success");
    }
}
