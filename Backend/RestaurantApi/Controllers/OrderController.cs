using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Data;
using RestaurantApi.DTOs;
using RestaurantApi.Models;

namespace RestaurantApi.Controllers;

[ApiController]
[Route("api/orders")]
public class OrderController : ControllerBase
{
    private readonly AppDbContext _db;

    public OrderController(AppDbContext db) => _db = db;

    // 客戶建立訂單（公開端點）
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        var table = await _db.Tables.FindAsync(request.TableId);
        if (table == null) return BadRequest(new { message = "桌位不存在" });

        if (request.Items == null || request.Items.Count == 0)
            return BadRequest(new { message = "請至少選擇一項菜品" });

        var order = new Order
        {
            TableId = request.TableId,
            Note = request.Note,
            Status = OrderStatus.Pending
        };

        decimal total = 0;
        foreach (var itemReq in request.Items)
        {
            var menuItem = await _db.MenuItems.FindAsync(itemReq.MenuItemId);
            if (menuItem == null || !menuItem.IsAvailable)
                return BadRequest(new { message = $"菜品 ID {itemReq.MenuItemId} 不存在或已下架" });

            var orderItem = new OrderItem
            {
                MenuItemId = menuItem.Id,
                ItemName = menuItem.Name,
                ItemPrice = menuItem.Price,
                Quantity = itemReq.Quantity,
                Note = itemReq.Note
            };
            order.OrderItems.Add(orderItem);
            total += menuItem.Price * itemReq.Quantity;
        }

        order.TotalAmount = total;
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return Ok(new { orderId = order.Id, message = "訂單已送出，請至櫃台確認", totalAmount = total });
    }

    // 後台：取得所有訂單
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] int? tableId,
        [FromQuery] DateOnly? date)
    {
        var query = _db.Orders
            .Include(o => o.Table)
            .Include(o => o.OrderItems)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
            query = query.Where(o => o.Status == parsedStatus);

        if (tableId.HasValue)
            query = query.Where(o => o.TableId == tableId);

        if (date.HasValue)
        {
            // CreatedAt 已存台灣時間(UTC+8)，直接比對台灣日期即可
            var start = date.Value.ToDateTime(TimeOnly.MinValue);
            var end   = date.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(o => o.CreatedAt >= start && o.CreatedAt <= end);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Ok(orders.Select(ToSummaryDto));
    }

    // 後台：取得訂單詳情
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Table)
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();
        return Ok(ToDto(order));
    }

    // 確認訂單
    [HttpPut("{id}/confirm")]
    [Authorize]
    public async Task<IActionResult> Confirm(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        if (order.Status != OrderStatus.Pending)
            return BadRequest(new { message = "只能確認待確認狀態的訂單" });

        order.Status = OrderStatus.Confirmed;
        order.ConfirmedAt = DateTime.UtcNow.AddHours(8);
        await _db.SaveChangesAsync();
        return Ok(new { message = "訂單已確認", status = "Confirmed" });
    }

    // 標記用餐中（後結帳）
    [HttpPut("{id}/dining")]
    [Authorize]
    public async Task<IActionResult> SetDining(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        if (order.Status != OrderStatus.Confirmed)
            return BadRequest(new { message = "只能將已確認訂單標記為用餐中" });

        order.Status = OrderStatus.Unpaid;
        await _db.SaveChangesAsync();
        return Ok(new { message = "已標記為用餐中（未結帳）", status = "Unpaid" });
    }

    // 結帳
    [HttpPut("{id}/pay")]
    [Authorize]
    public async Task<IActionResult> Pay(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        if (order.Status != OrderStatus.Confirmed && order.Status != OrderStatus.Unpaid)
            return BadRequest(new { message = "訂單狀態不可結帳" });

        order.Status = OrderStatus.Paid;
        order.PaidAt = DateTime.UtcNow.AddHours(8);
        await _db.SaveChangesAsync();
        return Ok(new { message = "結帳完成", status = "Paid" });
    }

    // 取消訂單
    [HttpPut("{id}/cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        if (order.Status == OrderStatus.Paid)
            return BadRequest(new { message = "已結帳的訂單不可取消" });

        order.Status = OrderStatus.Cancelled;
        await _db.SaveChangesAsync();
        return Ok(new { message = "訂單已取消", status = "Cancelled" });
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

    private static OrderDto ToDto(Order o) => new(
        o.Id,
        o.TableId,
        o.Table?.Name ?? $"桌 {o.TableId}",
        o.Status.ToString(),
        GetStatusLabel(o.Status),
        o.TotalAmount,
        o.Note,
        o.CreatedAt,
        o.ConfirmedAt,
        o.PaidAt,
        o.OrderItems.Select(oi => new OrderItemDto(
            oi.Id, oi.MenuItemId, oi.ItemName, oi.ItemPrice,
            oi.Quantity, oi.Note, oi.ItemPrice * oi.Quantity
        )).ToList()
    );

    private static OrderSummaryDto ToSummaryDto(Order o) => new(
        o.Id,
        o.TableId,
        o.Table?.Name ?? $"桌 {o.TableId}",
        o.Status.ToString(),
        GetStatusLabel(o.Status),
        o.TotalAmount,
        o.Note,
        o.CreatedAt,
        o.OrderItems.Sum(oi => oi.Quantity),
        o.OrderItems.Select(oi => new OrderItemDto(
            oi.Id, oi.MenuItemId, oi.ItemName, oi.ItemPrice,
            oi.Quantity, oi.Note, oi.ItemPrice * oi.Quantity
        )).ToList()
    );
}
