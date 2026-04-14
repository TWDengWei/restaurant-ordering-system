using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Data;

namespace RestaurantApi.Controllers;

[ApiController]
[Route("api/tables")]
public class TableController : ControllerBase
{
    private readonly AppDbContext _db;

    public TableController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tables = await _db.Tables
            .OrderBy(t => t.TableNumber)
            .Select(t => new { t.Id, t.TableNumber, t.Name })
            .ToListAsync();
        return Ok(tables);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var table = await _db.Tables.FindAsync(id);
        if (table == null) return NotFound(new { message = "桌位不存在" });
        return Ok(new { table.Id, table.TableNumber, table.Name });
    }
}
