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

    private readonly Expression<Func<Order, OrderResponse>> ToResponse = o => new OrderResponse(
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
            i.ProductNameSnapshot ?? (context.Products.Where(p => p.Id == i.ProductId)
                .Select(p => p.Name).FirstOrDefault() ?? "Producto Borrado"),
            i.Quantity,
            i.UnitPrice,
            i.Quantity * i.UnitPrice,
            i.Components.Select(c => new ProductComponentResponse(c.ProductId, c.ProductName, c.Quantity)).ToList(),
            i.FreeDelivery)).ToList());

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
            query = query.Where(o => o.Items.Any(i => i.ProductId == filter.ProductId.Value || i.Components.Any(c => c.ProductId == filter.ProductId.Value)));
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

        var newItems = await BuildItemsAsync(request.Items, cancellationToken);

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

        order.Items = newItems;
        ApplyTotals(order);

        context.Orders.Add(order);
        await context.SaveChangesAsync(cancellationToken);

        return order.Id;
    }

    public async Task UpdateAsync(int id, UpdateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await context.Orders
            .Include(o => o.Items).ThenInclude(i => i.Components)
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

        var newItems = await BuildItemsAsync(request.Items, cancellationToken, order.Items);

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

        order.Items = newItems;
        ApplyTotals(order);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<OrderItem>> BuildItemsAsync(List<CreateOrderItemRequest> requests,
        CancellationToken cancellationToken, ICollection<OrderItem>? previousItems = null)
    {
        if (requests.Count == 0 || requests.Any(i => i.Quantity <= 0))
            throw new BadRequestException("El pedido debe incluir productos con cantidades mayores a cero.");
        var ids = requests.Select(i => i.ProductId).Distinct().ToList();
        // Incluye borrados para validar explícitamente sin ocultar componentes de una promo.
        var products = await context.Products.IgnoreQueryFilters()
            .Include(p => p.Components).ThenInclude(c => c.Product)
            .Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
        var result = new List<OrderItem>();
        foreach (var request in requests)
        {
            var previous = previousItems?.FirstOrDefault(i => i.ProductId == request.ProductId && i.Components.Count > 0);
            if (previous != null)
            {
                result.Add(new OrderItem {
                    ProductId = previous.ProductId, ProductNameSnapshot = previous.ProductNameSnapshot,
                    Quantity = request.Quantity, UnitPrice = previous.UnitPrice, FreeDelivery = previous.FreeDelivery,
                    Components = previous.Components.Select(c => new OrderItemComponent {
                        ProductId = c.ProductId, ProductName = c.ProductName, Quantity = c.Quantity
                    }).ToList()
                });
                continue;
            }
            if (!products.TryGetValue(request.ProductId, out var product) || product.IsDeleted)
                throw new BadRequestException($"El producto con Id {request.ProductId} no existe.");
            if (product.Components.Count > 0 && (!product.IsAvailable || product.Components.Any(c => c.Product.IsDeleted || !c.Product.IsAvailable)))
                throw new BadRequestException($"La promo {product.Name} tiene productos no disponibles.");
            result.Add(new OrderItem {
                ProductId = product.Id, ProductNameSnapshot = product.Name,
                Quantity = request.Quantity, UnitPrice = product.Price, FreeDelivery = product.FreeDelivery,
                Components = product.Components.Select(c => new OrderItemComponent {
                    ProductId = c.ProductId, ProductName = c.Product.Name, Quantity = c.Quantity
                }).ToList()
            });
        }
        return result;
    }

    private static void ApplyTotals(Order order)
    {
        if (order.Items.Any(i => i.FreeDelivery)) order.DeliveryCost = 0;
        order.TotalPrice = order.DeliveryCost + order.Items.Sum(i => i.Quantity * i.UnitPrice);
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
