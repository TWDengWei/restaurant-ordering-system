namespace RestaurantApi.DTOs;

public record MenuItemDto(
    int Id,
    int? CategoryId,
    string? CategoryName,
    string Name,
    string? Description,
    decimal Price,
    string? ImageBase64,
    bool IsAvailable,
    int SortOrder
);

public record CreateMenuItemRequest(
    int? CategoryId,
    string Name,
    string? Description,
    decimal Price,
    string? ImageBase64,
    bool IsAvailable = true,
    int SortOrder = 0
);

public record UpdateMenuItemRequest(
    int? CategoryId,
    string Name,
    string? Description,
    decimal Price,
    string? ImageBase64,
    bool IsAvailable,
    int SortOrder
);

public record MenuCategoryWithItemsDto(int Id, string Name, int SortOrder, List<MenuItemDto> Items);
