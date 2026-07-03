namespace PizzasFuriosas.Core.DTOs;

public record CreateCustomerRequest(string Name, string? Phone, string? Notes);
public record UpdateCustomerRequest(string Name, string? Phone, string? Notes);

public record CustomerResponse(int Id, string Name, string? Phone, string? Notes);

public record AddressResponse(int Id, int CustomerId, string Street, string Number, string? Apartment, string? Notes);

// Resumen del historial del cliente para la ficha.
public record CustomerStatsResponse(int TotalOrders, decimal TotalSpent, string? FavoriteProduct);
