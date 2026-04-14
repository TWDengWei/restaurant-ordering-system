namespace RestaurantApi.DTOs;

public record DashboardDto(
    decimal TodayRevenue,
    int TodayOrderCount,
    int PendingOrderCount,
    int ActiveTableCount,
    List<OrderSummaryDto> RecentOrders
);

public record DailyReportDto(
    DateOnly Date,
    decimal TotalRevenue,
    int TotalOrders,
    int PaidOrders,
    int CancelledOrders,
    List<HourlyStatDto> HourlyStats
);

public record HourlyStatDto(int Hour, int OrderCount, decimal Revenue);

public record MonthlyReportDto(
    int Year,
    int Month,
    decimal TotalRevenue,
    int TotalOrders,
    List<DailySummaryDto> DailyStats
);

public record DailySummaryDto(DateOnly Date, int OrderCount, decimal Revenue);

public record MenuStatDto(
    int MenuItemId,
    string ItemName,
    int TotalQuantity,
    decimal TotalRevenue
);
