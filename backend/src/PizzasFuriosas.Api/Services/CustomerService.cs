using Microsoft.EntityFrameworkCore;
using PizzasFuriosas.Api.Exceptions;
using PizzasFuriosas.Core.Common;
using PizzasFuriosas.Core.DTOs;
using PizzasFuriosas.Core.Entities;
using PizzasFuriosas.Infrastructure.Data;

namespace PizzasFuriosas.Api.Services;

public class CustomerService(AppDbContext context)
{
    public async Task<PaginatedResult<CustomerResponse>> GetAllAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = context.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(c =>
                (c.Phone != null && c.Phone.Contains(search)) ||
                c.Name.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var customers = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerResponse(c.Id, c.Name, c.Phone, c.Notes))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<CustomerResponse>(customers, totalCount, page, pageSize);
    }

    public async Task<CustomerStatsResponse> GetStatsAsync(int id, CancellationToken cancellationToken)
    {
        var customerExists = await context.Customers.AnyAsync(c => c.Id == id, cancellationToken);
        if (!customerExists)
            throw new NotFoundException("Cliente no encontrado");

        var orders = context.Orders.AsNoTracking()
            .Where(o => o.CustomerId == id && o.StatusId != OrderStatuses.Cancelled);

        var totalOrders = await orders.CountAsync(cancellationToken);
        var totalSpent = await orders
            .Where(o => o.StatusId == OrderStatuses.Delivered)
            .SumAsync(o => o.TotalPrice, cancellationToken);

        var favoriteProduct = await context.OrderItems.AsNoTracking()
            .Where(i => i.Order.CustomerId == id && i.Order.StatusId != OrderStatuses.Cancelled)
            .GroupBy(i => i.Product.Name)
            .Select(g => new { Name = g.Key, Quantity = g.Sum(i => i.Quantity) })
            .OrderByDescending(x => x.Quantity)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return new CustomerStatsResponse(totalOrders, totalSpent, favoriteProduct);
    }

    public async Task<List<AddressResponse>> GetAddressesAsync(int id, CancellationToken cancellationToken)
    {
        return await context.Addresses
            .AsNoTracking()
            .Where(a => a.CustomerId == id)
            .Select(a => new AddressResponse(a.Id, a.CustomerId, a.Street, a.Number, a.Apartment, a.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = new Customer
        {
            Name = request.Name,
            Phone = request.Phone,
            Notes = request.Notes
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync(cancellationToken);

        return new CustomerResponse(customer.Id, customer.Name, customer.Phone, customer.Notes);
    }

    public async Task<CustomerResponse> UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (customer == null)
            throw new NotFoundException("Cliente no encontrado");

        customer.Name = request.Name;
        customer.Phone = request.Phone;
        customer.Notes = request.Notes;
        await context.SaveChangesAsync(cancellationToken);

        return new CustomerResponse(customer.Id, customer.Name, customer.Phone, customer.Notes);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (customer == null)
            throw new NotFoundException("Cliente no encontrado");

        customer.SoftDelete();
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AddressResponse> AddAddressAsync(int id, CreateAddressRequest request, CancellationToken cancellationToken)
    {
        var customerExists = await context.Customers.AnyAsync(c => c.Id == id, cancellationToken);
        if (!customerExists)
            throw new NotFoundException("Cliente no encontrado");

        var address = new Address
        {
            CustomerId = id,
            Street = request.Street,
            Number = request.Number,
            Apartment = request.Apartment,
            Notes = request.Notes
        };

        context.Addresses.Add(address);
        await context.SaveChangesAsync(cancellationToken);

        return new AddressResponse(address.Id, address.CustomerId, address.Street, address.Number, address.Apartment, address.Notes);
    }
}
