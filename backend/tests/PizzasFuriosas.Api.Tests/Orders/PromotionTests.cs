using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PizzasFuriosas.Api.Exceptions;
using PizzasFuriosas.Api.Services;
using PizzasFuriosas.Core.DTOs;
using PizzasFuriosas.Core.Entities;
using PizzasFuriosas.Core.Interfaces;
using PizzasFuriosas.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace PizzasFuriosas.Api.Tests.Orders;

public class PromotionTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine").WithDatabase("promotion_tests")
        .WithUsername("pizzas").WithPassword("pizzas").Build();
    public Task InitializeAsync() => _postgres.StartAsync();
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    private AppDbContext Context() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(_postgres.GetConnectionString()).Options);
    private static ProductService Products(AppDbContext db) => new(db, new NoPhotos());
    private static CreateOrderRequest Request(params CreateOrderItemRequest[] items) => new(
        null, "Cliente", "123", null, new CreateAddressRequest("Calle", "1", null, null),
        "Delivery", 2000, "Efectivo", null, null, items.ToList());
    private static UpdateOrderRequest Update(List<CreateOrderItemRequest> items) => new(
        null, new CreateAddressRequest("Calle", "1", null, null), "Delivery", 2000, "Efectivo", "editado", null, items);

    private async Task<(AppDbContext Db, int Pizza, int Fries, int Promo)> Setup(bool freeDelivery = true)
    {
        var db = Context();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        var service = Products(db);
        var pizza = await service.CreateAsync(new("Muzza", 12000, 1), default);
        var fries = await service.CreateAsync(new("Papas", 4500, 2), default);
        var promo = await service.CreateAsync(new("Promo", 30000, 1, true,
            [new(pizza.Id, 3), new(fries.Id, 1)], freeDelivery), default);
        return (db, pizza.Id, fries.Id, promo.Id);
    }

    [Fact]
    public async Task Promo_CountsActualProducts_AndChargesComboOnly_WithFreeDelivery()
    {
        var (db, pizza, fries, promo) = await Setup();
        await using var context = db;
        var service = new OrderService(db);
        var id = await service.CreateAsync(Request(new CreateOrderItemRequest(promo, 2), new(pizza, 1)));
        db.ChangeTracker.Clear();
        var order = await service.GetByIdAsync(id);
        Assert.Equal(72000, order.TotalPrice);
        Assert.Equal(0, order.DeliveryCost);
        Assert.Equal(2, order.Items.Count);
        Assert.Equal(3, order.Items.Single(i => i.ProductId == promo).Components!.Single(c => c.ProductId == pizza).Quantity);
        await service.UpdateStatusAsync(id, new(5));
        var stats = await new DashboardService(db).GetStatisticsAsync(null, null, default);
        Assert.Equal(7, stats.TopProducts.Single(p => p.ProductId == pizza).TotalQuantitySold);
        Assert.Equal(2, stats.TopProducts.Single(p => p.ProductId == fries).TotalQuantitySold);
        Assert.Equal(2, stats.TopPromotions!.Single().TotalQuantitySold);
        Assert.DoesNotContain(stats.TopProducts, p => p.ProductId == promo);
        var balance = await new DashboardService(db).GetBalanceAsync(null, null, default);
        Assert.Equal(72000, balance.TotalOrdersIncome);
        var filtered = await service.GetAllAsync(new(null, null, fries, null, null, null, null, null, null), 1, 10);
        Assert.Single(filtered.Items);
    }

    [Fact]
    public async Task EditingCatalogAndOrder_PreservesSoldCompositionPriceAndDeliveryBenefit()
    {
        var (db, pizza, fries, promo) = await Setup();
        await using var context = db;
        var service = new OrderService(db);
        var id = await service.CreateAsync(Request(new CreateOrderItemRequest(promo, 1)));
        await Products(db).UpdateAsync(promo, new("Nueva promo", 999, 1, true, [new(fries, 2)], false), default);
        await Products(db).UpdateAsync(pizza, new("Nuevo nombre", 99999, 1, true), default);
        db.ChangeTracker.Clear();
        await service.UpdateAsync(id, Update([new(promo, 2)]));
        db.ChangeTracker.Clear();
        var order = await service.GetByIdAsync(id);
        Assert.Equal(60000, order.TotalPrice);
        Assert.Equal(0, order.DeliveryCost);
        Assert.Equal("Promo", order.Items.Single().ProductName);
        Assert.Equal("Muzza", order.Items.Single().Components!.Single(c => c.ProductId == pizza).ProductName);
        await service.UpdateAsync(id, Update([new(fries, 1)]));
        var withoutPromo = await service.GetByIdAsync(id);
        Assert.Equal(2000, withoutPromo.DeliveryCost);
        Assert.Equal(6500, withoutPromo.TotalPrice);
        Assert.Empty(await db.OrderItemComponents.ToListAsync());
    }

    [Fact]
    public async Task OldProductUpdate_PreservesPromo_AndNonFreePromoChargesDelivery()
    {
        var (db, pizza, _, promo) = await Setup(false);
        await using var context = db;
        var response = await Products(db).UpdateAsync(promo, new("Promo", 30000, 1, true), default);
        Assert.Equal(2, response.Components!.Count);
        var orderId = await new OrderService(db).CreateAsync(Request(new CreateOrderItemRequest(promo, 1)));
        Assert.Equal(32000, (await new OrderService(db).GetByIdAsync(orderId)).TotalPrice);
        var products = Products(db);
        await Assert.ThrowsAsync<BadRequestException>(() => products.CreateAsync(new("Anidada", 1, 1, true, [new(promo, 1)]), default));
        await Assert.ThrowsAsync<BadRequestException>(() => products.UpdateAsync(pizza, new("Muzza", 1, 1, true, [new(promo, 1)]), default));
        await Assert.ThrowsAsync<ConflictException>(() => products.DeleteAsync(pizza, default));
    }

    [Fact]
    public async Task DeletedPromotionAndComponents_DoNotEraseOrderHistory()
    {
        var (db, pizza, _, promo) = await Setup();
        await using var context = db;
        var service = new OrderService(db);
        var id = await service.CreateAsync(Request(new CreateOrderItemRequest(promo, 1)));
        // A legacy availability update must not remove the free-delivery flag.
        var updated = await Products(db).UpdateAsync(promo, new("Promo", 30000, 1, false), default);
        Assert.True(updated.FreeDelivery);
        await Products(db).DeleteAsync(promo, default);
        await Products(db).DeleteAsync(pizza, default);
        db.ChangeTracker.Clear();
        var order = await service.GetByIdAsync(id);
        Assert.Single(order.Items);
        Assert.Equal("Promo", order.Items.Single().ProductName);
        Assert.Equal(2, order.Items.Single().Components!.Count);
        await service.UpdateStatusAsync(id, new(5));
        var stats = await new DashboardService(db).GetStatisticsAsync(null, null, default);
        Assert.Equal(3, stats.TopProducts.Single(p => p.ProductId == pizza).TotalQuantitySold);
    }

    [Fact]
    public async Task DuplicateSelfMissingAndEmptyFreeDeliveryComponents_AreRejected()
    {
        var (db, pizza, _, promo) = await Setup();
        await using var context = db;
        var service = Products(db);
        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(new("Duplicada", 1, 1, true, [new(pizza, 1), new(pizza, 1)]), default));
        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(new("Faltante", 1, 1, true, [new(9999, 1)]), default));
        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(new("Vacía", 1, 1, true, [], true), default));
        await Assert.ThrowsAsync<BadRequestException>(() => service.UpdateAsync(promo, new("Promo", 1, 1, true, [new(promo, 1)]), default));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task InvalidComponentQuantity_IsRejected(int quantity)
    {
        var (db, pizza, _, _) = await Setup();
        await using var context = db;
        await Assert.ThrowsAsync<BadRequestException>(() => Products(db).CreateAsync(
            new("Inválida", 1, 1, true, [new(pizza, quantity)]), default));
    }

    [Fact]
    public async Task UnavailableComponent_PreventsNewSale_AndHidesPromoFromAvailableCatalog()
    {
        var (db, pizza, _, promo) = await Setup();
        await using var context = db;
        await Products(db).UpdateAsync(pizza, new("Muzza", 12000, 1, false), default);
        var available = await Products(db).GetAllAsync(null, true, null, 1, 100, default);
        Assert.DoesNotContain(available.Items, p => p.Id == promo);
        await Assert.ThrowsAsync<BadRequestException>(() => new OrderService(db).CreateAsync(Request(new CreateOrderItemRequest(promo, 1))));
        Assert.Empty(await db.Orders.ToListAsync());
    }

    [Fact]
    public async Task Migration_PreservesLegacyData_AndHasNoPendingModelChanges()
    {
        await using var db = Context();
        await db.Database.EnsureDeletedAsync();
        await db.GetService<IMigrator>().MigrateAsync("20260718172057_AddCaseInsensitiveUniqueNameIndexes");
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO "Customers" ("Id", "Name", "CreatedAt", "IsDeleted") VALUES (100, 'Histórico', now(), false);
            INSERT INTO "Products" ("Id", "Name", "Price", "CategoryId", "IsAvailable", "CreatedAt", "IsDeleted")
                VALUES (100, 'Pizza histórica', 12000, 1, true, now(), false);
            INSERT INTO "Orders" ("Id", "CustomerId", "StatusId", "ShippingMethod", "PaymentMethod", "DeliveryCost", "TotalPrice", "CreatedAt", "IsDeleted")
                VALUES (100, 100, 5, 'Delivery', 'Efectivo', 1000, 25000, now(), false);
            INSERT INTO "OrderItems" ("Id", "OrderId", "ProductId", "Quantity", "UnitPrice", "CreatedAt", "IsDeleted")
                VALUES (100, 100, 100, 2, 12000, now(), false);
            """);
        await db.Database.MigrateAsync();
        Assert.False(db.Database.HasPendingModelChanges());
        var order = await new OrderService(db).GetByIdAsync(100);
        Assert.Equal(25000, order.TotalPrice);
        Assert.Equal(1000, order.DeliveryCost);
        Assert.Equal("Pizza histórica", order.Items.Single().ProductName);
        Assert.Equal(2, order.Items.Single().Quantity);
        Assert.Empty(order.Items.Single().Components!);
        Assert.False(order.Items.Single().FreeDelivery);
        Assert.Single(await db.Products.ToListAsync());
    }

    private class NoPhotos : IPhotoService
    {
        public Task<PhotoResult?> UploadPhotoAsync(Stream fileStream, string fileName) => throw new NotSupportedException();
        public Task<bool> DeletePhotoAsync(string publicId) => throw new NotSupportedException();
    }
}
