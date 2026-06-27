namespace PizzasFuriosas.Core.Entities;

public class EventSurcharge : BaseEntity
{
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
