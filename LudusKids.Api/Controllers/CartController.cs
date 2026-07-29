using System.Security.Claims;
using LudusKids.Api.Dtos;
using LudusKids.Api.Models;
using LudusKids.Api.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LudusKids.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/cart/items")]
public class CartController(LudusKidsDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CartItemResponse>>> GetAll()
    {
        var items = await db.CartItems
            .Where(c => c.UserId == CurrentUserId)
            .Include(c => c.Product)
            .Select(c => new CartItemResponse(c.Id, c.ProductId, c.Product!.Name, c.Product.Price, c.Quantity))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<CartItemResponse>> Add(AddCartItemRequest request)
    {
        var product = await db.Products.FindAsync(request.ProductId);
        if (product is null)
            return BadRequest("ProductId does not exist.");

        var existing = await db.CartItems
            .FirstOrDefaultAsync(c => c.UserId == CurrentUserId && c.ProductId == request.ProductId);

        if (existing is not null)
        {
            existing.Quantity += request.Quantity;
        }
        else
        {
            existing = new CartItem { UserId = CurrentUserId, ProductId = request.ProductId, Quantity = request.Quantity };
            db.CartItems.Add(existing);
        }

        await db.SaveChangesAsync();
        return Ok(new CartItemResponse(existing.Id, product.Id, product.Name, product.Price, existing.Quantity));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateQuantity(int id, UpdateCartItemRequest request)
    {
        var item = await db.CartItems.FirstOrDefaultAsync(c => c.Id == id && c.UserId == CurrentUserId);
        if (item is null)
            return NotFound();

        item.Quantity = request.Quantity;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Remove(int id)
    {
        var item = await db.CartItems.FirstOrDefaultAsync(c => c.Id == id && c.UserId == CurrentUserId);
        if (item is null)
            return NotFound();

        db.CartItems.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
