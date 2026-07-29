using LudusKids.Api.Dtos;
using LudusKids.Api.Models;
using LudusKids.Api.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LudusKids.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(LudusKidsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetAll()
    {
        var categories = await db.Categories
            .Select(c => new CategoryResponse(c.Id, c.Name))
            .ToListAsync();

        return Ok(categories);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(CategoryRequest request)
    {
        var category = new Category { Name = request.Name };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var response = new CategoryResponse(category.Id, category.Name);
        return CreatedAtAction(nameof(GetAll), response);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, CategoryRequest request)
    {
        var category = await db.Categories.FindAsync(id);
        if (category is null)
            return NotFound();

        category.Name = request.Name;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await db.Categories.FindAsync(id);
        if (category is null)
            return NotFound();

        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
