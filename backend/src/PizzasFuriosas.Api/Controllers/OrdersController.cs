using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzasFuriosas.Api.Services;
using PizzasFuriosas.Core.Common;
using PizzasFuriosas.Core.DTOs;

namespace PizzasFuriosas.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController(OrderService orderService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<OrderResponse>>>> GetAll([FromQuery] OrderFilterDto filter, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await orderService.GetAllAsync(filter, page, pageSize, cancellationToken);
        return Ok(new ApiResponse<PaginatedResult<OrderResponse>>(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<OrderResponse>>> GetById(int id, CancellationToken cancellationToken)
    {
        var order = await orderService.GetByIdAsync(id, cancellationToken);
        return Ok(new ApiResponse<OrderResponse>(order));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<int>>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var orderId = await orderService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = orderId },
            new ApiResponse<int>(orderId, "Pedido cargado exitosamente"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> Update(int id, UpdateOrderRequest request, CancellationToken cancellationToken)
    {
        await orderService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse.Ok("Pedido actualizado"));
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(int id, UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        await orderService.UpdateStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse.Ok("Estado del pedido actualizado"));
    }
}
