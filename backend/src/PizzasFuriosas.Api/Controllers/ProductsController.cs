using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzasFuriosas.Api.Services;
using PizzasFuriosas.Core.Common;
using PizzasFuriosas.Core.DTOs;

namespace PizzasFuriosas.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController(ProductService productService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ProductResponse>>>> GetAll(
        [FromQuery] int? categoryId,
        [FromQuery] bool? isAvailable,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await productService.GetAllAsync(categoryId, isAvailable, search, page, pageSize, cancellationToken);
        return Ok(new ApiResponse<PaginatedResult<ProductResponse>>(result));
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await productService.GetByIdAsync(id, cancellationToken);
        return Ok(new ApiResponse<ProductResponse>(product));
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await productService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id },
            new ApiResponse<ProductResponse>(product, $"Se creó el producto {product.Name}"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> Update(int id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await productService.UpdateAsync(id, request, cancellationToken);
        return Ok(new ApiResponse<ProductResponse>(product, $"Producto {product.Name} actualizado"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await productService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Producto eliminado"));
    }

    [HttpPost("{id}/image")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<IActionResult> UploadImage(int id, IFormFile file, CancellationToken cancellationToken)
    {
        // Validación de la ENTRADA (forma del archivo). Va acá, en el borde, porque IFormFile
        // es un tipo de la web y estas reglas son sobre el upload HTTP, no sobre el negocio.

        // 1. ¿Viene vacío?
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse.Error("No se envió ningún archivo válido"));

        // 2. Tamaño máximo (5 MB)
        const long maxSizeInBytes = 5 * 1024 * 1024;
        if (file.Length > maxSizeInBytes)
            return BadRequest(ApiResponse.Error("La imagen es demasiado pesada. El tamaño máximo es 5 MB"));

        // 3. Extensión y Content-Type, para evitar archivos maliciosos (.exe, .pdf, etc.)
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var contentType = file.ContentType.ToLowerInvariant();

        if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
            return BadRequest(ApiResponse.Error("Formato no permitido. Solo se aceptan .jpg, .jpeg, .png o .webp"));

        if (!allowedContentTypes.Contains(contentType))
            return BadRequest(ApiResponse.Error("Content-Type no permitido. Solo se aceptan image/jpeg, image/png o image/webp"));

        // Validado el archivo, delegamos la lógica de negocio al service (con un Stream pelado).
        var imageUrl = await productService.SetImageAsync(id, file.OpenReadStream(), file.FileName, cancellationToken);
        return Ok(new ApiResponse<string>(imageUrl, "Foto subida con éxito"));
    }
}
