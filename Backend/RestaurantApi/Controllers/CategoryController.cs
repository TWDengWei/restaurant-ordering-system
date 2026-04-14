using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Data;
using RestaurantApi.DTOs;
using RestaurantApi.Models;

namespace RestaurantApi.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoryController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoryController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _db.Categories
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .Select(c => new CategoryDto(
                c.Id, c.Name, c.SortOrder, c.IsActive,
                c.MenuItems.Count(m => m.IsAvailable)))
            .ToListAsync();
        return Ok(categories);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        var category = new Category { Name = request.Name, SortOrder = request.SortOrder };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return Ok(new CategoryDto(category.Id, category.Name, category.SortOrder, category.IsActive, 0));
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest request)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound();

        category.Name = request.Name;
        category.SortOrder = request.SortOrder;
        category.IsActive = request.IsActive;
        await _db.SaveChangesAsync();

        var itemCount = await _db.MenuItems.CountAsync(m => m.CategoryId == id && m.IsAvailable);
        return Ok(new CategoryDto(category.Id, category.Name, category.SortOrder, category.IsActive, itemCount));
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound();

        var hasItems = await _db.MenuItems.AnyAsync(m => m.CategoryId == id);
        if (hasItems)
            return BadRequest(new { message = "此分類下仍有菜品，請先移除或移動菜品" });

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return Ok(new { message = "分類已刪除" });
    }
}
