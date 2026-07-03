namespace PizzasFuriosas.Core.DTOs;

public record CreateEventSurchargeRequest(string Description, decimal Amount);
public record CreateEventPaymentRequest(string PaymentMethod, decimal Amount);

public record CreateEventRequest(
    int CustomerId,
    DateTime EventDate,
    string Location,
    string? Notes,
    int PizzaCount,
    decimal PricePerPizza,
    decimal Deposit,
    List<CreateEventSurchargeRequest> Surcharges,
    List<CreateEventPaymentRequest> Payments
);

public record EventSurchargeResponse(int Id, string Description, decimal Amount);
public record EventPaymentResponse(int Id, string PaymentMethod, decimal Amount);

public record EventResponse(
    int Id,
    int CustomerId,
    string CustomerName,
    DateTime EventDate,
    string Location,
    string? Notes,
    int PizzaCount,
    decimal PricePerPizza,
    decimal Deposit,
    decimal TotalCost,
    decimal OutstandingBalance,
    string Status,
    DateTime? CancelledAt,
    DateTime? CompletedAt,
    List<EventSurchargeResponse> Surcharges,
    List<EventPaymentResponse> Payments
);

// Edita un evento abierto. Viáticos y pagos tienen sus propios endpoints.
public record UpdateEventRequest(
    DateTime EventDate,
    string Location,
    string? Notes,
    int PizzaCount,
    decimal PricePerPizza,
    decimal Deposit
);

public record CompleteEventRequest(
    int ExtraPizzas,
    List<CreateEventSurchargeRequest> ExtraSurcharges
);
