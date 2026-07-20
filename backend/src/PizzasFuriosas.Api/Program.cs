using PizzasFuriosas.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureHosting();

builder.Services
    .AddApiValidation()
    .AddCorsPolicy(builder.Configuration, builder.Environment)
    .AddPersistence(builder.Configuration)
    .AddAppServices()
    .AddJwtAuth()
    .AddSwaggerDocs();

var app = builder.Build();

app.UseAppExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(ServiceCollectionExtensions.CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check del hosting (Render espera un 200 en "/").
app.MapGet("/", () => Results.Ok(new { status = "ok", service = "PizzasFuriosas.Api" }));

app.MigrateAndSeedDatabase();

app.Run();
