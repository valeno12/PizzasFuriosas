namespace PizzasFuriosas.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using PizzasFuriosas.Core.Entities;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<OrderStatus> OrderStatuses { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    
    public DbSet<Event> Events { get; set; }
    public DbSet<EventSurcharge> EventSurcharges { get; set; }
    public DbSet<EventPayment> EventPayments { get; set; }
    
    public DbSet<Purchase> Purchases { get; set; }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Customer>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<Address>().HasQueryFilter(a => !a.IsDeleted);
        modelBuilder.Entity<OrderStatus>().HasQueryFilter(os => !os.IsDeleted);
        modelBuilder.Entity<Order>().HasQueryFilter(o => !o.IsDeleted);
        modelBuilder.Entity<OrderItem>().HasQueryFilter(oi => !oi.IsDeleted);
        
        modelBuilder.Entity<Event>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<EventSurcharge>().HasQueryFilter(es => !es.IsDeleted);
        modelBuilder.Entity<EventPayment>().HasQueryFilter(ep => !ep.IsDeleted);
        
        modelBuilder.Entity<Purchase>().HasQueryFilter(p => !p.IsDeleted);

        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        // --- SEED DATA ---
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Pizzas", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Name = "Papas", CreatedAt = DateTime.UtcNow },
            new Category { Id = 3, Name = "Postres", CreatedAt = DateTime.UtcNow }
        );

        modelBuilder.Entity<OrderStatus>().HasData(
            new OrderStatus { Id = 1, Name = "Pendiente", Description = "Recibido, sin iniciar", CreatedAt = DateTime.UtcNow },
            new OrderStatus { Id = 2, Name = "En Preparación", Description = "En cocina", CreatedAt = DateTime.UtcNow },
            new OrderStatus { Id = 3, Name = "Listo", Description = "Esperando retiro o cadete", CreatedAt = DateTime.UtcNow },
            new OrderStatus { Id = 4, Name = "En Camino", Description = "El cadete lo tiene", CreatedAt = DateTime.UtcNow },
            new OrderStatus { Id = 5, Name = "Entregado", Description = "Completado", CreatedAt = DateTime.UtcNow },
            new OrderStatus { Id = 6, Name = "Cancelado", Description = "Cancelado", CreatedAt = DateTime.UtcNow }
        );
    }



}