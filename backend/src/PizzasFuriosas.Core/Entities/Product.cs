
namespace PizzasFuriosas.Core.Entities;
public class Product : BaseEntity
{

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public bool IsAvailable { get; set; } = true;

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;
}