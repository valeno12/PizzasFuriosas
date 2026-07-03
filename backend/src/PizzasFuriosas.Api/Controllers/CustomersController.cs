using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzasFuriosas.Core.Common;
using PizzasFuriosas.Core.DTOs;
using PizzasFuriosas.Core.Entities;
using PizzasFuriosas.Infrastructure.Data;

namespace PizzasFuriosas.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CustomersController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<CustomerResponse>>>> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = context.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(c => 
                (c.Phone != null && c.Phone.Contains(search)) || 
                c.Name.ToLower().Contains(searchLower)
            );
        }

        var totalCount = await query.CountAsync();

        var customers = await query
            .OrderBy(c => c.Name) // paginar sin orden estable puede duplicar/saltear ítems entre páginas
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerResponse(c.Id, c.Name, c.Phone, c.Notes))
            .ToListAsync();

        var paginatedResult = new PaginatedResult<CustomerResponse>(customers, totalCount, page, pageSize);
        return Ok(new ApiResponse<PaginatedResult<CustomerResponse>>(paginatedResult));
    }

    // Resumen para la ficha del cliente: pedidos totales, gastado y producto favorito.
    // Se excluyen cancelados; lo gastado cuenta solo pedidos entregados.
    [HttpGet("{id}/stats")]
    public async Task<ActionResult<ApiResponse<CustomerStatsResponse>>> GetStats(int id)
    {
        var customerExists = await context.Customers.AnyAsync(c => c.Id == id);
        if (!customerExists) return NotFound(ApiResponse.Error("Cliente no encontrado"));

        var orders = context.Orders.AsNoTracking().Where(o => o.CustomerId == id && o.StatusId != 6);

        var totalOrders = await orders.CountAsync();
        var totalSpent = await orders.Where(o => o.StatusId == 5).SumAsync(o => o.TotalPrice);

        var favoriteProduct = await context.OrderItems.AsNoTracking()
            .Where(i => i.Order.CustomerId == id && i.Order.StatusId != 6)
            .GroupBy(i => i.Product.Name)
            .Select(g => new { Name = g.Key, Quantity = g.Sum(i => i.Quantity) })
            .OrderByDescending(x => x.Quantity)
            .Select(x => x.Name)
            .FirstOrDefaultAsync();

        var response = new CustomerStatsResponse(totalOrders, totalSpent, favoriteProduct);
        return Ok(new ApiResponse<CustomerStatsResponse>(response));
    }

    [HttpGet("{id}/addresses")]
    public async Task<ActionResult<ApiResponse<List<AddressResponse>>>> GetAddresses(int id)
    {
        var addresses = await context.Addresses
            .AsNoTracking()
            .Where(a => a.CustomerId == id)
            .Select(a => new AddressResponse(a.Id, a.CustomerId, a.Street, a.Number, a.Apartment, a.Notes))
            .ToListAsync();

        return Ok(new ApiResponse<List<AddressResponse>>(addresses));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> Create(CreateCustomerRequest request)
    {
        var customer = new Customer
        {
            Name = request.Name,
            Phone = request.Phone,
            Notes = request.Notes
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var response = new CustomerResponse(customer.Id, customer.Name, customer.Phone, customer.Notes);
        return CreatedAtAction(nameof(GetAll), new { id = customer.Id }, new ApiResponse<CustomerResponse>(response, "Cliente creado"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> Update(int id, UpdateCustomerRequest request)
    {
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return NotFound(ApiResponse.Error("Cliente no encontrado"));

        customer.Name = request.Name;
        customer.Phone = request.Phone;
        customer.Notes = request.Notes;

        await context.SaveChangesAsync();
        
        var response = new CustomerResponse(customer.Id, customer.Name, customer.Phone, customer.Notes);
        return Ok(new ApiResponse<CustomerResponse>(response, "Cliente actualizado"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return NotFound(ApiResponse.Error("Cliente no encontrado"));

        customer.SoftDelete();
        await context.SaveChangesAsync();
        
        return Ok(ApiResponse.Ok("Cliente eliminado"));
    }

    [HttpPost("{id}/addresses")]
    public async Task<ActionResult<ApiResponse<AddressResponse>>> AddAddress(int id, CreateAddressRequest request)
    {
        var customerExists = await context.Customers.AnyAsync(c => c.Id == id);
        if (!customerExists) return NotFound(ApiResponse.Error("Cliente no encontrado"));

        var address = new Address
        {
            CustomerId = id,
            Street = request.Street,
            Number = request.Number,
            Apartment = request.Apartment,
            Notes = request.Notes
        };

        context.Addresses.Add(address);
        await context.SaveChangesAsync();

        var response = new AddressResponse(address.Id, address.CustomerId, address.Street, address.Number, address.Apartment, address.Notes);
        return Ok(new ApiResponse<AddressResponse>(response, "Dirección agregada al cliente"));
    }
}
