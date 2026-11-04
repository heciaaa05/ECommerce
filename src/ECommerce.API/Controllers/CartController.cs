using ECommerce.API.Extensions;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.Features.Cart.Commands;
using ECommerce.Application.Features.Cart.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly IMediator _mediator;

    public CartController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        var result = await _mediator.Send(new GetCartQuery(User.GetUserId()));
        return Ok(result);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddToCartRequest request)
    {
        try
        {
            await _mediator.Send(new AddToCartCommand(User.GetUserId(), request.ProductVariantId, request.Quantity));
            return Ok();
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

    [HttpPut("items")]
    public async Task<IActionResult> UpdateItem([FromBody] UpdateCartItemRequest request)
    {
        await _mediator.Send(new UpdateCartItemCommand(User.GetUserId(), request.ProductVariantId, request.Quantity));
        return NoContent();
    }

    [HttpDelete("items/{productVariantId}")]
    public async Task<IActionResult> RemoveItem(Guid productVariantId)
    {
        await _mediator.Send(new RemoveCartItemCommand(User.GetUserId(), productVariantId));
        return NoContent();
    }
}
