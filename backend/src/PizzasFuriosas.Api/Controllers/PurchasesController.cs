using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzasFuriosas.Api.Services;
using PizzasFuriosas.Core.Common;
using PizzasFuriosas.Core.DTOs;

namespace PizzasFuriosas.Api.Controllers;

[Authorize(Policy = AppPolicies.AdminOnly)]
[ApiController]
[Route("api/[controller]")]
public class PurchasesController(PurchaseService purchaseService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<PurchaseResponse>>>> GetAll([FromQuery] string? category, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await purchaseService.GetAllAsync(category, from, to, page, pageSize, cancellationToken);
        return Ok(new ApiResponse<PaginatedResult<PurchaseResponse>>(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PurchaseResponse>>> GetById(int id, CancellationToken cancellationToken)
    {
        var purchase = await purchaseService.GetByIdAsync(id, cancellationToken);
        return Ok(new ApiResponse<PurchaseResponse>(purchase));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PurchaseResponse>>> Create(CreatePurchaseRequest request, CancellationToken cancellationToken)
    {
        var purchase = await purchaseService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = purchase.Id }, new ApiResponse<PurchaseResponse>(purchase, "Gasto registrado exitosamente"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<PurchaseResponse>>> Update(int id, UpdatePurchaseRequest request, CancellationToken cancellationToken)
    {
        var purchase = await purchaseService.UpdateAsync(id, request, cancellationToken);
        return Ok(new ApiResponse<PurchaseResponse>(purchase, "Gasto actualizado"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(int id, CancellationToken cancellationToken)
    {
        await purchaseService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Gasto eliminado exitosamente"));
    }
}
