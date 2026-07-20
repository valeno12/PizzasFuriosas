using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PizzasFuriosas.Api.Exceptions;
using PizzasFuriosas.Core.Common;
using PizzasFuriosas.Core.DTOs;
using PizzasFuriosas.Core.Entities;
using PizzasFuriosas.Core.Interfaces;
using PizzasFuriosas.Infrastructure.Data;

namespace PizzasFuriosas.Api.Services;

public class ProductService(AppDbContext context, IPhotoService photoService)
{

    private static readonly Expression<Func<Product, ProductResponse>> ToResponse = p => new ProductResponse(
        p.Id,
        p.Name,
        p.Price,
        p.IsAvailable,
        p.CategoryId,
        p.Category != null ? p.Category.Name : "Categoría Borrada",
        p.ImageUrl);

    public async Task<PaginatedResult<ProductResponse>> GetAllAsync(
        int? categoryId,
        bool? isAvailable,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = context.Products.AsNoTracking();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (isAvailable.HasValue)
            query = query.Where(p => p.IsAvailable == isAvailable.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.ToLower().Contains(search.ToLower()));

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToResponse)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<ProductResponse>(products, totalCount, page, pageSize);
    }

    public async Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var product = await context.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(ToResponse)
            .FirstOrDefaultAsync(cancellationToken);

        if (product == null)
            throw new NotFoundException("Producto no encontrado");

        return product;
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (await context.Products.AnyAsync(p => p.Name.ToLower() == request.Name.ToLower(), cancellationToken))
            throw new ConflictException("Ya existe un producto con ese nombre");

        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (category == null)
            throw new BadRequestException("La categoría especificada no existe");

        var product = new Product
        {
            Name = request.Name,
            Price = request.Price,
            CategoryId = request.CategoryId,
            IsAvailable = request.IsAvailable
        };

        context.Products.Add(product);
        await context.SaveChangesAsync(cancellationToken);

        return new ProductResponse(
            product.Id, product.Name, product.Price, product.IsAvailable,
            product.CategoryId, category.Name, product.ImageUrl);
    }

    public async Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product == null)
            throw new NotFoundException("Producto no encontrado");

        if (await context.Products.AnyAsync(p => p.Name.ToLower() == request.Name.ToLower() && p.Id != id, cancellationToken))
            throw new ConflictException("Ya existe un producto con ese nombre");

        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (category == null)
            throw new BadRequestException("La categoría especificada no existe");

        product.Name = request.Name;
        product.Price = request.Price;
        product.CategoryId = request.CategoryId;
        product.IsAvailable = request.IsAvailable;

        await context.SaveChangesAsync(cancellationToken);

        return new ProductResponse(
            product.Id, product.Name, product.Price, product.IsAvailable,
            product.CategoryId, category.Name, product.ImageUrl);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product == null)
            throw new NotFoundException("Producto no encontrado");

        if (!string.IsNullOrEmpty(product.ImagePublicId))
        {
            await photoService.DeletePhotoAsync(product.ImagePublicId);
            product.ImageUrl = null;
            product.ImagePublicId = null;
        }

        product.SoftDelete();
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> SetImageAsync(int id, Stream imageStream, string fileName, CancellationToken cancellationToken)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product == null)
            throw new NotFoundException("Producto no encontrado");

        var result = await photoService.UploadPhotoAsync(imageStream, fileName);
        if (result == null)
            throw new BadRequestException("El archivo está vacío o es inválido");

        if (!string.IsNullOrEmpty(product.ImagePublicId))
            await photoService.DeletePhotoAsync(product.ImagePublicId);

        product.ImageUrl = result.Url;
        product.ImagePublicId = result.PublicId;
        await context.SaveChangesAsync(cancellationToken);

        return product.ImageUrl;
    }
}
