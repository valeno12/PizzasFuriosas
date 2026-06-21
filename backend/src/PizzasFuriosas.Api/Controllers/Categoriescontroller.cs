using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzasFuriosas.Core.Entities;
using PizzasFuriosas.Infrastructure.Data;

namespace PizzasFuriosas.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
public class CategoriesController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(){
        var categories = await context.Categories.ToListAsync();
        return Ok(categories);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Category category)
    {
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new {id = category.Id}, category);
    }
}