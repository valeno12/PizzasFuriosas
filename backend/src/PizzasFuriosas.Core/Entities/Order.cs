namespace PizzasFuriosas.Core.Entities;

public class Order : BaseEntity
{
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int? AddressId { get; set; }
    public Address? Address { get; set; }

    public int StatusId { get; set; } = 1; // 1 = Pendiente
    public OrderStatus Status { get; set; } = null!;
    
    public string ShippingMethod { get; set; } = "Take Away"; // Take Away, Delivery
    public decimal DeliveryCost { get; set; } = 0;
    
    public string PaymentMethod { get; set; } = "Efectivo"; // Efectivo, Transferencia

    public decimal TotalPrice { get; set; }

    // Aclaraciones del pedido: "sin cebolla", "tocar timbre", etc.
    public string? Notes { get; set; }

    // Hora prometida de entrega/retiro; null = lo antes posible.
    public DateTime? ScheduledFor { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
