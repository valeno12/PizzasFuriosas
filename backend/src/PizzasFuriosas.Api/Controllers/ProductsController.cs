using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzasFuriosas.Core.DTOs;
using PizzasFuriosas.Core.Entities;
using PizzasFuriosas.Infrastructure.Data;
using PizzasFuriosas.Core.Common;

using PizzasFuriosas.Core.Interfaces;

namespace PizzasFuriosas.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController(AppDbContext context, IPhotoService photoService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ProductResponse>>>> GetAll([FromQuery] int? categoryId, [FromQuery] bool? isAvailable, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = context.Products.AsNoTracking().AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (isAvailable.HasValue)
        {
            query = query.Where(p => p.IsAvailable == isAvailable.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.ToLower().Contains(search.ToLower()));
        }

        var totalCount = await query.CountAsync();

        var products = await query
            .OrderBy(p => p.Name) // paginar sin orden estable puede duplicar/saltear ítems entre páginas
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductResponse(
                p.Id, 
                p.Name, 
                p.Price, 
                p.IsAvailable, 
                p.CategoryId, 
                p.Category != null ? p.Category.Name : "Categoría Borrada",
                p.ImageUrl))
            .ToListAsync();

        var paginatedResult = new PaginatedResult<ProductResponse>(products, totalCount, page, pageSize);
        return Ok(new ApiResponse<PaginatedResult<ProductResponse>>(paginatedResult));
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> GetById(int id)
    {
        var product = await context.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductResponse(
                p.Id, 
                p.Name, 
                p.Price, 
                p.IsAvailable, 
                p.CategoryId, 
                p.Category != null ? p.Category.Name : "Categoría Borrada",
                p.ImageUrl))
            .FirstOrDefaultAsync();

        if (product == null)
        {
            return NotFound(ApiResponse.Error("Producto no encontrado"));
        }

        return Ok(new ApiResponse<ProductResponse>(product));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> Create (CreateProductRequest request)
    {
        if (await context.Products.AnyAsync(p => p.Name.ToLower() == request.Name.ToLower()))
        {
            return Conflict(ApiResponse.Error("Ya existe un producto con ese nombre"));
        }

        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId);
        
        if (category == null)
        {
            return BadRequest(ApiResponse.Error("La categoría especificada no existe"));
        }

        var product = new Product 
        {
            Name = request.Name,
            Price = request.Price,
            CategoryId = request.CategoryId,
            IsAvailable = request.IsAvailable
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var response = new ProductResponse(
            product.Id, 
            product.Name, 
            product.Price, 
            product.IsAvailable, 
            product.CategoryId, 
            category.Name,
            product.ImageUrl
        );

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, new ApiResponse<ProductResponse>(response, $"Se creó el producto {product.Name}"));    
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> Update(int id, UpdateProductRequest request)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound(ApiResponse.Error("Producto no encontrado"));
        }

        if (await context.Products.AnyAsync(p => p.Name.ToLower() == request.Name.ToLower() && p.Id != id))
        {
            return Conflict(ApiResponse.Error("Ya existe un producto con ese nombre"));
        }

        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId);
        
        if (category == null)
        {
            return BadRequest(ApiResponse.Error("La categoría especificada no existe"));
        }

        product.Name = request.Name;
        product.Price = request.Price;
        product.CategoryId = request.CategoryId;
        product.IsAvailable = request.IsAvailable;

        await context.SaveChangesAsync();

        var response = new ProductResponse(
            product.Id, 
            product.Name, 
            product.Price, 
            product.IsAvailable, 
            product.CategoryId, 
            category.Name,
            product.ImageUrl
        );

        return Ok(new ApiResponse<ProductResponse>(response, $"Producto {product.Name} actualizado"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound(ApiResponse.Error("Producto no encontrado"));
        }

        if (!string.IsNullOrEmpty(product.ImagePublicId))
        {
            await photoService.DeletePhotoAsync(product.ImagePublicId);
            product.ImageUrl = null;
            product.ImagePublicId = null;
        }

        product.SoftDelete();
        await context.SaveChangesAsync();
        
        return Ok(ApiResponse.Ok("Producto eliminado"));
    }

    [HttpPost("{id}/image")]
    public async Task<IActionResult> UploadImage(int id, IFormFile file)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return NotFound(ApiResponse.Error("Producto no encontrado"));
        }

        // 1. Validar si viene vacío
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Error("No se envió ningún archivo válido"));
        }

        // 2. Validar tamaño (Ej: Máximo 5 MB)
        long maxSizeInBytes = 5 * 1024 * 1024;
        if (file.Length > maxSizeInBytes)
        {
            return BadRequest(ApiResponse.Error("La imagen es demasiado pesada. El tamaño máximo es 5 MB"));
        }

        // 3. Validar extensión y Content-Type para evitar archivos maliciosos (.exe, .pdf, etc)
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var contentType = file.ContentType.ToLowerInvariant();
        
        if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
        {
            return BadRequest(ApiResponse.Error("Formato no permitido. Solo se aceptan .jpg, .jpeg, .png o .webp"));
        }

        if (!allowedContentTypes.Contains(contentType))
        {
            return BadRequest(ApiResponse.Error("Content-Type no permitido. Solo se aceptan image/jpeg, image/png o image/webp"));
        }

        var result = await photoService.UploadPhotoAsync(file.OpenReadStream(), file.FileName);
        
        if (result == null)
        {
            return BadRequest(ApiResponse.Error("El archivo está vacío o es inválido"));
        }

        // Si ya tenía una foto, la borramos de Cloudinary para no ocupar espacio al pedo
        if (!string.IsNullOrEmpty(product.ImagePublicId))
        {
            await photoService.DeletePhotoAsync(product.ImagePublicId);
        }

        product.ImageUrl = result.Url;
        product.ImagePublicId = result.PublicId;

        await context.SaveChangesAsync();

        return Ok(new ApiResponse<string>(product.ImageUrl, "Foto subida con éxito"));
    }
}