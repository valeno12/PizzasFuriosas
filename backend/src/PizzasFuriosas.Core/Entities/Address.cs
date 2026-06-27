namespace PizzasFuriosas.Core.Entities;

public class Address : BaseEntity
{
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string Street { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string? Apartment { get; set; }
    public string? Notes { get; set; }
}
