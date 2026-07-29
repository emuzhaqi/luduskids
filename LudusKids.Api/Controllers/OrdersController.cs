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
[Route("api/orders")]
public class OrdersController(LudusKidsDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create()
    {
        var cartItems = await db.CartItems
            .Where(c => c.UserId == CurrentUserId)
            .Include(c => c.Product)
            .ToListAsync();

        if (cartItems.Count == 0)
            return BadRequest("Cart is empty.");

        var order = new Order
        {
            UserId = CurrentUserId,
            Status = "Pending",
            TotalAmount = cartItems.Sum(c => c.Product!.Price * c.Quantity),
            OrderItems = cartItems.Select(c => new OrderItem
            {
                ProductId = c.ProductId,
                Quantity = c.Quantity,
                UnitPrice = c.Product!.Price,
            }).ToList(),
        };

        db.Orders.Add(order);
        db.CartItems.RemoveRange(cartItems);
        await db.SaveChangesAsync();

        var userEmail = User.FindFirstValue(ClaimTypes.Email)!;
        var items = cartItems.Select(c => new OrderItemResponse(c.ProductId, c.Product!.Name, c.Quantity, c.Product.Price)).ToList();
        var response = new OrderResponse(order.Id, order.CreatedAt, order.Status, order.TotalAmount, userEmail, items);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAll()
    {
        var query = db.Orders.Include(o => o.User).Include(o => o.OrderItems).ThenInclude(oi => oi.Product).AsQueryable();
        if (!User.IsInRole("Admin"))
            query = query.Where(o => o.UserId == CurrentUserId);

        var orders = await query.ToListAsync();
        return Ok(orders.Select(o => ToResponse(o)));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponse>> GetById(int id)
    {
        var order = await db.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
            return NotFound();

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && order.UserId != CurrentUserId)
            return Forbid();

        return Ok(ToResponse(order));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusRequest request)
    {
        var order = await db.Orders.FindAsync(id);
        if (order is null)
            return NotFound();

        order.Status = request.Status;
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static OrderResponse ToResponse(Order order)
    {
        var items = order.OrderItems
            .Select(oi => new OrderItemResponse(oi.ProductId, oi.Product!.Name, oi.Quantity, oi.UnitPrice))
            .ToList();

        return new OrderResponse(order.Id, order.CreatedAt, order.Status, order.TotalAmount, order.User!.Email, items);
    }
}
