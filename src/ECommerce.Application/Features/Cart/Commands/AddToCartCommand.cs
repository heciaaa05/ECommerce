using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Cart.Commands;

public record AddToCartCommand(Guid UserId, Guid ProductVariantId, int Quantity) : IRequest<Unit>;

public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, Unit>
{
    private readonly ICartService _cartService;
    private readonly IApplicationDbContext _context;

    public AddToCartCommandHandler(ICartService cartService, IApplicationDbContext context)
    {
        _cartService = cartService;
        _context = context;
    }

    public async Task<Unit> Handle(AddToCartCommand request, CancellationToken ct)
    {
        await _cartService.AddItemAsync(request.UserId, request.ProductVariantId, request.Quantity);

        // Ghi nhận hành vi "thêm vào giỏ" cho gợi ý; lỗi ghi log không được làm hỏng việc thêm giỏ
        try
        {
            var productId = await _context.ProductVariants
                .Where(v => v.Id == request.ProductVariantId)
                .Select(v => (Guid?)v.ProductId)
                .FirstOrDefaultAsync(ct);

            if (productId.HasValue)
            {
                _context.UserInteractions.Add(new UserInteraction
                {
                    UserId = request.UserId,
                    ProductId = productId.Value,
                    Type = InteractionType.AddToCart
                });
                await _context.SaveChangesAsync(ct);
            }
        }
        catch (Exception)
        {
            // bỏ qua
        }

        return Unit.Value;
    }
}
