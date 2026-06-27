namespace PizzasFuriosas.Core.Entities;

public class Event : BaseEntity
{
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public int PizzaCount { get; set; }
    public decimal PricePerPizza { get; set; }
    public decimal Deposit { get; set; }

    public DateTime? CancelledAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ICollection<EventSurcharge> Surcharges { get; set; } = new List<EventSurcharge>();
    public ICollection<EventPayment> Payments { get; set; } = new List<EventPayment>();
}
