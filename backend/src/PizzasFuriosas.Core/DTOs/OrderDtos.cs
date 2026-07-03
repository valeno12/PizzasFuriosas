namespace PizzasFuriosas.Core.DTOs;

public record CreateOrderItemRequest(int ProductId, int Quantity);
public record CreateAddressRequest(string Street, string Number, string? Apartment, string? Notes);

public record CreateOrderRequest(
    int? CustomerId,
    string? CustomerName,
    string? CustomerPhone,
    int? AddressId,
    CreateAddressRequest? NewAddress,
    string ShippingMethod, // "Take Away" o "Delivery"
    decimal DeliveryCost,
    string PaymentMethod, // "Efectivo" o "Transferencia"
    string? Notes,
    DateTime? ScheduledFor,
    List<CreateOrderItemRequest> Items
);

public record OrderItemResponse(int Id, int ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal Subtotal);

public record OrderAddressResponse(int Id, string Street, string Number, string? Apartment, string? Notes);

public record OrderResponse(
    int Id,
    int CustomerId,
    string CustomerName,
    string? CustomerPhone,
    string ShippingMethod,
    decimal DeliveryCost,
    string PaymentMethod,
    int StatusId,
    string StatusName,
    decimal TotalPrice,
    DateTime CreatedAt,
    DateTime? ScheduledFor,
    string? Notes,
    OrderAddressResponse? Address,
    List<OrderItemResponse> Items
);

// Edita un pedido activo. El cliente no se cambia (para eso: cancelar y recrear).
public record UpdateOrderRequest(
    int? AddressId,
    CreateAddressRequest? NewAddress,
    string ShippingMethod,
    decimal DeliveryCost,
    string PaymentMethod,
    string? Notes,
    DateTime? ScheduledFor,
    List<CreateOrderItemRequest> Items
);

public record UpdateOrderStatusRequest(int StatusId);

public record OrderFilterDto(
    int? StatusId,
    int? CustomerId,
    int? ProductId,
    string? ShippingMethod,
    string? PaymentMethod,
    DateTime? From,
    DateTime? To,
    bool? OnlyActive,
    string? Search
);
