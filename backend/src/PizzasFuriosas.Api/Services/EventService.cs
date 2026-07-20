using Microsoft.EntityFrameworkCore;
using PizzasFuriosas.Api.Exceptions;
using PizzasFuriosas.Core.Common;
using PizzasFuriosas.Core.DTOs;
using PizzasFuriosas.Core.Entities;
using PizzasFuriosas.Infrastructure.Data;

namespace PizzasFuriosas.Api.Services;

public class EventService(AppDbContext context)
{
    public async Task<PaginatedResult<EventResponse>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = context.Events
            .AsNoTracking()
            .Include(e => e.Customer)
            .Include(e => e.Surcharges)
            .Include(e => e.Payments);

        var totalCount = await query.CountAsync(cancellationToken);

        var events = await query
            .OrderByDescending(e => e.EventDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var responseList = events.Select(MapToResponse).ToList();
        return new PaginatedResult<EventResponse>(responseList, totalCount, page, pageSize);
    }

    public async Task<EventResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var ev = await context.Events
            .AsNoTracking()
            .Include(e => e.Customer)
            .Include(e => e.Surcharges)
            .Include(e => e.Payments)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (ev == null)
            throw new NotFoundException("Evento no encontrado");

        return MapToResponse(ev);
    }

    public async Task<int> CreateAsync(CreateEventRequest request, CancellationToken cancellationToken)
    {
        var customerExists = await context.Customers.AnyAsync(c => c.Id == request.CustomerId, cancellationToken);
        if (!customerExists)
            throw new NotFoundException("Cliente no encontrado");

        var newEvent = new Event
        {
            CustomerId = request.CustomerId,
            EventDate = request.EventDate,
            Location = request.Location,
            Notes = request.Notes,
            PizzaCount = request.PizzaCount,
            PricePerPizza = request.PricePerPizza,
            Deposit = request.Deposit,
            Surcharges = request.Surcharges.Select(s => new EventSurcharge { Description = s.Description, Amount = s.Amount }).ToList(),
            Payments = request.Payments.Select(p => new EventPayment { PaymentMethod = p.PaymentMethod, Amount = p.Amount }).ToList()
        };

        context.Events.Add(newEvent);
        await context.SaveChangesAsync(cancellationToken);
        return newEvent.Id;
    }

    public async Task UpdateAsync(int id, UpdateEventRequest request, CancellationToken cancellationToken)
    {
        var ev = await context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (ev == null)
            throw new NotFoundException("Evento no encontrado");

        if (ev.CancelledAt.HasValue || ev.CompletedAt.HasValue)
            throw new ConflictException("Un evento cerrado no se puede editar");

        ev.EventDate = request.EventDate;
        ev.Location = request.Location;
        ev.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        ev.PizzaCount = request.PizzaCount;
        ev.PricePerPizza = request.PricePerPizza;
        ev.Deposit = request.Deposit;

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteAsync(int id, CompleteEventRequest request, CancellationToken cancellationToken)
    {
        var ev = await context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (ev == null)
            throw new NotFoundException("Evento no encontrado");

        if (ev.CancelledAt.HasValue)
            throw new ConflictException("El evento ya fue cancelado");
        if (ev.CompletedAt.HasValue)
            throw new ConflictException("El evento ya fue completado");

        ev.CompletedAt = DateTime.UtcNow;
        ev.PizzaCount += request.ExtraPizzas;

        foreach (var sur in request.ExtraSurcharges)
        {
            context.EventSurcharges.Add(new EventSurcharge { EventId = id, Description = sur.Description, Amount = sur.Amount });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(int id, CancellationToken cancellationToken)
    {
        var ev = await context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (ev == null)
            throw new NotFoundException("Evento no encontrado");

        if (ev.CancelledAt.HasValue)
            throw new ConflictException("El evento ya fue cancelado");
        if (ev.CompletedAt.HasValue)
            throw new ConflictException("El evento ya fue completado");

        ev.CancelledAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddPaymentAsync(int id, CreateEventPaymentRequest request, CancellationToken cancellationToken)
    {
        var ev = await context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (ev == null)
            throw new NotFoundException("Evento no encontrado");

        if (ev.CancelledAt.HasValue)
            throw new ConflictException("El evento está cancelado; no admite pagos");

        context.EventPayments.Add(new EventPayment { EventId = id, PaymentMethod = request.PaymentMethod, Amount = request.Amount });
        await context.SaveChangesAsync(cancellationToken);
    }

    private static EventResponse MapToResponse(Event e)
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
            e.Payments.Select(p => new EventPaymentResponse(p.Id, p.PaymentMethod, p.Amount)).ToList());
    }

    private static string CalculateStatus(DateTime eventDate, DateTime? cancelledAt, DateTime? completedAt)
    {
        if (cancelledAt.HasValue) return "Cancelado";
        if (completedAt.HasValue) return "Completado";
        if (eventDate > DateTime.UtcNow) return "Próximo";
        return "Pendiente de cierre";
    }
}
