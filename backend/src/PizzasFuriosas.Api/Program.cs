using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PizzasFuriosas.Core.Common;
using PizzasFuriosas.Core.Validators;
using PizzasFuriosas.Infrastructure.Data;
using System.Globalization;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Cargar variables de entorno desde el archivo .env
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env");
DotNetEnv.Env.Load(envPath);

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

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<PizzasFuriosas.Core.Interfaces.IPhotoService, PizzasFuriosas.Infrastructure.Services.CloudinaryPhotoService>();

// Configurar JWT usando variables de entorno
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
                System.Text.Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_KEY")!))
        };
    });

builder.Services.AddAuthorization();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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

// Inicialización de la base de datos
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // Si no hay usuarios en la base de datos, creamos el admin por defecto
    if (!context.Users.Any())
    {
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_DEFAULT_EMAIL");
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_DEFAULT_PASSWORD");

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
