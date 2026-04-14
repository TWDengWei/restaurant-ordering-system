namespace RestaurantApi.Models;

public enum OrderStatus
{
    Pending = 0,    // 待確認
    Confirmed = 1,  // 已確認（進行中）
    Unpaid = 2,     // 用餐中（未結帳）
    Paid = 3,       // 已結帳
    Cancelled = 4   // 已取消
}

public class Order
{
    public int Id { get; set; }
    public int TableId { get; set; }
    public Table? Table { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(8);
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
