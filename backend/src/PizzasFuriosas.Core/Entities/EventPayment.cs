namespace PizzasFuriosas.Core.Entities;

public class EventPayment : BaseEntity
{
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
