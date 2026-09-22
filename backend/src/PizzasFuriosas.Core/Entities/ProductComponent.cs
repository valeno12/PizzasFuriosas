namespace PizzasFuriosas.Core.Entities;

public class ProductComponent
{
    public int Id { get; set; }
    public int PromotionId { get; set; }
    public Product Promotion { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
}
