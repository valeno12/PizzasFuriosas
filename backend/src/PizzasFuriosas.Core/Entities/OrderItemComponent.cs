namespace PizzasFuriosas.Core.Entities;

// Snapshot por unidad de promo: no depende de cambios posteriores del catálogo.
public class OrderItemComponent
{
    public int Id { get; set; }
    public int OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = null!;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
