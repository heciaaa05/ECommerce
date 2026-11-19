using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Products.Commands;

public record RecordProductViewCommand(Guid UserId, Guid ProductId) : IRequest<Unit>;

public class RecordProductViewCommandHandler : IRequestHandler<RecordProductViewCommand, Unit>
{
    private static readonly TimeSpan DedupWindow = TimeSpan.FromMinutes(5);

    private readonly IApplicationDbContext _context;

    public RecordProductViewCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(RecordProductViewCommand request, CancellationToken ct)
    {
        var exists = await _context.Products.AnyAsync(p => p.Id == request.ProductId && p.IsActive, ct);
        if (!exists)
            throw new KeyNotFoundException("Sản phẩm không tồn tại.");

        // F5 liên tục không được tính nhiều lượt xem: bỏ qua nếu cùng người, cùng sản phẩm trong 5 phút
        var since = DateTime.UtcNow - DedupWindow;
        var recent = await _context.UserInteractions.AnyAsync(i =>
            i.UserId == request.UserId && i.ProductId == request.ProductId &&
            i.Type == InteractionType.View && i.CreatedAt >= since, ct);
        if (recent) return Unit.Value;

        _context.UserInteractions.Add(new UserInteraction
        {
            UserId = request.UserId,
            ProductId = request.ProductId,
            Type = InteractionType.View
        });
        await _context.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
