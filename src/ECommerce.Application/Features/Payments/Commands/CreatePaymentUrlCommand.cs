using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Payments.Commands;

public record CreatePaymentUrlCommand(Guid UserId, Guid OrderId, string IpAddress) : IRequest<PaymentUrlResponse>;

public class CreatePaymentUrlCommandHandler : IRequestHandler<CreatePaymentUrlCommand, PaymentUrlResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentGateway _paymentGateway;

    public CreatePaymentUrlCommandHandler(IApplicationDbContext context, IPaymentGateway paymentGateway)
    {
        _context = context;
        _paymentGateway = paymentGateway;
    }

    public async Task<PaymentUrlResponse> Handle(CreatePaymentUrlCommand request, CancellationToken ct)
    {
        var order = await _context.Orders
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == request.UserId, ct)
            ?? throw new KeyNotFoundException("Đơn hàng không tồn tại.");

        if (order.Status != OrderStatus.Pending)
            throw new InvalidOperationException("Đơn hàng không ở trạng thái chờ thanh toán.");

        if (order.Payment is null)
        {
            _context.Payments.Add(new Payment
            {
                OrderId = order.Id,
                Provider = "VNPay",
                Status = PaymentStatus.Pending,
                Amount = order.TotalAmount
            });
            await _context.SaveChangesAsync(ct);
        }

        var url = _paymentGateway.CreatePaymentUrl(order, request.IpAddress);
        return new PaymentUrlResponse(url);
    }
}
