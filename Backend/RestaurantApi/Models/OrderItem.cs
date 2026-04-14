namespace RestaurantApi.Models;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int? MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal ItemPrice { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
}
