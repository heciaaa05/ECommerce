using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Cart.Queries;

public record GetCartQuery(Guid UserId) : IRequest<CartDto>;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, CartDto>
{
    private readonly ICartService _cartService;

    public GetCartQueryHandler(ICartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<CartDto> Handle(GetCartQuery request, CancellationToken ct)
    {
        return await _cartService.GetCartAsync(request.UserId);
    }
}
