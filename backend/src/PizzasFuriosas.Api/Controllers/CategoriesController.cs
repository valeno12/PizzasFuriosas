using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzasFuriosas.Core.DTOs;
using PizzasFuriosas.Core.Entities;
using PizzasFuriosas.Infrastructure.Data;
using PizzasFuriosas.Core.Common;

namespace PizzasFuriosas.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CategoriesController(AppDbContext context) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<CategoryResponse>>>> GetAll()
    {
        var categories = await context.Categories
            .AsNoTracking()
            .Select(c => new CategoryResponse(c.Id, c.Name))
            .ToListAsync();

        return Ok(new ApiResponse<List<CategoryResponse>>(categories));
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> GetById(int id)
    {
        var category = await context.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryResponse(c.Id, c.Name))
            .FirstOrDefaultAsync();

        if (category == null)
        {
            return NotFound(ApiResponse.Error("Categoría no encontrada"));
        }
        return Ok(new ApiResponse<CategoryResponse>(category));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> Create(CreateCategoryRequest request)
    {
        if (await context.Categories.AnyAsync(c => c.Name.ToLower() == request.Name.ToLower()))
        {
            return Conflict(ApiResponse.Error("Ya existe una categoría con ese nombre"));
        }

        var category = new Category { Name = request.Name };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var response = new CategoryResponse(category.Id, category.Name);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, new ApiResponse<CategoryResponse>(response, "Categoría creada"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> Update(int id, UpdateCategoryRequest request)
    {
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return NotFound(ApiResponse.Error("Categoría no encontrada"));
        }

        if (await context.Categories.AnyAsync(c => c.Name.ToLower() == request.Name.ToLower() && c.Id != id))
        {
            return Conflict(ApiResponse.Error("Ya existe otra categoría con ese nombre"));
        }

        category.Name = request.Name;
        await context.SaveChangesAsync();

        var response = new CategoryResponse(category.Id, category.Name);
        return Ok(new ApiResponse<CategoryResponse>(response, "Categoría actualizada"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return NotFound(ApiResponse.Error("Categoría no encontrada"));
        }

        if (await context.Products.AnyAsync(p => p.CategoryId == id))
        {
            return Conflict(ApiResponse.Error("No se puede eliminar la categoría porque tiene productos asociados"));
        }

        category.SoftDelete();
        await context.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Categoría eliminada"));
    }
}