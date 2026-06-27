using System.ComponentModel.DataAnnotations;

namespace PizzasFuriosas.Core.Entities;

public class Category : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Product> Products { get; set; } = [];
}