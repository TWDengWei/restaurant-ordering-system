using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Data;
using RestaurantApi.DTOs;
using RestaurantApi.Models;

namespace RestaurantApi.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var todayOrders = await _db.Orders
            .Where(o => o.CreatedAt >= today && o.CreatedAt < tomorrow)
            .ToListAsync();

        var todayRevenue = todayOrders
            .Where(o => o.Status == OrderStatus.Paid)
            .Sum(o => o.TotalAmount);

        var todayOrderCount = todayOrders.Count;
        var pendingCount = todayOrders.Count(o => o.Status == OrderStatus.Pending);

        var activeTableIds = await _db.Orders
            .Where(o => o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Unpaid)
            .Select(o => o.TableId)
            .Distinct()
            .CountAsync();

        var recentOrders = await _db.Orders
            .Include(o => o.Table)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .ToListAsync();

        var dto = new DashboardDto(
            todayRevenue,
            todayOrderCount,
            pendingCount,
            activeTableIds,
            recentOrders.Select(o => new OrderSummaryDto(
                o.Id, o.TableId, o.Table?.Name ?? $"桌 {o.TableId}",
                o.Status.ToString(), GetStatusLabel(o.Status),
                o.TotalAmount, o.CreatedAt,
                o.OrderItems.Sum(oi => oi.Quantity)
            )).ToList()
        );

        return Ok(dto);
    }

    private static string GetStatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "待確認",
        OrderStatus.Confirmed => "進行中",
        OrderStatus.Unpaid => "未結帳",
        OrderStatus.Paid => "已結帳",
        OrderStatus.Cancelled => "已取消",
        _ => "未知"
    };
}
