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
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = context.Customers.AsQueryable();

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
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerResponse(c.Id, c.Name, c.Phone, c.Notes))
            .ToListAsync();

        var paginatedResult = new PaginatedResult<CustomerResponse>(customers, totalCount, page, pageSize);
        return Ok(new ApiResponse<PaginatedResult<CustomerResponse>>(paginatedResult));
    }

    [HttpGet("{id}/addresses")]
    public async Task<IActionResult> GetAddresses(int id)
    {
        var addresses = await context.Addresses
            .Where(a => a.CustomerId == id)
            .Select(a => new AddressResponse(a.Id, a.CustomerId, a.Street, a.Number, a.Apartment, a.Notes))
            .ToListAsync();

        return Ok(new ApiResponse<List<AddressResponse>>(addresses));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerRequest request)
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
    public async Task<IActionResult> Update(int id, UpdateCustomerRequest request)
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
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return NotFound(ApiResponse.Error("Cliente no encontrado"));

        customer.SoftDelete();
        await context.SaveChangesAsync();
        
        return Ok(ApiResponse.Ok("Cliente eliminado"));
    }

    [HttpPost("{id}/addresses")]
    public async Task<IActionResult> AddAddress(int id, CreateAddressRequest request)
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
