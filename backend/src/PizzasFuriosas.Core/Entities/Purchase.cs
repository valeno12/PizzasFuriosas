namespace PizzasFuriosas.Core.Entities;

public class Purchase : BaseEntity
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
}
