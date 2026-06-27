using FluentValidation.Resources;
using System.Globalization;

namespace PizzasFuriosas.Core.Validators;

public class SpanishLanguageManager : LanguageManager
{
    public SpanishLanguageManager()
    {
        AddTranslation("es", "NotEmptyValidator", "'{PropertyName}' es obligatorio.");
        AddTranslation("es", "NotNullValidator", "'{PropertyName}' es obligatorio.");
        AddTranslation("es", "MinimumLengthValidator", "'{PropertyName}' debe tener al menos {MinLength} caracteres. Ingresaste {TotalLength}.");
        AddTranslation("es", "MaximumLengthValidator", "'{PropertyName}' no puede superar {MaxLength} caracteres. Ingresaste {TotalLength}.");
        AddTranslation("es", "GreaterThanValidator", "'{PropertyName}' debe ser mayor que {ComparisonValue}.");
        AddTranslation("es", "GreaterThanOrEqualValidator", "'{PropertyName}' debe ser mayor o igual que {ComparisonValue}.");
        AddTranslation("es", "LessThanValidator", "'{PropertyName}' debe ser menor que {ComparisonValue}.");
        AddTranslation("es", "PredicateValidator", "'{PropertyName}' no cumple con la condición especificada.");
        AddTranslation("es", "EmailValidator", "'{PropertyName}' debe ser una dirección de email válida.");
        AddTranslation("es", "RegularExpressionValidator", "'{PropertyName}' tiene un formato incorrecto.");
        AddTranslation("es", "ExactLengthValidator", "'{PropertyName}' debe tener exactamente {MaxLength} caracteres. Ingresaste {TotalLength}.");
        AddTranslation("es", "InclusiveBetweenValidator", "'{PropertyName}' debe estar entre {From} y {To}.");
    }
}
