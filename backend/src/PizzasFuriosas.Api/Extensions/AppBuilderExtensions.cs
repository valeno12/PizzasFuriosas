using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PizzasFuriosas.Api.Exceptions;
using PizzasFuriosas.Core.Common;
using PizzasFuriosas.Core.Entities;
using PizzasFuriosas.Infrastructure.Data;

namespace PizzasFuriosas.Api.Extensions;

public static class AppBuilderExtensions
{
    public static void ConfigureHosting(this WebApplicationBuilder builder)
    {
        // .env solo en local; en producción las variables las inyecta el hosting.
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env");
        if (File.Exists(envPath))
            DotNetEnv.Env.Load(envPath);

        // Render/Fly asignan el puerto por PORT; en local se respeta launchSettings.
        var port = Environment.GetEnvironmentVariable("PORT");
        if (!string.IsNullOrWhiteSpace(port))
            builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
    }

    public static WebApplication UseAppExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

                // Errores de negocio esperados: cada excepción trae su código HTTP.
                if (exception is AppException appException)
                {
                    context.Response.StatusCode = appException.StatusCode;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(ApiResponse.Error(appException.Message));
                    return;
                }

                // El resto es inesperado: se loguea y se devuelve un 500 genérico.
                if (exception != null)
                    logger.LogError(exception, "Unhandled exception while processing request {Method} {Path}", context.Request.Method, context.Request.Path);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(ApiResponse.Error("Ocurrió un error inesperado."));
            });
        });

        return app;
    }

    public static WebApplication MigrateAndSeedDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Aplica migraciones al arrancar (clave en hosting gratuito, sin consola remota).
        context.Database.Migrate();

        if (!context.Users.Any())
        {
            var adminEmail = Environment.GetEnvironmentVariable("ADMIN_DEFAULT_EMAIL")
                ?? throw new InvalidOperationException("Falta ADMIN_DEFAULT_EMAIL: definila en el archivo .env (ver .env.example)");
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_DEFAULT_PASSWORD")
                ?? throw new InvalidOperationException("Falta ADMIN_DEFAULT_PASSWORD: definila en el archivo .env (ver .env.example)");

            context.Users.Add(new User
            {
                Name = "Admin Default",
                Email = adminEmail.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Role = AppRoles.Admin,
                CreatedAt = DateTime.UtcNow
            });

            context.SaveChanges();
        }

        return app;
    }
}
