namespace ECommerce.Application.DTOs.Payments;

public record PaymentUrlResponse(string PaymentUrl);

public record PaymentCallbackResult(
    bool IsValidSignature,
    bool IsSuccess,
    string TxnRef,
    string ResponseCode,
    long Amount
);
