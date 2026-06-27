namespace PizzasFuriosas.Core.DTOs;

public record BalanceResponse(
    decimal TotalGrossIncome,
    decimal TotalOrdersIncome,
    decimal TotalEventsIncome,
    decimal DeliveryFees,
    decimal NetSalesIncome,
    decimal TotalExpenses,
    decimal NetProfit
);

public record TopProductDto(int ProductId, string ProductName, int TotalQuantitySold);
public record TopCustomerDto(int CustomerId, string CustomerName, int TotalOrders, decimal TotalSpent);

public record StatisticsResponse(
    List<TopProductDto> TopProducts,
    List<TopCustomerDto> TopCustomers,
    decimal AverageOrderValue,
    int TotalDeliveryOrders,
    int TotalTakeAwayOrders,
    int TotalCashPayments,
    int TotalTransferPayments
);
