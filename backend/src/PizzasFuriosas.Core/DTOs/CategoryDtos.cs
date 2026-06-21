namespace PizzasFuriosas.Core.DTOs;

public record CreateCategoryRequest(string Name);
public record CategoryResponse(int Id, string Name, DateTime CreatedAt);
