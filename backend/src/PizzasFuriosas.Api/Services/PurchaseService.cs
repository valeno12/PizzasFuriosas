using Microsoft.EntityFrameworkCore;
using PizzasFuriosas.Api.Exceptions;
using PizzasFuriosas.Core.Common;
using PizzasFuriosas.Core.DTOs;
using PizzasFuriosas.Core.Entities;
using PizzasFuriosas.Infrastructure.Data;

namespace PizzasFuriosas.Api.Services;

public class PurchaseService(AppDbContext context)
{
    public async Task<PaginatedResult<PurchaseResponse>> GetAllAsync(string? category, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = context.Purchases.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category.ToLower() == category.ToLower());
        if (from.HasValue)
            query = query.Where(p => p.Date >= from.Value);
        if (to.HasValue)
            query = query.Where(p => p.Date <= to.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var purchases = await query
            .OrderByDescending(p => p.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PurchaseResponse(p.Id, p.Date, p.Description, p.Amount, p.Category))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<PurchaseResponse>(purchases, totalCount, page, pageSize);
    }

    public async Task<PurchaseResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var purchase = await context.Purchases
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PurchaseResponse(p.Id, p.Date, p.Description, p.Amount, p.Category))
            .FirstOrDefaultAsync(cancellationToken);

        if (purchase == null)
            throw new NotFoundException("Gasto no encontrado");

        return purchase;
    }

    public async Task<PurchaseResponse> CreateAsync(CreatePurchaseRequest request, CancellationToken cancellationToken)
    {
        var purchase = new Purchase
        {
            Date = request.Date,
            Description = request.Description,
            Amount = request.Amount,
            Category = request.Category
        };

        context.Purchases.Add(purchase);
        await context.SaveChangesAsync(cancellationToken);

        return new PurchaseResponse(purchase.Id, purchase.Date, purchase.Description, purchase.Amount, purchase.Category);
    }

    public async Task<PurchaseResponse> UpdateAsync(int id, UpdatePurchaseRequest request, CancellationToken cancellationToken)
    {
        var purchase = await context.Purchases.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (purchase == null)
            throw new NotFoundException("Gasto no encontrado");

        purchase.Date = request.Date;
        purchase.Description = request.Description;
        purchase.Amount = request.Amount;
        purchase.Category = request.Category;
        await context.SaveChangesAsync(cancellationToken);

        return new PurchaseResponse(purchase.Id, purchase.Date, purchase.Description, purchase.Amount, purchase.Category);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var purchase = await context.Purchases.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (purchase == null)
            throw new NotFoundException("Gasto no encontrado");

        purchase.SoftDelete();
        await context.SaveChangesAsync(cancellationToken);
    }
}
