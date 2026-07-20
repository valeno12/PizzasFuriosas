using Microsoft.EntityFrameworkCore;
using PizzasFuriosas.Api.Exceptions;
using PizzasFuriosas.Api.Services;
using PizzasFuriosas.Core.DTOs;
using PizzasFuriosas.Core.Entities;
using PizzasFuriosas.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace PizzasFuriosas.Api.Tests.Orders;

public class OrderServiceCreateTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("pizzasfuriosas_create_tests")
        .WithUsername("pizzas")
        .WithPassword("pizzas")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task CreateAsync_WithExistingCustomer_CreatesOrder()
    {
        await using var context = await CreateContextAsync();
        var customer = new Customer { Name = "Ada", Phone = "111" };
        var product = new Product { Name = "Muzza", Price = 100, CategoryId = 1, IsAvailable = true };
        context.Customers.Add(customer);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var orderId = await Service(context).CreateAsync(new CreateOrderRequest(
            customer.Id, null, null, null, null, "Take Away", 0, "Efectivo", null, null,
            [new CreateOrderItemRequest(product.Id, 2)]));

        var order = await context.Orders.Include(o => o.Items).SingleAsync(o => o.Id == orderId);
        Assert.Equal(customer.Id, order.CustomerId);
        Assert.Equal(200, order.TotalPrice);
        Assert.Single(order.Items);
    }

    [Fact]
    public async Task CreateAsync_WithNewCustomer_CreatesCustomerAndOrder()
    {
        await using var context = await CreateContextAsync();
        var product = await AddProductAsync(context, price: 50);

        var orderId = await Service(context).CreateAsync(new CreateOrderRequest(
            null, "Grace", "222", null, null, "Take Away", 0, "Transferencia", " sin cebolla ", null,
            [new CreateOrderItemRequest(product.Id, 1)]));

        var order = await context.Orders.Include(o => o.Customer).SingleAsync(o => o.Id == orderId);
        Assert.Equal("Grace", order.Customer.Name);
        Assert.Equal("222", order.Customer.Phone);
        Assert.Equal("sin cebolla", order.Notes);
    }

    [Fact]
    public async Task CreateAsync_WithNewDeliveryAddress_CreatesAddressForOrder()
    {
        await using var context = await CreateContextAsync();
        var customer = new Customer { Name = "Linus", Phone = "333" };
        context.Customers.Add(customer);
        var product = await AddProductAsync(context, price: 80);
        await context.SaveChangesAsync();

        var orderId = await Service(context).CreateAsync(new CreateOrderRequest(
            customer.Id, null, null, null, new CreateAddressRequest("Falsa", "123", "A", "Timbre"),
            "Delivery", 20, "Efectivo", null, null, [new CreateOrderItemRequest(product.Id, 1)]));

        var order = await context.Orders.Include(o => o.Address).SingleAsync(o => o.Id == orderId);
        Assert.NotNull(order.Address);
        Assert.Equal(customer.Id, order.Address!.CustomerId);
        Assert.Equal("Falsa", order.Address.Street);
        Assert.Equal(100, order.TotalPrice);
    }

    [Fact]
    public async Task CreateAsync_WithAddressFromAnotherCustomer_ThrowsNotFoundAndPersistsNothing()
    {
        await using var context = await CreateContextAsync();
        var customer = new Customer { Name = "Owner", Phone = "444" };
        var otherCustomer = new Customer { Name = "Other", Phone = "555" };
        var otherAddress = new Address { Customer = otherCustomer, Street = "Otra", Number = "1" };
        context.Customers.AddRange(customer, otherCustomer);
        context.Addresses.Add(otherAddress);
        var product = await AddProductAsync(context, price: 90);
        await context.SaveChangesAsync();

        var initialOrderCount = await context.Orders.CountAsync();

        await Assert.ThrowsAsync<NotFoundException>(() => Service(context).CreateAsync(new CreateOrderRequest(
            customer.Id, null, null, otherAddress.Id, null, "Delivery", 10, "Efectivo", null, null,
            [new CreateOrderItemRequest(product.Id, 1)])));

        Assert.Equal(initialOrderCount, await context.Orders.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_WithMissingProduct_ThrowsBadRequestAndPersistsNothing()
    {
        await using var context = await CreateContextAsync();

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Service(context).CreateAsync(new CreateOrderRequest(
            null, "Barbara", "666", null, null, "Take Away", 0, "Efectivo", null, null,
            [new CreateOrderItemRequest(9999, 1)])));

        Assert.Contains("9999", ex.Message);
        Assert.False(await context.Customers.AnyAsync(c => c.Name == "Barbara"));
        Assert.Empty(await context.Orders.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_UsesCurrentStoredProductPrice()
    {
        await using var context = await CreateContextAsync();
        var product = await AddProductAsync(context, price: 123.45m);
        await context.SaveChangesAsync();

        var orderId = await Service(context).CreateAsync(new CreateOrderRequest(
            null, "Price", "777", null, null, "Take Away", 0, "Efectivo", null, null,
            [new CreateOrderItemRequest(product.Id, 2)]));

        var order = await context.Orders.Include(o => o.Items).SingleAsync(o => o.Id == orderId);
        Assert.Equal(123.45m, order.Items.Single().UnitPrice);
        Assert.Equal(246.90m, order.TotalPrice);
    }

    [Fact]
    public async Task CreateAsync_TakeAway_ForcesDeliveryCostToZero()
    {
        await using var context = await CreateContextAsync();
        var product = await AddProductAsync(context, price: 40);
        await context.SaveChangesAsync();

        var orderId = await Service(context).CreateAsync(new CreateOrderRequest(
            null, "Take", "888", null, null, "Take Away", 999, "Efectivo", null, null,
            [new CreateOrderItemRequest(product.Id, 1)]));

        var order = await context.Orders.SingleAsync(o => o.Id == orderId);
        Assert.Equal(0, order.DeliveryCost);
        Assert.Equal(40, order.TotalPrice);
    }

    [Fact]
    public async Task CreateAsync_WithNewCustomerNewAddressAndMissingProduct_PersistsNothing()
    {
        await using var context = await CreateContextAsync();

        await Assert.ThrowsAsync<BadRequestException>(() => Service(context).CreateAsync(new CreateOrderRequest(
            null, "Atomic", "999", null, new CreateAddressRequest("No", "1", null, null),
            "Delivery", 100, "Efectivo", null, null, [new CreateOrderItemRequest(404, 1)])));

        Assert.False(await context.Customers.AnyAsync(c => c.Name == "Atomic"));
        Assert.Empty(await context.Addresses.ToListAsync());
        Assert.Empty(await context.Orders.ToListAsync());
    }

    private async Task<AppDbContext> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var context = new AppDbContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        return context;
    }

    private static OrderService Service(AppDbContext context) => new(context);

    private static async Task<Product> AddProductAsync(AppDbContext context, decimal price)
    {
        var product = new Product
        {
            Name = $"Producto {Guid.NewGuid():N}",
            Price = price,
            CategoryId = 1,
            IsAvailable = true
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product;
    }
}
