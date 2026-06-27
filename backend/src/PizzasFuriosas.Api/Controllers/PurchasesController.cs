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
public class PurchasesController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? category, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = context.Purchases.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category.ToLower() == category.ToLower());

        if (from.HasValue)
            query = query.Where(p => p.Date >= from.Value);

        if (to.HasValue)
            query = query.Where(p => p.Date <= to.Value);

        var totalCount = await query.CountAsync();

        var purchases = await query
            .OrderByDescending(p => p.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PurchaseResponse(p.Id, p.Date, p.Description, p.Amount, p.Category))
            .ToListAsync();

        var paginatedResult = new PaginatedResult<PurchaseResponse>(purchases, totalCount, page, pageSize);
        return Ok(new ApiResponse<PaginatedResult<PurchaseResponse>>(paginatedResult));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var purchase = await context.Purchases
            .Where(p => p.Id == id)
            .Select(p => new PurchaseResponse(p.Id, p.Date, p.Description, p.Amount, p.Category))
            .FirstOrDefaultAsync();

        if (purchase == null) return NotFound(ApiResponse.Error("Gasto no encontrado"));

        return Ok(new ApiResponse<PurchaseResponse>(purchase));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePurchaseRequest request)
    {
        var purchase = new Purchase
        {
            Date = request.Date,
            Description = request.Description,
            Amount = request.Amount,
            Category = request.Category
        };

        context.Purchases.Add(purchase);
        await context.SaveChangesAsync();

        var response = new PurchaseResponse(purchase.Id, purchase.Date, purchase.Description, purchase.Amount, purchase.Category);
        return CreatedAtAction(nameof(GetById), new { id = purchase.Id }, new ApiResponse<PurchaseResponse>(response, "Gasto registrado exitosamente"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdatePurchaseRequest request)
    {
        var purchase = await context.Purchases.FirstOrDefaultAsync(p => p.Id == id);
        if (purchase == null) return NotFound(ApiResponse.Error("Gasto no encontrado"));

        purchase.Date = request.Date;
        purchase.Description = request.Description;
        purchase.Amount = request.Amount;
        purchase.Category = request.Category;

        await context.SaveChangesAsync();

        var response = new PurchaseResponse(purchase.Id, purchase.Date, purchase.Description, purchase.Amount, purchase.Category);
        return Ok(new ApiResponse<PurchaseResponse>(response, "Gasto actualizado"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var purchase = await context.Purchases.FirstOrDefaultAsync(p => p.Id == id);
        if (purchase == null) return NotFound(ApiResponse.Error("Gasto no encontrado"));

        purchase.SoftDelete();
        await context.SaveChangesAsync();

        return Ok(ApiResponse.Ok("Gasto eliminado exitosamente"));
    }
}
