using ECommerce.API.Extensions;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Features.Payments.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/payments/vnpay")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("create/{orderId}")]
    [Authorize]
    public async Task<ActionResult<PaymentUrlResponse>> CreatePaymentUrl(Guid orderId)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        if (ip == "::1") ip = "127.0.0.1"; // chuẩn hoá IPv6 localhost cho VNPay

        try
        {
            var result = await _mediator.Send(new CreatePaymentUrlCommand(User.GetUserId(), orderId, ip));
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // IPN: VNPay server gọi trực tiếp (không có JWT) nên phải AllowAnonymous.
    // Đây là nơi DUY NHẤT được cập nhật trạng thái đơn hàng.
    [HttpGet("ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> Ipn()
    {
        var queryParams = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
        var (_, rspCode, message) = await _mediator.Send(new ProcessPaymentCallbackCommand(queryParams));

        // VNPay yêu cầu đúng key "RspCode" / "Message" (PascalCase) - dùng Dictionary để JSON không bị đổi sang camelCase
        return Ok(new Dictionary<string, string>
        {
            ["RspCode"] = rspCode,
            ["Message"] = message
        });
    }

    // Return URL: chỉ để hiển thị kết quả cho người dùng, KHÔNG dùng để cập nhật DB (user có thể tự sửa URL)
    [HttpGet("return")]
    [AllowAnonymous]
    public IActionResult Return()
    {
        var responseCode = Request.Query["vnp_ResponseCode"].ToString();
        var isSuccess = responseCode == "00";

        return Ok(new
        {
            success = isSuccess,
            message = isSuccess
                ? "Thanh toán thành công. Đơn hàng đang được xác nhận."
                : "Thanh toán thất bại hoặc bị huỷ."
        });
    }
}
