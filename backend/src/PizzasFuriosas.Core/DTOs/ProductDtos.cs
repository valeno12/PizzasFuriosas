namespace PizzasFuriosas.Core.DTOs;

public record ProductComponentRequest(int ProductId, int Quantity);
public record ProductComponentResponse(int ProductId, string ProductName, int Quantity);
public record CreateProductRequest(string Name, decimal Price, int CategoryId, bool IsAvailable = true,
    List<ProductComponentRequest>? Components = null, bool FreeDelivery = false);
// null conserva la configuración para clientes anteriores que no conocen promos.
public record UpdateProductRequest(string Name, decimal Price, int CategoryId, bool IsAvailable,
    List<ProductComponentRequest>? Components = null, bool? FreeDelivery = null);
public record ProductResponse(int Id, string Name, decimal Price, bool IsAvailable, int CategoryId, string CategoryName, string? ImageUrl,
    List<ProductComponentResponse>? Components = null, bool FreeDelivery = false);
