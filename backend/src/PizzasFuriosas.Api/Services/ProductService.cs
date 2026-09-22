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
        p.ImageUrl,
        p.Components.Select(c => new ProductComponentResponse(c.ProductId, c.Product.Name, c.Quantity)).ToList(),
        p.FreeDelivery);

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

        if (isAvailable == true)
            query = query.Where(p => !p.Components.Any(c => c.Product.IsDeleted || !c.Product.IsAvailable));

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

        var components = await ValidateComponentsAsync(null, request.Components ?? [], request.FreeDelivery, cancellationToken);

        var product = new Product
        {
            Name = request.Name,
            Price = request.Price,
            CategoryId = request.CategoryId,
            Components = components,
            FreeDelivery = request.FreeDelivery,
            IsAvailable = request.IsAvailable
        };

        context.Products.Add(product);
        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(product.Id, cancellationToken);
    }

    public async Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await context.Products.Include(p => p.Components).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product == null)
            throw new NotFoundException("Producto no encontrado");

        if (await context.Products.AnyAsync(p => p.Name.ToLower() == request.Name.ToLower() && p.Id != id, cancellationToken))
            throw new ConflictException("Ya existe un producto con ese nombre");

        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (category == null)
            throw new BadRequestException("La categoría especificada no existe");

        var componentRequests = request.Components ?? product.Components
            .Select(c => new ProductComponentRequest(c.ProductId, c.Quantity)).ToList();
        var freeDelivery = request.FreeDelivery ?? product.FreeDelivery;
        var components = await ValidateComponentsAsync(id, componentRequests, freeDelivery, cancellationToken);
        foreach (var existing in product.Components.ToList())
        {
            var replacement = components.FirstOrDefault(c => c.ProductId == existing.ProductId);
            if (replacement == null)
            {
                context.ProductComponents.Remove(existing);
                product.Components.Remove(existing);
            }
            else
            {
                existing.Quantity = replacement.Quantity;
                components.Remove(replacement);
            }
        }
        foreach (var component in components) product.Components.Add(component);
        product.FreeDelivery = freeDelivery;

        product.Name = request.Name;
        product.Price = request.Price;
        product.CategoryId = request.CategoryId;
        product.IsAvailable = request.IsAvailable;

        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(product.Id, cancellationToken);
    }

    private async Task<List<ProductComponent>> ValidateComponentsAsync(int? promotionId,
        List<ProductComponentRequest> components, bool freeDelivery, CancellationToken cancellationToken)
    {
        if (components.Count > 50 || components.Any(c => c.Quantity <= 0 || c.Quantity > 100 || c.ProductId == promotionId)
            || components.Select(c => c.ProductId).Distinct().Count() != components.Count)
            throw new BadRequestException("La promo debe incluir productos distintos, con cantidades entre 1 y 100, y no puede incluirse a sí misma.");
        if (freeDelivery && components.Count == 0)
            throw new BadRequestException("El envío gratis requiere una promo con productos.");
        if (components.Count > 0 && promotionId.HasValue && await context.ProductComponents.AnyAsync(c => c.ProductId == promotionId, cancellationToken))
            throw new BadRequestException("Este producto forma parte de otra promo y no se puede convertir en promo.");
        var ids = components.Select(c => c.ProductId).ToList();
        var validCount = await context.Products.CountAsync(p => ids.Contains(p.Id) && !p.Components.Any(), cancellationToken);
        if (validCount != ids.Count)
            throw new BadRequestException("Los componentes deben ser productos existentes, no otras promos.");
        return components.Select(c => new ProductComponent { ProductId = c.ProductId, Quantity = c.Quantity }).ToList();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var product = await context.Products.Include(p => p.Components).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product == null)
            throw new NotFoundException("Producto no encontrado");

        if (await context.ProductComponents.AnyAsync(c => c.ProductId == id && !c.Promotion.IsDeleted, cancellationToken))
            throw new ConflictException("El producto forma parte de una promo. Quitalo de la promo antes de borrarlo.");

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
        var product = await context.Products.Include(p => p.Components).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
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
