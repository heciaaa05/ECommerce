using ECommerce.Application.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Cart.Commands;

public record RemoveCartItemCommand(Guid UserId, Guid ProductVariantId) : IRequest<Unit>;

public class RemoveCartItemCommandHandler : IRequestHandler<RemoveCartItemCommand, Unit>
{
    private readonly ICartService _cartService;

    public RemoveCartItemCommandHandler(ICartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<Unit> Handle(RemoveCartItemCommand request, CancellationToken ct)
    {
        await _cartService.RemoveItemAsync(request.UserId, request.ProductVariantId);
        return Unit.Value;
    }
}
