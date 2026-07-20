using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzasFuriosas.Api.Services;
using PizzasFuriosas.Core.Common;
using PizzasFuriosas.Core.DTOs;

namespace PizzasFuriosas.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EventsController(EventService eventService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<EventResponse>>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await eventService.GetAllAsync(page, pageSize, cancellationToken);
        return Ok(new ApiResponse<PaginatedResult<EventResponse>>(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<EventResponse>>> GetById(int id, CancellationToken cancellationToken)
    {
        var ev = await eventService.GetByIdAsync(id, cancellationToken);
        return Ok(new ApiResponse<EventResponse>(ev));
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<int>>> Create(CreateEventRequest request, CancellationToken cancellationToken)
    {
        var id = await eventService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new ApiResponse<int>(id, "Evento creado exitosamente"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<ActionResult<ApiResponse>> Update(int id, UpdateEventRequest request, CancellationToken cancellationToken)
    {
        await eventService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse.Ok("Evento actualizado"));
    }

    [HttpPut("{id}/complete")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<ActionResult<ApiResponse>> Complete(int id, CompleteEventRequest request, CancellationToken cancellationToken)
    {
        await eventService.CompleteAsync(id, request, cancellationToken);
        return Ok(ApiResponse.Ok("Evento marcado como completado"));
    }

    [HttpPut("{id}/cancel")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<ActionResult<ApiResponse>> Cancel(int id, CancellationToken cancellationToken)
    {
        await eventService.CancelAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Evento cancelado"));
    }

    [HttpPost("{id}/payments")]
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public async Task<ActionResult<ApiResponse>> AddPayment(int id, CreateEventPaymentRequest request, CancellationToken cancellationToken)
    {
        await eventService.AddPaymentAsync(id, request, cancellationToken);
        return Ok(ApiResponse.Ok("Pago registrado exitosamente"));
    }
}
