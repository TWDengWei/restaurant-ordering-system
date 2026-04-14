namespace RestaurantApi.DTOs;

public record CategoryDto(int Id, string Name, int SortOrder, bool IsActive, int ItemCount);

public record CreateCategoryRequest(string Name, int SortOrder = 0);

public record UpdateCategoryRequest(string Name, int SortOrder, bool IsActive);
