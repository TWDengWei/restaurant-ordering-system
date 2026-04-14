namespace RestaurantApi.Models;

public class MenuItem
{
    public int Id { get; set; }
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageBase64 { get; set; }
    public bool IsAvailable { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(8);
}
