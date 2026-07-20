using Microsoft.EntityFrameworkCore;
using PizzasFuriosas.Api.Exceptions;
using PizzasFuriosas.Core.DTOs;
using PizzasFuriosas.Core.Entities;
using PizzasFuriosas.Infrastructure.Data;

namespace PizzasFuriosas.Api.Services;

// Toda la lógica de negocio de categorías vive acá. El controller solo traduce HTTP.
public class CategoryService(AppDbContext context)
{
    public async Task<List<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context.Categories
            .AsNoTracking()
            .Select(c => new CategoryResponse(c.Id, c.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var category = await context.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryResponse(c.Id, c.Name))
            .FirstOrDefaultAsync(cancellationToken);

        if (category == null)
            throw new NotFoundException("Categoría no encontrada");

        return category;
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        if (await context.Categories.AnyAsync(c => c.Name.ToLower() == request.Name.ToLower(), cancellationToken))
            throw new ConflictException("Ya existe una categoría con ese nombre");

        var category = new Category { Name = request.Name };

        context.Categories.Add(category);
        await context.SaveChangesAsync(cancellationToken);

        return new CategoryResponse(category.Id, category.Name);
    }

    public async Task<CategoryResponse> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category == null)
            throw new NotFoundException("Categoría no encontrada");

        if (await context.Categories.AnyAsync(c => c.Name.ToLower() == request.Name.ToLower() && c.Id != id, cancellationToken))
            throw new ConflictException("Ya existe otra categoría con ese nombre");

        category.Name = request.Name;
        await context.SaveChangesAsync(cancellationToken);

        return new CategoryResponse(category.Id, category.Name);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category == null)
            throw new NotFoundException("Categoría no encontrada");

        if (await context.Products.AnyAsync(p => p.CategoryId == id, cancellationToken))
            throw new ConflictException("No se puede eliminar la categoría porque tiene productos asociados");

        category.SoftDelete();
        await context.SaveChangesAsync(cancellationToken);
    }
}
