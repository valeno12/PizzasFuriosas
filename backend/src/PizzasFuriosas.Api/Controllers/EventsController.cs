using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzasFuriosas.Core.Common;
using PizzasFuriosas.Core.DTOs;
using PizzasFuriosas.Core.Entities;
using PizzasFuriosas.Infrastructure.Data;

namespace PizzasFuriosas.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EventsController(AppDbContext context) : ControllerBase
{
    private string CalculateStatus(DateTime eventDate, DateTime? cancelledAt, DateTime? completedAt)
    {
        if (cancelledAt.HasValue) return "Cancelado";
        if (completedAt.HasValue) return "Completado";
        if (eventDate > DateTime.UtcNow) return "Próximo";
        return "Pendiente de cierre";
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = context.Events
            .Include(e => e.Customer)
            .Include(e => e.Surcharges)
            .Include(e => e.Payments)
            .AsQueryable();

        var totalCount = await query.CountAsync();

        var events = await query
            .OrderByDescending(e => e.EventDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var responseList = events.Select(e =>
        {
            var totalSurcharges = e.Surcharges.Sum(s => s.Amount);
            var totalPayments = e.Payments.Sum(p => p.Amount);
            var totalCost = (e.PizzaCount * e.PricePerPizza) + totalSurcharges;
            
            return new EventResponse(
                e.Id,
                e.CustomerId,
                e.Customer?.Name ?? "Cliente Borrado",
                e.EventDate,
                e.Location,
                e.Notes,
                e.PizzaCount,
                e.PricePerPizza,
                e.Deposit,
                totalCost,
                totalCost - e.Deposit - totalPayments,
                CalculateStatus(e.EventDate, e.CancelledAt, e.CompletedAt),
                e.CancelledAt,
                e.CompletedAt,
                e.Surcharges.Select(s => new EventSurchargeResponse(s.Id, s.Description, s.Amount)).ToList(),
                e.Payments.Select(p => new EventPaymentResponse(p.Id, p.PaymentMethod, p.Amount)).ToList()
            );
        }).ToList();

        var paginatedResult = new PaginatedResult<EventResponse>(responseList, totalCount, page, pageSize);
        return Ok(new ApiResponse<PaginatedResult<EventResponse>>(paginatedResult));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var e = await context.Events
            .Include(ev => ev.Customer)
            .Include(ev => ev.Surcharges)
            .Include(ev => ev.Payments)
            .FirstOrDefaultAsync(ev => ev.Id == id);

        if (e == null) return NotFound(ApiResponse.Error("Evento no encontrado"));

        var totalSurcharges = e.Surcharges.Sum(s => s.Amount);
        var totalPayments = e.Payments.Sum(p => p.Amount);
        var totalCost = (e.PizzaCount * e.PricePerPizza) + totalSurcharges;

        var response = new EventResponse(
            e.Id,
            e.CustomerId,
            e.Customer?.Name ?? "Cliente Borrado",
            e.EventDate,
            e.Location,
            e.Notes,
            e.PizzaCount,
            e.PricePerPizza,
            e.Deposit,
            totalCost,
            totalCost - e.Deposit - totalPayments,
            CalculateStatus(e.EventDate, e.CancelledAt, e.CompletedAt),
            e.CancelledAt,
            e.CompletedAt,
            e.Surcharges.Select(s => new EventSurchargeResponse(s.Id, s.Description, s.Amount)).ToList(),
            e.Payments.Select(p => new EventPaymentResponse(p.Id, p.PaymentMethod, p.Amount)).ToList()
        );

        return Ok(new ApiResponse<EventResponse>(response));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateEventRequest request)
    {
        var customerExists = await context.Customers.AnyAsync(c => c.Id == request.CustomerId);
        if (!customerExists) return NotFound(ApiResponse.Error("Cliente no encontrado"));

        var newEvent = new Event
        {
            CustomerId = request.CustomerId,
            EventDate = request.EventDate,
            Location = request.Location,
            Notes = request.Notes,
            PizzaCount = request.PizzaCount,
            PricePerPizza = request.PricePerPizza,
            Deposit = request.Deposit,
            Surcharges = request.Surcharges.Select(s => new EventSurcharge
            {
                Description = s.Description,
                Amount = s.Amount
            }).ToList(),
            Payments = request.Payments.Select(p => new EventPayment
            {
                PaymentMethod = p.PaymentMethod,
                Amount = p.Amount
            }).ToList()
        };

        context.Events.Add(newEvent);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = newEvent.Id }, new ApiResponse<int>(newEvent.Id, "Evento creado exitosamente"));
    }

    [HttpPut("{id}/complete")]
    public async Task<IActionResult> Complete(int id, CompleteEventRequest request)
    {
        var e = await context.Events.FirstOrDefaultAsync(ev => ev.Id == id);
        if (e == null) return NotFound(ApiResponse.Error("Evento no encontrado"));
        
        if (e.CancelledAt.HasValue) return Conflict(ApiResponse.Error("El evento ya fue cancelado"));
        if (e.CompletedAt.HasValue) return Conflict(ApiResponse.Error("El evento ya fue completado"));

        e.CompletedAt = DateTime.UtcNow;
        e.PizzaCount += request.ExtraPizzas;

        foreach (var sur in request.ExtraSurcharges)
        {
            context.EventSurcharges.Add(new EventSurcharge
            {
                EventId = id,
                Description = sur.Description,
                Amount = sur.Amount
            });
        }

        await context.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Evento marcado como completado"));
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var e = await context.Events.FirstOrDefaultAsync(ev => ev.Id == id);
        if (e == null) return NotFound(ApiResponse.Error("Evento no encontrado"));

        if (e.CancelledAt.HasValue) return Conflict(ApiResponse.Error("El evento ya fue cancelado"));
        if (e.CompletedAt.HasValue) return Conflict(ApiResponse.Error("El evento ya fue completado"));

        e.CancelledAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Evento cancelado"));
    }

    [HttpPost("{id}/payments")]
    public async Task<IActionResult> AddPayment(int id, CreateEventPaymentRequest request)
    {
        var e = await context.Events.FirstOrDefaultAsync(ev => ev.Id == id);
        if (e == null) return NotFound(ApiResponse.Error("Evento no encontrado"));

        context.EventPayments.Add(new EventPayment
        {
            EventId = id,
            PaymentMethod = request.PaymentMethod,
            Amount = request.Amount
        });

        await context.SaveChangesAsync();
        return Ok(ApiResponse.Ok("Pago registrado exitosamente"));
    }
}
