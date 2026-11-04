using ECommerce.Application.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Cart.Commands;

public record AddToCartCommand(Guid UserId, Guid ProductVariantId, int Quantity) : IRequest<Unit>;

public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, Unit>
{
    private readonly ICartService _cartService;

    public AddToCartCommandHandler(ICartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<Unit> Handle(AddToCartCommand request, CancellationToken ct)
    {
        await _cartService.AddItemAsync(request.UserId, request.ProductVariantId, request.Quantity);
        return Unit.Value;
    }
}
