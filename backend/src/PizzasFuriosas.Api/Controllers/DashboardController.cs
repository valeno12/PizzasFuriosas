using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzasFuriosas.Core.Common;
using PizzasFuriosas.Core.DTOs;
using PizzasFuriosas.Infrastructure.Data;

namespace PizzasFuriosas.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController(AppDbContext context) : ControllerBase
{
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var startDate = from ?? DateTime.MinValue;
        var endDate = to ?? DateTime.MaxValue;

        var ordersQuery = context.Orders
            .Where(o => o.StatusId == 5 && o.CreatedAt >= startDate && o.CreatedAt <= endDate);

        var totalOrdersIncome = await ordersQuery.SumAsync(o => o.TotalPrice);
        var deliveryFees = await ordersQuery.SumAsync(o => o.DeliveryCost);

        var eventsList = await context.Events
            .Include(e => e.Surcharges)
            .Where(e => e.CompletedAt != null && e.EventDate >= startDate && e.EventDate <= endDate)
            .ToListAsync();

        var totalEventsIncome = eventsList.Sum(e => 
            (e.PizzaCount * e.PricePerPizza) + e.Surcharges.Sum(s => s.Amount)
        );

        var totalExpenses = await context.Purchases
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .SumAsync(p => p.Amount);

        var totalGrossIncome = totalOrdersIncome + totalEventsIncome;
        var netSalesIncome = totalGrossIncome - deliveryFees; 
        var netProfit = netSalesIncome - totalExpenses;      
        var response = new BalanceResponse(
            TotalGrossIncome: totalGrossIncome,
            TotalOrdersIncome: totalOrdersIncome,
            TotalEventsIncome: totalEventsIncome,
            DeliveryFees: deliveryFees,
            NetSalesIncome: netSalesIncome,
            TotalExpenses: totalExpenses,
            NetProfit: netProfit
        );

        return Ok(new ApiResponse<BalanceResponse>(response));
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var startDate = from ?? DateTime.MinValue;
        var endDate = to ?? DateTime.MaxValue;

        var completedOrdersQuery = context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Include(o => o.Customer)
            .Where(o => o.StatusId == 5 && o.CreatedAt >= startDate && o.CreatedAt <= endDate);

        var allOrders = await completedOrdersQuery.ToListAsync();

        // 1. Top Products
        var allItems = allOrders.SelectMany(o => o.Items).ToList();
        
        var topProducts = allItems
            .GroupBy(i => i.ProductId)
            .Select(g => new TopProductDto(
                g.Key, 
                g.First().Product?.Name ?? "Producto Borrado",
                g.Sum(i => i.Quantity)))
            .OrderByDescending(p => p.TotalQuantitySold)
            .Take(5)
            .ToList();

        // 2. Top Customers
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

        // 3. Ticket Promedio
        decimal averageOrderValue = allOrders.Any() ? allOrders.Average(o => o.TotalPrice) : 0;

        // 4. Delivery vs TakeAway
        int totalDelivery = allOrders.Count(o => o.ShippingMethod.Contains("Delivery", StringComparison.OrdinalIgnoreCase));
        int totalTakeAway = allOrders.Count(o => o.ShippingMethod.Contains("Take Away", StringComparison.OrdinalIgnoreCase));

        // 5. Pagos
        int totalCash = allOrders.Count(o => o.PaymentMethod.Contains("Efectivo", StringComparison.OrdinalIgnoreCase));
        int totalTransfer = allOrders.Count(o => o.PaymentMethod.Contains("Transferencia", StringComparison.OrdinalIgnoreCase));

        var response = new StatisticsResponse(
            TopProducts: topProducts,
            TopCustomers: topCustomers,
            AverageOrderValue: Math.Round(averageOrderValue, 2),
            TotalDeliveryOrders: totalDelivery,
            TotalTakeAwayOrders: totalTakeAway,
            TotalCashPayments: totalCash,
            TotalTransferPayments: totalTransfer
        );

        return Ok(new ApiResponse<StatisticsResponse>(response));
    }
}
