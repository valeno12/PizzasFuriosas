using Microsoft.EntityFrameworkCore;
using PizzasFuriosas.Api.Exceptions;
using PizzasFuriosas.Api.Services;
using PizzasFuriosas.Core.DTOs;
using PizzasFuriosas.Core.Entities;
using PizzasFuriosas.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace PizzasFuriosas.Api.Tests.Orders;

public class OrderServiceUpdateTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("pizzasfuriosas_update_tests")
        .WithUsername("pizzas")
        .WithPassword("pizzas")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task UpdateAsync_WithMissingOrder_ThrowsNotFound()
    {
        await using var context = await CreateContextAsync();
        var product = await AddProductAsync(context, price: 100);

        await Assert.ThrowsAsync<NotFoundException>(() => Service(context).UpdateAsync(9999, new UpdateOrderRequest(
            null, null, "Take Away", 0, "Efectivo", null, null,
            [new CreateOrderItemRequest(product.Id, 1)])));
    }

    [Fact]
    public async Task UpdateAsync_WithDeliveredOrder_ThrowsConflict()
    {
        await using var context = await CreateContextAsync();
        var order = await AddOrderAsync(context, statusId: 5, productPrice: 100);
        var originalItem = order.Items.Single();
        var replacementProduct = await AddProductAsync(context, price: 50);

        await Assert.ThrowsAsync<ConflictException>(() => Service(context).UpdateAsync(order.Id, new UpdateOrderRequest(
            null, null, "Take Away", 0, "Transferencia", "nueva", null,
            [new CreateOrderItemRequest(replacementProduct.Id, 3)])));

        await using var verificationContext = await CreateContextAsync(resetDatabase: false);
        var persistedOrder = await verificationContext.Orders.Include(o => o.Items).SingleAsync(o => o.Id == order.Id);
        Assert.Equal(5, persistedOrder.StatusId);
        Assert.Equal("Efectivo", persistedOrder.PaymentMethod);
        Assert.Null(persistedOrder.Notes);
        var persistedItem = Assert.Single(persistedOrder.Items);
        Assert.Equal(originalItem.Id, persistedItem.Id);
        Assert.Equal(originalItem.ProductId, persistedItem.ProductId);
        Assert.Equal(originalItem.Quantity, persistedItem.Quantity);
        Assert.Equal(originalItem.UnitPrice, persistedItem.UnitPrice);
    }

    [Fact]
    public async Task UpdateAsync_WithCancelledOrder_ThrowsConflict()
    {
        await using var context = await CreateContextAsync();
        var order = await AddOrderAsync(context, statusId: 6, productPrice: 100);
        var originalItem = order.Items.Single();
        var replacementProduct = await AddProductAsync(context, price: 50);

        await Assert.ThrowsAsync<ConflictException>(() => Service(context).UpdateAsync(order.Id, new UpdateOrderRequest(
            null, null, "Take Away", 0, "Transferencia", "nueva", null,
            [new CreateOrderItemRequest(replacementProduct.Id, 3)])));

        await using var verificationContext = await CreateContextAsync(resetDatabase: false);
        var persistedOrder = await verificationContext.Orders.Include(o => o.Items).SingleAsync(o => o.Id == order.Id);
        Assert.Equal(6, persistedOrder.StatusId);
        Assert.Equal("Efectivo", persistedOrder.PaymentMethod);
        Assert.Null(persistedOrder.Notes);
        var persistedItem = Assert.Single(persistedOrder.Items);
        Assert.Equal(originalItem.Id, persistedItem.Id);
    }

    [Fact]
    public async Task UpdateAsync_WithAddressFromAnotherCustomer_ThrowsNotFoundAndPersistsNothing()
    {
        await using var context = await CreateContextAsync();
        var order = await AddOrderAsync(context, statusId: 1, productPrice: 100);
        var otherCustomer = new Customer { Name = "Other", Phone = "222" };
        var otherAddress = new Address { Customer = otherCustomer, Street = "Otra", Number = "1" };
        context.Customers.Add(otherCustomer);
        context.Addresses.Add(otherAddress);
        await context.SaveChangesAsync();

        var originalTotal = order.TotalPrice;
        var originalPaymentMethod = order.PaymentMethod;

        await Assert.ThrowsAsync<NotFoundException>(() => Service(context).UpdateAsync(order.Id, new UpdateOrderRequest(
            otherAddress.Id, null, "Delivery", 50, "Transferencia", "nota", null,
            [new CreateOrderItemRequest(order.Items.Single().ProductId, 3)])));

        await using var verificationContext = await CreateContextAsync(resetDatabase: false);
        var persistedOrder = await verificationContext.Orders.Include(o => o.Items).SingleAsync(o => o.Id == order.Id);
        Assert.Equal(originalTotal, persistedOrder.TotalPrice);
        Assert.Equal(originalPaymentMethod, persistedOrder.PaymentMethod);
        Assert.Single(persistedOrder.Items);
    }

    [Fact]
    public async Task UpdateAsync_WithMissingAddress_ThrowsNotFoundAndPersistsNothing()
    {
        await using var context = await CreateContextAsync();
        var order = await AddOrderAsync(context, statusId: 1, productPrice: 100);
        var originalItem = order.Items.Single();

        await Assert.ThrowsAsync<NotFoundException>(() => Service(context).UpdateAsync(order.Id, new UpdateOrderRequest(
            9999, null, "Delivery", 50, "Transferencia", "nueva", null,
            [new CreateOrderItemRequest(originalItem.ProductId, 3)])));

        await using var verificationContext = await CreateContextAsync(resetDatabase: false);
        var persistedOrder = await verificationContext.Orders.Include(o => o.Items).SingleAsync(o => o.Id == order.Id);
        Assert.Equal("Take Away", persistedOrder.ShippingMethod);
        Assert.Equal(0, persistedOrder.DeliveryCost);
        Assert.Equal("Efectivo", persistedOrder.PaymentMethod);
        Assert.Null(persistedOrder.AddressId);
        var persistedItem = Assert.Single(persistedOrder.Items);
        Assert.Equal(originalItem.Id, persistedItem.Id);
    }

    [Fact]
    public async Task UpdateAsync_WithMissingProduct_ThrowsBadRequestAndPersistsNothing()
    {
        await using var context = await CreateContextAsync();
        var order = await AddOrderAsync(context, statusId: 1, productPrice: 100);
        var originalItem = order.Items.Single();
        var originalTotal = order.TotalPrice;

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Service(context).UpdateAsync(order.Id, new UpdateOrderRequest(
            null, null, "Take Away", 999, "Transferencia", "nueva", null,
            [new CreateOrderItemRequest(404, 2)])));

        Assert.Contains("404", ex.Message);

        await using var verificationContext = await CreateContextAsync(resetDatabase: false);
        var persistedOrder = await verificationContext.Orders.Include(o => o.Items).SingleAsync(o => o.Id == order.Id);
        var persistedItem = persistedOrder.Items.Single();
        Assert.Equal(originalTotal, persistedOrder.TotalPrice);
        Assert.Equal("Efectivo", persistedOrder.PaymentMethod);
        Assert.Equal(originalItem.ProductId, persistedItem.ProductId);
        Assert.Equal(originalItem.Quantity, persistedItem.Quantity);
        Assert.Equal(originalItem.UnitPrice, persistedItem.UnitPrice);
    }

    [Fact]
    public async Task UpdateAsync_WithValidUpdate_UpdatesOrder()
    {
        await using var context = await CreateContextAsync();
        var order = await AddOrderAsync(context, statusId: 1, productPrice: 100);
        var address = new Address { CustomerId = order.CustomerId, Street = "Nueva", Number = "9" };
        var newProduct = await AddProductAsync(context, price: 75);
        context.Addresses.Add(address);
        await context.SaveChangesAsync();

        var scheduledFor = new DateTime(2026, 7, 13, 20, 30, 0, DateTimeKind.Utc);
        await Service(context).UpdateAsync(order.Id, new UpdateOrderRequest(
            address.Id, null, "Delivery", 25, "Transferencia", " con cambio ", scheduledFor,
            [new CreateOrderItemRequest(newProduct.Id, 2)]));

        await using var verificationContext = await CreateContextAsync(resetDatabase: false);
        var updatedOrder = await verificationContext.Orders.Include(o => o.Items).SingleAsync(o => o.Id == order.Id);
        Assert.Equal("Delivery", updatedOrder.ShippingMethod);
        Assert.Equal(25, updatedOrder.DeliveryCost);
        Assert.Equal("Transferencia", updatedOrder.PaymentMethod);
        Assert.Equal(address.Id, updatedOrder.AddressId);
        Assert.Equal("con cambio", updatedOrder.Notes);
        Assert.Equal(scheduledFor, updatedOrder.ScheduledFor);
        Assert.Equal(175, updatedOrder.TotalPrice);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesExistingItems()
    {
        await using var context = await CreateContextAsync();
        var order = await AddOrderAsync(context, statusId: 1, productPrice: 100);
        var oldItemId = order.Items.Single().Id;
        var firstProduct = await AddProductAsync(context, price: 30);
        var secondProduct = await AddProductAsync(context, price: 40);
        await context.SaveChangesAsync();

        await Service(context).UpdateAsync(order.Id, new UpdateOrderRequest(
            null, null, "Take Away", 0, "Efectivo", null, null,
            [new CreateOrderItemRequest(firstProduct.Id, 1), new CreateOrderItemRequest(secondProduct.Id, 2)]));

        await using var verificationContext = await CreateContextAsync(resetDatabase: false);
        var updatedOrder = await verificationContext.Orders.Include(o => o.Items).SingleAsync(o => o.Id == order.Id);
        Assert.Equal(2, updatedOrder.Items.Count);
        Assert.DoesNotContain(updatedOrder.Items, i => i.Id == oldItemId);
        Assert.Contains(updatedOrder.Items, i => i.ProductId == firstProduct.Id && i.Quantity == 1);
        Assert.Contains(updatedOrder.Items, i => i.ProductId == secondProduct.Id && i.Quantity == 2);
        Assert.False(await verificationContext.OrderItems.IgnoreQueryFilters().AnyAsync(i => i.Id == oldItemId));
    }

    [Fact]
    public async Task UpdateAsync_UsesCurrentStoredProductPrices()
    {
        await using var context = await CreateContextAsync();
        var order = await AddOrderAsync(context, statusId: 1, productPrice: 100);
        var product = await AddProductAsync(context, price: 123.45m);
        await context.SaveChangesAsync();

        await Service(context).UpdateAsync(order.Id, new UpdateOrderRequest(
            null, null, "Take Away", 0, "Efectivo", null, null,
            [new CreateOrderItemRequest(product.Id, 2)]));

        var updatedOrder = await context.Orders.Include(o => o.Items).SingleAsync(o => o.Id == order.Id);
        Assert.Equal(123.45m, updatedOrder.Items.Single().UnitPrice);
        Assert.Equal(246.90m, updatedOrder.TotalPrice);
    }

    [Fact]
    public async Task UpdateAsync_TakeAway_ForcesDeliveryCostToZero()
    {
        await using var context = await CreateContextAsync();
        var order = await AddOrderAsync(context, statusId: 1, productPrice: 40);
        var productId = order.Items.Single().ProductId;
        var address = new Address { CustomerId = order.CustomerId, Street = "Anterior", Number = "10" };
        context.Addresses.Add(address);
        order.Address = address;
        order.ShippingMethod = "Delivery";
        order.DeliveryCost = 20;
        order.TotalPrice = 60;
        await context.SaveChangesAsync();

        await Service(context).UpdateAsync(order.Id, new UpdateOrderRequest(
            null, null, "Take Away", 999, "Efectivo", null, null,
            [new CreateOrderItemRequest(productId, 1)]));

        await using var verificationContext = await CreateContextAsync(resetDatabase: false);
        var updatedOrder = await verificationContext.Orders.SingleAsync(o => o.Id == order.Id);
        Assert.Equal(0, updatedOrder.DeliveryCost);
        Assert.Equal(40, updatedOrder.TotalPrice);
        Assert.Null(updatedOrder.AddressId);
    }

    [Fact]
    public async Task UpdateAsync_WithNewAddress_CreatesAndAssignsAddressInSingleSave()
    {
        await using var context = await CreateContextAsync();
        var order = await AddOrderAsync(context, statusId: 1, productPrice: 80);
        var productId = order.Items.Single().ProductId;
        var initialAddressCount = await context.Addresses.CountAsync();

        await Service(context).UpdateAsync(order.Id, new UpdateOrderRequest(
            null, new CreateAddressRequest("Falsa", "123", "A", "Timbre"),
            "Delivery", 20, "Efectivo", null, null,
            [new CreateOrderItemRequest(productId, 1)]));

        await using var verificationContext = await CreateContextAsync(resetDatabase: false);
        var updatedOrder = await verificationContext.Orders.Include(o => o.Address).SingleAsync(o => o.Id == order.Id);
        Assert.Equal(initialAddressCount + 1, await verificationContext.Addresses.CountAsync());
        Assert.NotNull(updatedOrder.Address);
        Assert.Equal(order.CustomerId, updatedOrder.Address!.CustomerId);
        Assert.Equal("Falsa", updatedOrder.Address.Street);
        Assert.Equal("123", updatedOrder.Address.Number);
        Assert.Equal("A", updatedOrder.Address.Apartment);
        Assert.Equal("Timbre", updatedOrder.Address.Notes);
        Assert.Equal(100, updatedOrder.TotalPrice);
    }

    [Fact]
    public async Task UpdateAsync_WithNewAddressAndMissingProduct_PersistsNothing()
    {
        await using var context = await CreateContextAsync();
        var order = await AddOrderAsync(context, statusId: 1, productPrice: 100);
        var initialAddressCount = await context.Addresses.CountAsync();
        var originalTotal = order.TotalPrice;

        await Assert.ThrowsAsync<BadRequestException>(() => Service(context).UpdateAsync(order.Id, new UpdateOrderRequest(
            null, new CreateAddressRequest("No", "1", null, null), "Delivery", 100, "Transferencia", "nueva", null,
            [new CreateOrderItemRequest(404, 1)])));

        await using var verificationContext = await CreateContextAsync(resetDatabase: false);
        var persistedOrder = await verificationContext.Orders.Include(o => o.Items).SingleAsync(o => o.Id == order.Id);
        Assert.Equal(initialAddressCount, await verificationContext.Addresses.CountAsync());
        Assert.Equal(originalTotal, persistedOrder.TotalPrice);
        Assert.Equal("Take Away", persistedOrder.ShippingMethod);
        Assert.Equal("Efectivo", persistedOrder.PaymentMethod);
        Assert.Single(persistedOrder.Items);
    }

    private async Task<AppDbContext> CreateContextAsync(bool resetDatabase = true)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var context = new AppDbContext(options);
        if (resetDatabase)
        {
            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();
        }

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

    private static async Task<Order> AddOrderAsync(AppDbContext context, int statusId, decimal productPrice)
    {
        var customer = new Customer { Name = $"Cliente {Guid.NewGuid():N}", Phone = "111" };
        var product = new Product
        {
            Name = $"Producto {Guid.NewGuid():N}",
            Price = productPrice,
            CategoryId = 1,
            IsAvailable = true
        };
        var order = new Order
        {
            Customer = customer,
            StatusId = statusId,
            ShippingMethod = "Take Away",
            DeliveryCost = 0,
            PaymentMethod = "Efectivo",
            TotalPrice = productPrice,
            Items =
            [
                new OrderItem
                {
                    Product = product,
                    Quantity = 1,
                    UnitPrice = productPrice
                }
            ]
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();
        return order;
    }
}
