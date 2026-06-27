namespace PizzasFuriosas.Core.DTOs;

public record CreatePurchaseRequest(DateTime Date, string Description, decimal Amount, string Category);
public record UpdatePurchaseRequest(DateTime Date, string Description, decimal Amount, string Category);

public record PurchaseResponse(int Id, DateTime Date, string Description, decimal Amount, string Category);
