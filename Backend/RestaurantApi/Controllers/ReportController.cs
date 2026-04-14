using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Data;
using RestaurantApi.DTOs;
using RestaurantApi.Models;

namespace RestaurantApi.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReportController(AppDbContext db) => _db = db;

    [HttpGet("daily")]
    public async Task<IActionResult> Daily([FromQuery] DateOnly? date)
    {
        var target = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = target.ToDateTime(TimeOnly.MinValue);
        var end = target.ToDateTime(TimeOnly.MaxValue);

        var orders = await _db.Orders
            .Where(o => o.CreatedAt >= start && o.CreatedAt <= end)
            .ToListAsync();

        var hourlyStats = orders
            .GroupBy(o => o.CreatedAt.Hour)
            .Select(g => new HourlyStatDto(
                g.Key,
                g.Count(),
                g.Where(o => o.Status == OrderStatus.Paid).Sum(o => o.TotalAmount)
            ))
            .OrderBy(h => h.Hour)
            .ToList();

        var report = new DailyReportDto(
            target,
            orders.Where(o => o.Status == OrderStatus.Paid).Sum(o => o.TotalAmount),
            orders.Count,
            orders.Count(o => o.Status == OrderStatus.Paid),
            orders.Count(o => o.Status == OrderStatus.Cancelled),
            hourlyStats
        );

        return Ok(report);
    }

    [HttpGet("monthly")]
    public async Task<IActionResult> Monthly([FromQuery] int? year, [FromQuery] int? month)
    {
        var y = year ?? DateTime.UtcNow.Year;
        var m = month ?? DateTime.UtcNow.Month;
        var start = new DateTime(y, m, 1);
        var end = start.AddMonths(1);

        var orders = await _db.Orders
            .Where(o => o.CreatedAt >= start && o.CreatedAt < end)
            .ToListAsync();

        var dailyStats = orders
            .GroupBy(o => DateOnly.FromDateTime(o.CreatedAt))
            .Select(g => new DailySummaryDto(
                g.Key,
                g.Count(),
                g.Where(o => o.Status == OrderStatus.Paid).Sum(o => o.TotalAmount)
            ))
            .OrderBy(d => d.Date)
            .ToList();

        var report = new MonthlyReportDto(
            y, m,
            orders.Where(o => o.Status == OrderStatus.Paid).Sum(o => o.TotalAmount),
            orders.Count,
            dailyStats
        );

        return Ok(report);
    }

    [HttpGet("menu-stats")]
    public async Task<IActionResult> MenuStats(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate)
    {
        var start = startDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate?.ToDateTime(TimeOnly.MaxValue) ?? DateTime.UtcNow;

        var stats = await _db.OrderItems
            .Include(oi => oi.Order)
            .Where(oi => oi.Order!.Status == OrderStatus.Paid
                      && oi.Order.CreatedAt >= start
                      && oi.Order.CreatedAt <= end)
            .GroupBy(oi => new { oi.MenuItemId, oi.ItemName })
            .Select(g => new MenuStatDto(
                g.Key.MenuItemId ?? 0,
                g.Key.ItemName,
                g.Sum(oi => oi.Quantity),
                g.Sum(oi => oi.ItemPrice * oi.Quantity)
            ))
            .OrderByDescending(s => s.TotalQuantity)
            .Take(20)
            .ToListAsync();

        return Ok(stats);
    }
}
