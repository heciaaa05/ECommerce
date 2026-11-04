using ECommerce.Application.Features.Orders.Commands;
using MediatR;

namespace ECommerce.API.BackgroundJobs;

// Mỗi phút huỷ các đơn Pending quá hạn thanh toán (15 phút) và hoàn trả tồn kho.
// BackgroundService là Singleton còn DbContext là Scoped nên phải tạo scope mới mỗi lần chạy.
public class OrderExpirationJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderExpirationJob> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    public OrderExpirationJob(IServiceScopeFactory scopeFactory, ILogger<OrderExpirationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var count = await mediator.Send(new ExpireOrdersCommand(), stoppingToken);

                if (count > 0)
                    _logger.LogInformation("Đã huỷ {Count} đơn hàng quá hạn thanh toán.", count);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chạy job huỷ đơn hết hạn.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
