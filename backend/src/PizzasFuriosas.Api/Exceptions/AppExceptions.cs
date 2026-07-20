namespace PizzasFuriosas.Api.Exceptions;

public abstract class AppException(string message) : Exception(message)
{
    public abstract int StatusCode { get; }
}

/// <summary>El recurso pedido no existe. Se traduce a HTTP 404.</summary>
public sealed class NotFoundException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status404NotFound;
}

/// <summary>La operación choca con una regla del negocio (ej: nombre duplicado). Se traduce a HTTP 409.</summary>
public sealed class ConflictException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status409Conflict;
}

/// <summary>Los datos enviados no son válidos para la operación. Se traduce a HTTP 400.</summary>
public sealed class BadRequestException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
}

/// <summary>Credenciales inválidas o falta de autenticación. Se traduce a HTTP 401.</summary>
public sealed class UnauthorizedException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status401Unauthorized;
}
