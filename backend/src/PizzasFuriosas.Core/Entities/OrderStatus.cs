namespace PizzasFuriosas.Core.Entities;

public class OrderStatus : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
