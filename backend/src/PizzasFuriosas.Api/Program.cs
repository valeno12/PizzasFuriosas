using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzasFuriosas.Core.Common;
using PizzasFuriosas.Core.Validators;
using PizzasFuriosas.Infrastructure.Data;
using System.Globalization;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Cargar variables de entorno desde el archivo .env (solo en local; en producción
// las variables las inyecta el hosting, así que el archivo puede no existir).
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
}

// Hostings como Render/Fly asignan el puerto por la variable PORT. En local no está
// definida, así que se respeta la config de launchSettings (puerto de desarrollo).
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Configurar FluentValidation con mensajes globales en español
ValidatorOptions.Global.LanguageManager = new SpanishLanguageManager();
ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("es");

// Diccionario global de traducciones de propiedades para FluentValidation
var propertyTranslations = new Dictionary<string, string>
{
    { "Name", "Nombre" },
    { "Price", "Precio" },
    { "CategoryId", "Categoría" }
};

ValidatorOptions.Global.DisplayNameResolver = (type, member, expression) =>
{
    if (member != null && propertyTranslations.TryGetValue(member.Name, out var translation))
    {
        return translation;
    }
    return member?.Name;
};

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin() // En producción deberías restringir esto a tu dominio (ej: app.pizzasfuriosas.com)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var validationErrors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => JsonNamingPolicy.CamelCase.ConvertName(kvp.Key),
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            var response = ApiResponse.ValidationError("Se encontraron uno o más errores de validación.", validationErrors);
            return new BadRequestObjectResult(response);
        };
    });

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateCategoryRequestValidator>();

// Cadena de conexión: DATABASE_URL (formato URL que dan los hostings gratuitos tipo
// Neon/Supabase/Render) tiene prioridad; si no está, se usa la de appsettings
// (Postgres local levantado con docker-compose).
var connectionString = BuildConnectionString(builder.Configuration);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

static string BuildConnectionString(IConfiguration configuration)
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (string.IsNullOrWhiteSpace(databaseUrl))
    {
        return configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Falta la cadena de conexión: definí DATABASE_URL o ConnectionStrings:DefaultConnection");
    }

    // Convierte postgres://usuario:clave@host:puerto/db al formato de Npgsql.
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    var database = uri.AbsolutePath.TrimStart('/');
    var port = uri.Port > 0 ? uri.Port : 5432;
    var sslMode = uri.Host is "" or "127.0.0.1" ? "Disable" : "Require";

    return $"Host={uri.Host};Port={port};Database={database};Username={Uri.UnescapeDataString(userInfo[0])};" +
           $"Password={Uri.UnescapeDataString(userInfo.Length > 1 ? userInfo[1] : "")};SSL Mode={sslMode}";
}

builder.Services.AddScoped<PizzasFuriosas.Core.Interfaces.IPhotoService, PizzasFuriosas.Infrastructure.Services.CloudinaryPhotoService>();

// Configurar JWT usando variables de entorno (falla al arrancar con mensaje claro si falta la clave)
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
    ?? throw new InvalidOperationException("Falta JWT_KEY: definila en el archivo .env (ver .env.example)");

builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
            ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        if (exception != null)
        {
            logger.LogError(exception, "Unhandled exception while processing request {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(ApiResponse.Error("Ocurrió un error inesperado."));
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Endpoint raíz simple para el health check del hosting (Render espera un 200 en "/").
app.MapGet("/", () => Results.Ok(new { status = "ok", service = "PizzasFuriosas.Api" }));

// Inicialización de la base de datos
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Aplica las migraciones pendientes al arrancar (clave en hosting gratuito,
    // donde no hay consola para correr dotnet ef contra la base remota).
    context.Database.Migrate();

    // Si no hay usuarios en la base de datos, creamos el admin por defecto
    if (!context.Users.Any())
    {
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_DEFAULT_EMAIL")
            ?? throw new InvalidOperationException("Falta ADMIN_DEFAULT_EMAIL: definila en el archivo .env (ver .env.example)");
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_DEFAULT_PASSWORD")
            ?? throw new InvalidOperationException("Falta ADMIN_DEFAULT_PASSWORD: definila en el archivo .env (ver .env.example)");

        var adminUser = new PizzasFuriosas.Core.Entities.User
        {
            Name = "Admin Default",
            Email = adminEmail.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(adminUser);
        context.SaveChanges();
    }
}

app.Run();
