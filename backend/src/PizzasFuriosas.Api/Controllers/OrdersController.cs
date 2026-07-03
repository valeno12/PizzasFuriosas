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
public class OrdersController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<OrderResponse>>>> GetAll([FromQuery] OrderFilterDto filter, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 1000);

        var query = context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Status)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .AsQueryable();

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

        if (filter.OnlyActive.HasValue && filter.OnlyActive.Value)
            query = query.Where(o => o.StatusId != 5 && o.StatusId != 6);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchLower = filter.Search.ToLower();
            query = query.Where(o => 
                o.Id.ToString().Contains(searchLower) || 
                (o.Customer != null && o.Customer.Name.ToLower().Contains(searchLower))
            );
        }

        var totalCount = await query.CountAsync();

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderResponse(
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
                    i.Quantity * i.UnitPrice
                )).ToList()
            )).ToListAsync();

        var paginatedResult = new PaginatedResult<OrderResponse>(orders, totalCount, page, pageSize);
        return Ok(new ApiResponse<PaginatedResult<OrderResponse>>(paginatedResult));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<OrderResponse>>> GetById(int id)
    {
        var order = await context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Status)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Where(o => o.Id == id)
            .Select(o => new OrderResponse(
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
                    i.Quantity * i.UnitPrice
                )).ToList()
            )).FirstOrDefaultAsync();

        if (order == null)
            return NotFound(ApiResponse.Error("Pedido no encontrado"));

        return Ok(new ApiResponse<OrderResponse>(order));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<int>>> Create(CreateOrderRequest request)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        // 1. Manejo del Cliente (Buscar o Crear)
        Customer customer;
        if (request.CustomerId.HasValue)
        {
            var existingCustomer = await context.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId.Value);
            if (existingCustomer == null) return NotFound(ApiResponse.Error("El cliente especificado no existe."));
            customer = existingCustomer;
        }
        else
        {
            customer = new Customer
            {
                Name = request.CustomerName!,
                Phone = request.CustomerPhone
            };
            context.Customers.Add(customer);
            await context.SaveChangesAsync(); 
        }

        // 2. Manejo de la Dirección (Buscar o Crear)
        int? finalAddressId = null;
        if (request.ShippingMethod == "Delivery")
        {
            if (request.AddressId.HasValue)
            {
                var existingAddress = await context.Addresses.FirstOrDefaultAsync(a => a.Id == request.AddressId.Value && a.CustomerId == customer.Id);
                if (existingAddress == null) return NotFound(ApiResponse.Error("La dirección especificada no pertenece al cliente o no existe."));
                finalAddressId = existingAddress.Id;
            }
            else if (request.NewAddress != null)
            {
                var address = new Address
                {
                    CustomerId = customer.Id,
                    Street = request.NewAddress.Street,
                    Number = request.NewAddress.Number,
                    Apartment = request.NewAddress.Apartment,
                    Notes = request.NewAddress.Notes
                };
                context.Addresses.Add(address);
                await context.SaveChangesAsync();
                finalAddressId = address.Id;
            }
        }

        // 3. Extracción de productos
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var productsInDb = await context.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        var order = new Order
        {
            CustomerId = customer.Id,
            AddressId = finalAddressId,
            ShippingMethod = request.ShippingMethod,
            PaymentMethod = request.PaymentMethod,
            DeliveryCost = request.ShippingMethod == "Delivery" ? request.DeliveryCost : 0,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            ScheduledFor = request.ScheduledFor,
            StatusId = 1, // Pendiente
            Items = new List<OrderItem>()
        };

        decimal totalPrice = order.DeliveryCost;

        foreach (var itemRequest in request.Items)
        {
            if (!productsInDb.TryGetValue(itemRequest.ProductId, out var product))
            {
                return BadRequest(ApiResponse.Error($"El producto con Id {itemRequest.ProductId} no existe."));
            }

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                Quantity = itemRequest.Quantity,
                UnitPrice = product.Price
            };

            totalPrice += orderItem.Quantity * orderItem.UnitPrice;
            order.Items.Add(orderItem);
        }

        order.TotalPrice = totalPrice;

        context.Orders.Add(order);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, new ApiResponse<int>(order.Id, "Pedido cargado exitosamente"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> Update(int id, UpdateOrderRequest request)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        var order = await context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
            return NotFound(ApiResponse.Error("Pedido no encontrado"));

        // Solo se editan pedidos en curso: uno entregado/cancelado ya impactó en la caja.
        if (order.StatusId == 5 || order.StatusId == 6)
            return Conflict(ApiResponse.Error("Un pedido entregado o cancelado no se puede editar"));

        // Dirección (igual que en Create, pero contra el cliente del pedido)
        int? finalAddressId = null;
        if (request.ShippingMethod == "Delivery")
        {
            if (request.AddressId.HasValue)
            {
                var existingAddress = await context.Addresses
                    .FirstOrDefaultAsync(a => a.Id == request.AddressId.Value && a.CustomerId == order.CustomerId);
                if (existingAddress == null)
                    return NotFound(ApiResponse.Error("La dirección especificada no pertenece al cliente o no existe."));
                finalAddressId = existingAddress.Id;
            }
            else if (request.NewAddress != null)
            {
                var address = new Address
                {
                    CustomerId = order.CustomerId,
                    Street = request.NewAddress.Street,
                    Number = request.NewAddress.Number,
                    Apartment = request.NewAddress.Apartment,
                    Notes = request.NewAddress.Notes
                };
                context.Addresses.Add(address);
                await context.SaveChangesAsync();
                finalAddressId = address.Id;
            }
        }

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var productsInDb = await context.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        // Se reemplazan los ítems por completo, repreciando al valor actual del producto.
        context.OrderItems.RemoveRange(order.Items);
        order.Items.Clear();

        order.ShippingMethod = request.ShippingMethod;
        order.PaymentMethod = request.PaymentMethod;
        order.DeliveryCost = request.ShippingMethod == "Delivery" ? request.DeliveryCost : 0;
        order.AddressId = finalAddressId;
        order.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        order.ScheduledFor = request.ScheduledFor;

        decimal totalPrice = order.DeliveryCost;

        foreach (var itemRequest in request.Items)
        {
            if (!productsInDb.TryGetValue(itemRequest.ProductId, out var product))
            {
                return BadRequest(ApiResponse.Error($"El producto con Id {itemRequest.ProductId} no existe."));
            }

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                Quantity = itemRequest.Quantity,
                UnitPrice = product.Price
            };

            totalPrice += orderItem.Quantity * orderItem.UnitPrice;
            order.Items.Add(orderItem);
        }

        order.TotalPrice = totalPrice;

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(ApiResponse.Ok("Pedido actualizado"));
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(int id, UpdateOrderStatusRequest request)
    {
        var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound(ApiResponse.Error("Pedido no encontrado"));

        // Un pedido cancelado es final: reabrirlo alteraría la caja sin querer.
        // Entregado sí se puede revertir (marcar de más es un error frecuente).
        if (order.StatusId == 6)
            return Conflict(ApiResponse.Error("El pedido está cancelado y no se puede modificar"));

        order.StatusId = request.StatusId;
        await context.SaveChangesAsync();

        return Ok(ApiResponse.Ok($"Estado del pedido actualizado al ID {request.StatusId}"));
    }
}
