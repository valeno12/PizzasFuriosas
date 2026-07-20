using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PizzasFuriosas.Api.Exceptions;
using PizzasFuriosas.Core.Common;
using PizzasFuriosas.Core.DTOs;
using PizzasFuriosas.Core.Entities;
using PizzasFuriosas.Infrastructure.Data;

namespace PizzasFuriosas.Api.Services;

public class OrderService(AppDbContext context)
{
    private const string DeliveryShippingMethod = "Delivery";

    private static readonly Expression<Func<Order, OrderResponse>> ToResponse = o => new OrderResponse(
        o.Id,
        o.CustomerId,
        o.Customer != null ? o.Customer.Name : "Cliente Borrado",
        o.Customer != null ? o.Customer.Phone : null,
        o.ShippingMethod,
        o.DeliveryCost,
        o.PaymentMethod,
        o.StatusId,
        o.Status != null ? o.Status.Name : "Estado Borrado",
        o.TotalPrice,
        o.CreatedAt,
        o.ScheduledFor,
        o.Notes,
        o.Address != null
            ? new OrderAddressResponse(o.Address.Id, o.Address.Street, o.Address.Number, o.Address.Apartment, o.Address.Notes)
            : null,
        o.Items.Select(i => new OrderItemResponse(
            i.Id,
            i.ProductId,
            i.Product != null ? i.Product.Name : "Producto Borrado",
            i.Quantity,
            i.UnitPrice,
            i.Quantity * i.UnitPrice)).ToList());

    public async Task<PaginatedResult<OrderResponse>> GetAllAsync(OrderFilterDto filter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 1000);

        var query = context.Orders.AsNoTracking();

        if (filter.StatusId.HasValue)
            query = query.Where(o => o.StatusId == filter.StatusId.Value);
        if (filter.CustomerId.HasValue)
            query = query.Where(o => o.CustomerId == filter.CustomerId.Value);
        if (filter.ProductId.HasValue)
            query = query.Where(o => o.Items.Any(i => i.ProductId == filter.ProductId.Value));
        if (!string.IsNullOrWhiteSpace(filter.ShippingMethod))
            query = query.Where(o => o.ShippingMethod.ToLower() == filter.ShippingMethod.ToLower());
        if (!string.IsNullOrWhiteSpace(filter.PaymentMethod))
            query = query.Where(o => o.PaymentMethod.ToLower() == filter.PaymentMethod.ToLower());
        if (filter.From.HasValue)
            query = query.Where(o => o.CreatedAt >= filter.From.Value);
        if (filter.To.HasValue)
            query = query.Where(o => o.CreatedAt <= filter.To.Value);
        if (filter.OnlyActive == true)
            query = query.Where(o => o.StatusId != OrderStatuses.Delivered && o.StatusId != OrderStatuses.Cancelled);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchLower = filter.Search.ToLower();
            query = query.Where(o =>
                o.Id.ToString().Contains(searchLower) ||
                (o.Customer != null && o.Customer.Name.ToLower().Contains(searchLower)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToResponse)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<OrderResponse>(orders, totalCount, page, pageSize);
    }

    public async Task<OrderResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await context.Orders
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(ToResponse)
            .FirstOrDefaultAsync(cancellationToken);

        if (order == null)
            throw new NotFoundException("Pedido no encontrado");

        return order;
    }

    public async Task<int> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        Customer customer;
        if (request.CustomerId.HasValue)
        {
            var existingCustomer = await context.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId.Value, cancellationToken);
            if (existingCustomer == null)
                throw new NotFoundException("El cliente especificado no existe.");
            customer = existingCustomer;
        }
        else
        {
            customer = new Customer { Name = request.CustomerName!, Phone = request.CustomerPhone };
        }

        Address? address = null;
        if (request.ShippingMethod == DeliveryShippingMethod)
        {
            if (request.AddressId.HasValue)
            {
                address = await context.Addresses.FirstOrDefaultAsync(
                    a => a.Id == request.AddressId.Value && a.CustomerId == customer.Id, cancellationToken);
                if (address == null)
                    throw new NotFoundException("La dirección especificada no pertenece al cliente o no existe.");
            }
            else if (request.NewAddress != null)
            {
                address = new Address
                {
                    Customer = customer,
                    Street = request.NewAddress.Street,
                    Number = request.NewAddress.Number,
                    Apartment = request.NewAddress.Apartment,
                    Notes = request.NewAddress.Notes
                };
            }
        }

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var productsInDb = await context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var order = new Order
        {
            Customer = customer,
            Address = address,
            ShippingMethod = request.ShippingMethod,
            PaymentMethod = request.PaymentMethod,
            DeliveryCost = request.ShippingMethod == DeliveryShippingMethod ? request.DeliveryCost : 0,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            ScheduledFor = request.ScheduledFor,
            StatusId = OrderStatuses.Pending,
            Items = new List<OrderItem>()
        };

        decimal totalPrice = order.DeliveryCost;

        foreach (var item in request.Items)
        {
            if (!productsInDb.TryGetValue(item.ProductId, out var product))
                throw new BadRequestException($"El producto con Id {item.ProductId} no existe.");

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            };

            totalPrice += orderItem.Quantity * orderItem.UnitPrice;
            order.Items.Add(orderItem);
        }

        order.TotalPrice = totalPrice;

        context.Orders.Add(order);
        await context.SaveChangesAsync(cancellationToken);

        return order.Id;
    }

    public async Task UpdateAsync(int id, UpdateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order == null)
            throw new NotFoundException("Pedido no encontrado");

        if (order.StatusId == OrderStatuses.Delivered || order.StatusId == OrderStatuses.Cancelled)
            throw new ConflictException("Un pedido entregado o cancelado no se puede editar");

        int? finalAddressId = null;
        Address? newAddress = null;

        if (request.ShippingMethod == DeliveryShippingMethod)
        {
            if (request.AddressId.HasValue)
            {
                var existingAddress = await context.Addresses.FirstOrDefaultAsync(
                    a => a.Id == request.AddressId.Value && a.CustomerId == order.CustomerId, cancellationToken);
                if (existingAddress == null)
                    throw new NotFoundException("La dirección especificada no pertenece al cliente o no existe.");
                finalAddressId = existingAddress.Id;
            }
            else if (request.NewAddress != null)
            {
                newAddress = new Address
                {
                    CustomerId = order.CustomerId,
                    Street = request.NewAddress.Street,
                    Number = request.NewAddress.Number,
                    Apartment = request.NewAddress.Apartment,
                    Notes = request.NewAddress.Notes
                };
            }
        }

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var productsInDb = await context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var item in request.Items)
        {
            if (!productsInDb.ContainsKey(item.ProductId))
                throw new BadRequestException($"El producto con Id {item.ProductId} no existe.");
        }

        if (newAddress != null)
        {
            context.Addresses.Add(newAddress);
            order.Address = newAddress;
        }
        else
        {
            order.Address = null;
            order.AddressId = finalAddressId;
        }

        context.OrderItems.RemoveRange(order.Items);
        order.Items.Clear();

        order.ShippingMethod = request.ShippingMethod;
        order.PaymentMethod = request.PaymentMethod;
        order.DeliveryCost = request.ShippingMethod == DeliveryShippingMethod ? request.DeliveryCost : 0;
        order.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        order.ScheduledFor = request.ScheduledFor;

        decimal totalPrice = order.DeliveryCost;

        foreach (var item in request.Items)
        {
            var product = productsInDb[item.ProductId];
            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            };
            totalPrice += orderItem.Quantity * orderItem.UnitPrice;
            order.Items.Add(orderItem);
        }

        order.TotalPrice = totalPrice;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(int id, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default)
    {
        var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order == null)
            throw new NotFoundException("Pedido no encontrado");

        // Un pedido cancelado es final: reabrirlo alteraría la caja sin querer.
        if (order.StatusId == OrderStatuses.Cancelled)
            throw new ConflictException("El pedido está cancelado y no se puede modificar");

        var statusExists = await context.OrderStatuses.AnyAsync(s => s.Id == request.StatusId, cancellationToken);
        if (!statusExists)
            throw new BadRequestException("El estado especificado no existe");

        order.StatusId = request.StatusId;
        await context.SaveChangesAsync(cancellationToken);
    }
}
