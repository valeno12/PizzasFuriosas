using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PizzasFuriosas.Core.Common;
using PizzasFuriosas.Core.DTOs;
using PizzasFuriosas.Core.Entities;
using PizzasFuriosas.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PizzasFuriosas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDbContext context) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (await context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower()))
        {
            return Conflict(ApiResponse.Error("Ya existe un usuario con ese email"));
        }

        var user = new User
        {
            Name = request.Name,
            Email = request.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = GenerateToken(user);
        var response = new AuthResponse(user.Id, user.Name, user.Email, user.Role, token);

        return CreatedAtAction(nameof(Register), new ApiResponse<AuthResponse>(response, "Usuario registrado"));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(ApiResponse.Error("Email o contraseña incorrectos"));
        }

        var token = GenerateToken(user);
        var response = new AuthResponse(user.Id, user.Name, user.Email, user.Role, token);

        return Ok(new ApiResponse<AuthResponse>(response, "Login exitoso"));
    }

    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_KEY")!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expirationHours = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRATION_HOURS") ?? "24");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: Environment.GetEnvironmentVariable("JWT_ISSUER"),
            audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
