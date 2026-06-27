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
    public async Task<IActionResult> GetAll([FromQuery] OrderFilterDto filter, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = context.Orders
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

        var totalCount = await query.CountAsync();

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderResponse(
                o.Id,
                o.CustomerId,
                o.Customer != null ? o.Customer.Name : "Cliente Borrado",
                o.Customer.Phone,
                o.ShippingMethod,
                o.DeliveryCost,
                o.PaymentMethod,
                o.StatusId,
                o.Status.Name,
                o.TotalPrice,
                o.CreatedAt,
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
    public async Task<IActionResult> GetById(int id)
    {
        var order = await context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Status)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Where(o => o.Id == id)
            .Select(o => new OrderResponse(
                o.Id,
                o.CustomerId,
                o.Customer != null ? o.Customer.Name : "Cliente Borrado",
                o.Customer.Phone,
                o.ShippingMethod,
                o.DeliveryCost,
                o.PaymentMethod,
                o.StatusId,
                o.Status.Name,
                o.TotalPrice,
                o.CreatedAt,
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
    public async Task<IActionResult> Create(CreateOrderRequest request)
    {
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

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, new ApiResponse<int>(order.Id, "Pedido cargado exitosamente"));
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusRequest request)
    {
        var order = await context.Orders.FirstOrDefaultAsync(o => o.Id == id);
        
        if (order == null)
            return NotFound(ApiResponse.Error("Pedido no encontrado"));

        order.StatusId = request.StatusId;
        await context.SaveChangesAsync();

        return Ok(ApiResponse.Ok($"Estado del pedido actualizado al ID {request.StatusId}"));
    }
}
