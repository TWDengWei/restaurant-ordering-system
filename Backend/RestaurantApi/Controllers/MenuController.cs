using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Data;
using RestaurantApi.DTOs;
using RestaurantApi.Models;

namespace RestaurantApi.Controllers;

[ApiController]
[Route("api/menu")]
public class MenuController : ControllerBase
{
    private readonly AppDbContext _db;

    public MenuController(AppDbContext db) => _db = db;

    // 公開端點：給客戶點餐用（按分類分組）
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicMenu()
    {
        var categories = await _db.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .Include(c => c.MenuItems.Where(m => m.IsAvailable))
            .ToListAsync();

        var result = categories
            .Where(c => c.MenuItems.Any())
            .Select(c => new MenuCategoryWithItemsDto(
                c.Id, c.Name, c.SortOrder,
                c.MenuItems.OrderBy(m => m.SortOrder).ThenBy(m => m.Id)
                    .Select(m => ToDto(m, c.Name)).ToList()
            )).ToList();

        return Ok(result);
    }

    // 後台管理：取得所有菜品
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll([FromQuery] int? categoryId)
    {
        var query = _db.MenuItems.Include(m => m.Category).AsQueryable();
        if (categoryId.HasValue)
            query = query.Where(m => m.CategoryId == categoryId);

        var items = await query
            .OrderBy(m => m.SortOrder).ThenBy(m => m.Id)
            .ToListAsync();

        return Ok(items.Select(m => ToDto(m, m.Category?.Name)));
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.MenuItems.Include(m => m.Category).FirstOrDefaultAsync(m => m.Id == id);
        if (item == null) return NotFound();
        return Ok(ToDto(item, item.Category?.Name));
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateMenuItemRequest request)
    {
        var item = new MenuItem
        {
            CategoryId = request.CategoryId,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            ImageBase64 = request.ImageBase64,
            IsAvailable = request.IsAvailable,
            SortOrder = request.SortOrder
        };
        _db.MenuItems.Add(item);
        await _db.SaveChangesAsync();

        var category = item.CategoryId.HasValue
            ? await _db.Categories.FindAsync(item.CategoryId.Value) : null;
        return Ok(ToDto(item, category?.Name));
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMenuItemRequest request)
    {
        var item = await _db.MenuItems.FindAsync(id);
        if (item == null) return NotFound();

        item.CategoryId = request.CategoryId;
        item.Name = request.Name;
        item.Description = request.Description;
        item.Price = request.Price;
        item.ImageBase64 = request.ImageBase64;
        item.IsAvailable = request.IsAvailable;
        item.SortOrder = request.SortOrder;
        await _db.SaveChangesAsync();

        var category = item.CategoryId.HasValue
            ? await _db.Categories.FindAsync(item.CategoryId.Value) : null;
        return Ok(ToDto(item, category?.Name));
    }

    [HttpPatch("{id}/toggle")]
    [Authorize]
    public async Task<IActionResult> ToggleAvailability(int id)
    {
        var item = await _db.MenuItems.FindAsync(id);
        if (item == null) return NotFound();

        item.IsAvailable = !item.IsAvailable;
        await _db.SaveChangesAsync();
        return Ok(new { id = item.Id, isAvailable = item.IsAvailable });
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.MenuItems.FindAsync(id);
        if (item == null) return NotFound();

        _db.MenuItems.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { message = "菜品已刪除" });
    }

    private static MenuItemDto ToDto(MenuItem m, string? categoryName) =>
        new(m.Id, m.CategoryId, categoryName, m.Name, m.Description,
            m.Price, m.ImageBase64, m.IsAvailable, m.SortOrder);
}
