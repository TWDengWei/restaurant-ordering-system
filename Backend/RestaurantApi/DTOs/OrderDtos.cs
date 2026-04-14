using RestaurantApi.Models;

namespace RestaurantApi.DTOs;

public record CreateOrderRequest(
    int TableId,
    string? Note,
    List<CreateOrderItemRequest> Items
);

public record CreateOrderItemRequest(
    int MenuItemId,
    int Quantity,
    string? Note
);

public record OrderItemDto(
    int Id,
    int? MenuItemId,
    string ItemName,
    decimal ItemPrice,
    int Quantity,
    string? Note,
    decimal Subtotal
);

public record OrderDto(
    int Id,
    int TableId,
    string TableName,
    string Status,
    string StatusLabel,
    decimal TotalAmount,
    string? Note,
    DateTime CreatedAt,
    DateTime? ConfirmedAt,
    DateTime? PaidAt,
    List<OrderItemDto> Items
);

public record OrderSummaryDto(
    int Id,
    int TableId,
    string TableName,
    string Status,
    string StatusLabel,
    decimal TotalAmount,
    string? Note,
    DateTime CreatedAt,
    int ItemCount,
    List<OrderItemDto> Items
);
