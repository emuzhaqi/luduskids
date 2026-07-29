using LudusKids.Api.Dtos;
using LudusKids.Api.Models;
using LudusKids.Api.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LudusKids.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(LudusKidsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetAll()
    {
        var products = await db.Products
            .Include(p => p.Category)
            .Select(p => new ProductResponse(
                p.Id, p.Name, p.Description, p.Price, p.ImageUrl, p.StockQuantity, p.CategoryId, p.Category!.Name))
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductResponse>> GetById(int id)
    {
        var product = await db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
            return NotFound();

        return Ok(ToResponse(product));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(ProductRequest request)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId))
            return BadRequest("CategoryId does not exist.");

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            ImageUrl = request.ImageUrl,
            StockQuantity = request.StockQuantity,
            CategoryId = request.CategoryId,
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();
        await db.Entry(product).Reference(p => p.Category).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToResponse(product));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, ProductRequest request)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null)
            return NotFound();

        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId))
            return BadRequest("CategoryId does not exist.");

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.ImageUrl = request.ImageUrl;
        product.StockQuantity = request.StockQuantity;
        product.CategoryId = request.CategoryId;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null)
            return NotFound();

        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static ProductResponse ToResponse(Product p) => new(
        p.Id, p.Name, p.Description, p.Price, p.ImageUrl, p.StockQuantity, p.CategoryId, p.Category!.Name);
}
