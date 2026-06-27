using FluentValidation;
using PizzasFuriosas.Core.DTOs;

namespace PizzasFuriosas.Core.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.CustomerId.HasValue || (!string.IsNullOrWhiteSpace(x.CustomerName) && !string.IsNullOrWhiteSpace(x.CustomerPhone)))
            .WithMessage("Debe proporcionar un CustomerId existente o crear uno nuevo pasando CustomerName y CustomerPhone.");

        RuleFor(x => x.ShippingMethod)
            .Must(s => s == "Take Away" || s == "Delivery")
            .WithMessage("ShippingMethod debe ser 'Take Away' o 'Delivery'.");

        RuleFor(x => x.PaymentMethod)
            .Must(p => p == "Efectivo" || p == "Transferencia")
            .WithMessage("PaymentMethod debe ser 'Efectivo' o 'Transferencia'.");

        RuleFor(x => x)
            .Must(x => x.ShippingMethod != "Delivery" || (x.AddressId.HasValue || x.NewAddress != null))
            .WithMessage("Si el método de envío es 'Delivery', debe proporcionar un AddressId o un NewAddress.");

        RuleFor(x => x.DeliveryCost)
            .GreaterThanOrEqualTo(0)
            .WithMessage("El costo de envío no puede ser negativo.");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("El pedido debe contener al menos un producto.");

        RuleForEach(x => x.Items).SetValidator(new CreateOrderItemRequestValidator());
    }
}

public class CreateOrderItemRequestValidator : AbstractValidator<CreateOrderItemRequest>
{
    public CreateOrderItemRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0);

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("La cantidad debe ser mayor a 0.");
    }
}

public class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(x => x.StatusId)
            .InclusiveBetween(1, 6)
            .WithMessage("El estado debe ser un ID válido (1 al 6).");
    }
}
