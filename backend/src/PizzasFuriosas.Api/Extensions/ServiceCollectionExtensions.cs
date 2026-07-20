using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PizzasFuriosas.Api.Configuration;
using PizzasFuriosas.Api.Services;
using PizzasFuriosas.Core.Common;
using PizzasFuriosas.Core.Interfaces;
using PizzasFuriosas.Core.Validators;
using PizzasFuriosas.Infrastructure.Data;
using PizzasFuriosas.Infrastructure.Services;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace PizzasFuriosas.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public const string CorsPolicy = "AllowFrontend";

    public static IServiceCollection AddApiValidation(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager = new SpanishLanguageManager();
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("es");

        var propertyTranslations = new Dictionary<string, string>
        {
            { "Name", "Nombre" },
            { "Price", "Precio" },
            { "CategoryId", "Categoría" }
        };

        ValidatorOptions.Global.DisplayNameResolver = (type, member, expression) =>
            member != null && propertyTranslations.TryGetValue(member.Name, out var translation)
                ? translation
                : member?.Name;

        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var validationErrors = context.ModelState
                        .Where(e => e.Value?.Errors.Count > 0)
                        .ToDictionary(
                            kvp => JsonNamingPolicy.CamelCase.ConvertName(kvp.Key),
                            kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                    return new BadRequestObjectResult(
                        ApiResponse.ValidationError("Se encontraron uno o más errores de validación.", validationErrors));
                };
            });

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<CreateCategoryRequestValidator>();

        return services;
    }

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var allowedOrigins = GetAllowedCorsOrigins(configuration, environment);

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicy, policy =>
                policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader());
        });

        return services;
    }

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(BuildConnectionString(configuration)));
        return services;
    }

    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<CategoryService>();
        services.AddScoped<ProductService>();
        services.AddScoped<CustomerService>();
        services.AddScoped<PurchaseService>();
        services.AddScoped<EventService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<AuthService>();
        services.AddScoped<TokenService>();
        services.AddScoped<OrderService>();
        services.AddScoped<IPhotoService, CloudinaryPhotoService>();
        return services;
    }

    public static IServiceCollection AddJwtAuth(this IServiceCollection services)
    {
        var jwtSettings = new JwtSettings
        {
            Key = Environment.GetEnvironmentVariable("JWT_KEY")
                ?? throw new InvalidOperationException("Falta JWT_KEY: definila en el archivo .env (ver .env.example)"),
            Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
            Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
            ExpirationHours = int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRATION_HOURS"), out var hours) ? hours : 24
        };

        services.AddSingleton(jwtSettings);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
                };
            });

        services.AddAuthorization(options =>
            options.AddPolicy(AppPolicies.AdminOnly, policy => policy.RequireRole(AppRoles.Admin)));

        return services;
    }

    public static IServiceCollection AddSwaggerDocs(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        return services;
    }

    private static string[] GetAllowedCorsOrigins(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .GetChildren()
            .Select(origin => origin.Value)
            .Where(origin => !string.IsNullOrWhiteSpace(origin));

        var environmentOrigins = (Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS") ?? string.Empty)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var origins = configuredOrigins
            .Concat(environmentOrigins)
            .Select(origin => origin!.Trim().TrimEnd('/'))
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (origins.Length > 0)
            return origins;

        if (environment.IsDevelopment())
        {
            return
            [
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:4321",
                "http://127.0.0.1:5173",
                "http://127.0.0.1:5174",
                "http://127.0.0.1:4321"
            ];
        }

        throw new InvalidOperationException(
            "Falta configurar CORS_ALLOWED_ORIGINS o Cors:AllowedOrigins con los dominios permitidos para el frontend.");
    }

    private static string BuildConnectionString(IConfiguration configuration)
    {
        // DATABASE_URL (formato URL de hostings tipo Neon/Supabase/Render) tiene prioridad.
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (string.IsNullOrWhiteSpace(databaseUrl))
        {
            return configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Falta la cadena de conexión: definí DATABASE_URL o ConnectionStrings:DefaultConnection");
        }

        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var database = uri.AbsolutePath.TrimStart('/');
        var port = uri.Port > 0 ? uri.Port : 5432;
        var sslMode = uri.Host is "" or "127.0.0.1" ? "Disable" : "Require";

        return $"Host={uri.Host};Port={port};Database={database};Username={Uri.UnescapeDataString(userInfo[0])};" +
               $"Password={Uri.UnescapeDataString(userInfo.Length > 1 ? userInfo[1] : "")};SSL Mode={sslMode}";
    }
}
