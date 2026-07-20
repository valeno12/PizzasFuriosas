using Microsoft.EntityFrameworkCore;
using PizzasFuriosas.Api.Exceptions;
using PizzasFuriosas.Core.DTOs;
using PizzasFuriosas.Core.Entities;
using PizzasFuriosas.Infrastructure.Data;

namespace PizzasFuriosas.Api.Services;

public class AuthService(AppDbContext context, TokenService tokenService)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.ToLower();

        if (await context.Users.AnyAsync(u => u.Email.ToLower() == email, cancellationToken))
            throw new ConflictException("Ya existe un usuario con ese email");

        var user = new User
        {
            Name = request.Name,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        var token = tokenService.GenerateToken(user);
        return new AuthResponse(user.Id, user.Name, user.Email, user.Role, token);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.ToLower();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Email o contraseña incorrectos");

        var token = tokenService.GenerateToken(user);
        return new AuthResponse(user.Id, user.Name, user.Email, user.Role, token);
    }
}
