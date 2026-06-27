using FluentValidation;
using PizzasFuriosas.Core.DTOs;

namespace PizzasFuriosas.Core.Validators;

public class CreatePurchaseRequestValidator : AbstractValidator<CreatePurchaseRequest>
{
    public CreatePurchaseRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("La fecha de la compra es obligatoria.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .MaximumLength(250).WithMessage("La descripción no puede superar los 250 caracteres.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto de la compra debe ser mayor a cero.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("La categoría es obligatoria.")
            .MaximumLength(100).WithMessage("La categoría no puede superar los 100 caracteres.");
    }
}

public class UpdatePurchaseRequestValidator : AbstractValidator<UpdatePurchaseRequest>
{
    public UpdatePurchaseRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("La fecha de la compra es obligatoria.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .MaximumLength(250).WithMessage("La descripción no puede superar los 250 caracteres.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto de la compra debe ser mayor a cero.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("La categoría es obligatoria.")
            .MaximumLength(100).WithMessage("La categoría no puede superar los 100 caracteres.");
    }
}
