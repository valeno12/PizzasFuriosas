using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzasFuriosas.Api.Services;
using PizzasFuriosas.Core.Common;
using PizzasFuriosas.Core.DTOs;

namespace PizzasFuriosas.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CategoriesController(CategoryService categoryService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CategoryResponse>>>> GetAll(CancellationToken cancellationToken)
    {
        var categories = await categoryService.GetAllAsync(cancellationToken);
        return Ok(new ApiResponse<List<CategoryResponse>>(categories));
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> GetById(int id, CancellationToken cancellationToken)
    {
        var category = await categoryService.GetByIdAsync(id, cancellationToken);
        return Ok(new ApiResponse<CategoryResponse>(category));
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> Create(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await categoryService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = category.Id },
            new ApiResponse<CategoryResponse>(category, "Categoría creada"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> Update(int id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await categoryService.UpdateAsync(id, request, cancellationToken);
        return Ok(new ApiResponse<CategoryResponse>(category, "Categoría actualizada"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<ActionResult<ApiResponse>> Delete(int id, CancellationToken cancellationToken)
    {
        await categoryService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Categoría eliminada"));
    }
}
