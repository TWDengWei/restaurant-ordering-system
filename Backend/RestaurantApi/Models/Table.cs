namespace RestaurantApi.Models;

public class Table
{
    public int Id { get; set; }
    public int TableNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
