namespace RestaurantApi.DTOs;

public record LoginRequest(string Username, string Password);

public record LoginResponse(string Token, string Username, DateTime ExpiresAt);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
