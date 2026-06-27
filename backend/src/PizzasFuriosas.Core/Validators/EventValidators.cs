using FluentValidation;
using PizzasFuriosas.Core.DTOs;

namespace PizzasFuriosas.Core.Validators;

public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("El cliente es obligatorio.");

        RuleFor(x => x.EventDate)
            .NotEmpty().WithMessage("La fecha del evento es obligatoria.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("La ubicación es obligatoria.")
            .MaximumLength(200).WithMessage("La ubicación no puede superar los 200 caracteres.");

        RuleFor(x => x.PizzaCount)
            .GreaterThanOrEqualTo(0).WithMessage("La cantidad de pizzas no puede ser negativa.");

        RuleFor(x => x.PricePerPizza)
            .GreaterThanOrEqualTo(0).WithMessage("El precio por pizza no puede ser negativo.");

        RuleFor(x => x.Deposit)
            .GreaterThanOrEqualTo(0).WithMessage("La seña no puede ser negativa.");

        RuleForEach(x => x.Surcharges).SetValidator(new CreateEventSurchargeRequestValidator());
        RuleForEach(x => x.Payments).SetValidator(new CreateEventPaymentRequestValidator());
    }
}

public class CreateEventSurchargeRequestValidator : AbstractValidator<CreateEventSurchargeRequest>
{
    public CreateEventSurchargeRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción del viático es obligatoria.")
            .MaximumLength(150);

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto del viático debe ser mayor a cero.");
    }
}

public class CreateEventPaymentRequestValidator : AbstractValidator<CreateEventPaymentRequest>
{
    public CreateEventPaymentRequestValidator()
    {
        RuleFor(x => x.PaymentMethod)
            .Must(p => p == "Efectivo" || p == "Transferencia" || p == "MercadoPago")
            .WithMessage("Método de pago inválido.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto del pago debe ser mayor a cero.");
    }
}

public class CompleteEventRequestValidator : AbstractValidator<CompleteEventRequest>
{
    public CompleteEventRequestValidator()
    {
        RuleFor(x => x.ExtraPizzas)
            .GreaterThanOrEqualTo(0).WithMessage("Las pizzas extra no pueden ser negativas.");

        RuleForEach(x => x.ExtraSurcharges).SetValidator(new CreateEventSurchargeRequestValidator());
    }
}
