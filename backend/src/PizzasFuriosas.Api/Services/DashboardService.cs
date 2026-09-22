using Microsoft.EntityFrameworkCore;
using PizzasFuriosas.Core.Common;
using PizzasFuriosas.Core.DTOs;
using PizzasFuriosas.Infrastructure.Data;

namespace PizzasFuriosas.Api.Services;

public class DashboardService(AppDbContext context)
{
    public async Task<BalanceResponse> GetBalanceAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        var startDate = UtcOrMin(from);
        var endDate = UtcOrMax(to);

        var ordersQuery = context.Orders
            .AsNoTracking()
            .Where(o => o.StatusId == OrderStatuses.Delivered && o.CreatedAt >= startDate && o.CreatedAt <= endDate);

        var totalOrdersIncome = await ordersQuery.SumAsync(o => o.TotalPrice, cancellationToken);
        var deliveryFees = await ordersQuery.SumAsync(o => o.DeliveryCost, cancellationToken);

        var eventsList = await context.Events
            .AsNoTracking()
            .Include(e => e.Surcharges)
            .Where(e => e.CompletedAt != null && e.EventDate >= startDate && e.EventDate <= endDate)
            .ToListAsync(cancellationToken);

        var totalEventsIncome = eventsList.Sum(e =>
            (e.PizzaCount * e.PricePerPizza) + e.Surcharges.Sum(s => s.Amount));

        var totalExpenses = await context.Purchases
            .AsNoTracking()
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .SumAsync(p => p.Amount, cancellationToken);

        var totalGrossIncome = totalOrdersIncome + totalEventsIncome;
        var netSalesIncome = totalGrossIncome - deliveryFees;
        var netProfit = netSalesIncome - totalExpenses;

        return new BalanceResponse(
            TotalGrossIncome: totalGrossIncome,
            TotalOrdersIncome: totalOrdersIncome,
            TotalEventsIncome: totalEventsIncome,
            DeliveryFees: deliveryFees,
            NetSalesIncome: netSalesIncome,
            TotalExpenses: totalExpenses,
            NetProfit: netProfit);
    }

    public async Task<StatisticsResponse> GetStatisticsAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        var startDate = UtcOrMin(from);
        var endDate = UtcOrMax(to);

        var allOrders = await context.Orders
            .AsNoTracking()
            .Include(o => o.Items).ThenInclude(i => i.Components)
            .Include(o => o.Customer)
            .Where(o => o.StatusId == OrderStatuses.Delivered && o.CreatedAt >= startDate && o.CreatedAt <= endDate)
            .ToListAsync(cancellationToken);

        var allItems = allOrders.SelectMany(o => o.Items).ToList();

        // No unir Items con Products: un producto borrado no debe ocultar una venta.
        var legacyProductIds = allItems.Where(i => i.ProductNameSnapshot == null).Select(i => i.ProductId).Distinct().ToList();
        var legacyNames = await context.Products.Where(p => legacyProductIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        var topProducts = allItems
            .SelectMany(i => i.Components.Count > 0
                ? i.Components.Select(c => new { c.ProductId, Name = c.ProductName, Quantity = c.Quantity * i.Quantity })
                : new[] { new { i.ProductId, Name = i.ProductNameSnapshot ?? legacyNames.GetValueOrDefault(i.ProductId, "Producto Borrado"), i.Quantity } })
            .GroupBy(i => i.ProductId)
            .Select(g => new TopProductDto(
                g.Key,
                g.First().Name,
                g.Sum(i => i.Quantity)))
            .OrderByDescending(p => p.TotalQuantitySold)
            .Take(5)
            .ToList();

        var topCustomers = allOrders
            .GroupBy(o => o.CustomerId)
            .Select(g => new TopCustomerDto(
                g.Key,
                g.First().Customer?.Name ?? "Cliente Borrado",
                g.Count(),
                g.Sum(o => o.TotalPrice)))
            .OrderByDescending(c => c.TotalSpent)
            .Take(5)
            .ToList();

        decimal averageOrderValue = allOrders.Any() ? allOrders.Average(o => o.TotalPrice) : 0;

        int totalDelivery = allOrders.Count(o => o.ShippingMethod.Contains("Delivery", StringComparison.OrdinalIgnoreCase));
        int totalTakeAway = allOrders.Count(o => o.ShippingMethod.Contains("Take Away", StringComparison.OrdinalIgnoreCase));
        int totalCash = allOrders.Count(o => o.PaymentMethod.Contains("Efectivo", StringComparison.OrdinalIgnoreCase));
        int totalTransfer = allOrders.Count(o => o.PaymentMethod.Contains("Transferencia", StringComparison.OrdinalIgnoreCase));

        return new StatisticsResponse(
            TopProducts: topProducts,
            TopCustomers: topCustomers,
            AverageOrderValue: Math.Round(averageOrderValue, 2),
            TotalDeliveryOrders: totalDelivery,
            TotalTakeAwayOrders: totalTakeAway,
            TotalCashPayments: totalCash,
            TotalTransferPayments: totalTransfer,
            TopPromotions: allItems.Where(i => i.Components.Count > 0).GroupBy(i => i.ProductId)
                .Select(g => new TopProductDto(g.Key, g.First().ProductNameSnapshot ?? "Promo", g.Sum(i => i.Quantity)))
                .OrderByDescending(p => p.TotalQuantitySold).Take(5).ToList());
    }

    // Npgsql exige DateTime en UTC para "timestamp with time zone".
    private static DateTime UtcOrMin(DateTime? value) =>
        value?.ToUniversalTime() ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

    private static DateTime UtcOrMax(DateTime? value) =>
        value?.ToUniversalTime() ?? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
}
