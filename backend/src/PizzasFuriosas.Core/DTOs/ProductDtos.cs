namespace PizzasFuriosas.Core.DTOs;

public record CreateProductRequest(string Name, decimal Price, int CategoryId, bool IsAvailable = true);
public record UpdateProductRequest(string Name, decimal Price, int CategoryId, bool IsAvailable);
public record ProductResponse(int Id, string Name, decimal Price, bool IsAvailable, int CategoryId, string CategoryName, string? ImageUrl);
