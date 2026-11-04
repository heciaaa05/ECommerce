using ECommerce.Application.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Cart.Commands;

public record UpdateCartItemCommand(Guid UserId, Guid ProductVariantId, int Quantity) : IRequest<Unit>;

public class UpdateCartItemCommandHandler : IRequestHandler<UpdateCartItemCommand, Unit>
{
    private readonly ICartService _cartService;

    public UpdateCartItemCommandHandler(ICartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<Unit> Handle(UpdateCartItemCommand request, CancellationToken ct)
    {
        await _cartService.UpdateItemAsync(request.UserId, request.ProductVariantId, request.Quantity);
        return Unit.Value;
    }
}
