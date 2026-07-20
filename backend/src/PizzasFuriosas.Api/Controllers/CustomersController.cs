using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzasFuriosas.Api.Services;
using PizzasFuriosas.Core.Common;
using PizzasFuriosas.Core.DTOs;

namespace PizzasFuriosas.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CustomersController(CustomerService customerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<CustomerResponse>>>> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await customerService.GetAllAsync(search, page, pageSize, cancellationToken);
        return Ok(new ApiResponse<PaginatedResult<CustomerResponse>>(result));
    }

    [HttpGet("{id}/stats")]
    public async Task<ActionResult<ApiResponse<CustomerStatsResponse>>> GetStats(int id, CancellationToken cancellationToken)
    {
        var stats = await customerService.GetStatsAsync(id, cancellationToken);
        return Ok(new ApiResponse<CustomerStatsResponse>(stats));
    }

    [HttpGet("{id}/addresses")]
    public async Task<ActionResult<ApiResponse<List<AddressResponse>>>> GetAddresses(int id, CancellationToken cancellationToken)
    {
        var addresses = await customerService.GetAddressesAsync(id, cancellationToken);
        return Ok(new ApiResponse<List<AddressResponse>>(addresses));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await customerService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = customer.Id }, new ApiResponse<CustomerResponse>(customer, "Cliente creado"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> Update(int id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await customerService.UpdateAsync(id, request, cancellationToken);
        return Ok(new ApiResponse<CustomerResponse>(customer, "Cliente actualizado"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(int id, CancellationToken cancellationToken)
    {
        await customerService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Cliente eliminado"));
    }

    [HttpPost("{id}/addresses")]
    public async Task<ActionResult<ApiResponse<AddressResponse>>> AddAddress(int id, CreateAddressRequest request, CancellationToken cancellationToken)
    {
        var address = await customerService.AddAddressAsync(id, request, cancellationToken);
        return Ok(new ApiResponse<AddressResponse>(address, "Dirección agregada al cliente"));
    }
}
