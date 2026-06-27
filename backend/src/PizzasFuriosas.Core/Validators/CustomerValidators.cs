using FluentValidation;
using PizzasFuriosas.Core.DTOs;

namespace PizzasFuriosas.Core.Validators;

public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede tener más de 100 caracteres.");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("El teléfono no puede tener más de 20 caracteres.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Las notas no pueden superar los 500 caracteres.");
    }
}

public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede tener más de 100 caracteres.");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("El teléfono no puede tener más de 20 caracteres.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Las notas no pueden superar los 500 caracteres.");
    }
}

public class CreateAddressRequestValidator : AbstractValidator<CreateAddressRequest>
{
    public CreateAddressRequestValidator()
    {
        RuleFor(x => x.Street)
            .NotEmpty().WithMessage("La calle es obligatoria.")
            .MaximumLength(150).WithMessage("La calle no puede tener más de 150 caracteres.");

        RuleFor(x => x.Number)
            .NotEmpty().WithMessage("El número es obligatorio.")
            .MaximumLength(10).WithMessage("El número no puede tener más de 10 caracteres.");

        RuleFor(x => x.Apartment)
            .MaximumLength(20).WithMessage("El departamento no puede tener más de 20 caracteres.");

        RuleFor(x => x.Notes)
            .MaximumLength(200).WithMessage("Las notas no pueden superar los 200 caracteres.");
    }
}
