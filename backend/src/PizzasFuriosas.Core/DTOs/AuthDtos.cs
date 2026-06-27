namespace PizzasFuriosas.Core.DTOs;

public record RegisterRequest(string Name, string Email, string Password, string Role = "Employee");
public record LoginRequest(string Email, string Password);
public record AuthResponse(int Id, string Name, string Email, string Role, string Token);
