namespace LudusKids.Api.Dtos;

public record RegisterRequest(string Email, string Password);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string Token, string Email, string Role);

public record UserResponse(string Email, string Role);
