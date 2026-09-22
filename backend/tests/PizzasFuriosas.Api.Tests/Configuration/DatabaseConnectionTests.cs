using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PizzasFuriosas.Api.Extensions;
using PizzasFuriosas.Infrastructure.Data;
using Xunit;

namespace PizzasFuriosas.Api.Tests.Configuration;

public class DatabaseConnectionTests
{
    [Theory]
    [InlineData("localhost", SslMode.Disable)]
    [InlineData("127.0.0.1", SslMode.Disable)]
    [InlineData("[::1]", SslMode.Disable)]
    [InlineData("db.example.com", SslMode.Require)]
    [InlineData("localhost.example.com", SslMode.Require)]
    public void DatabaseUrl_DisablesSslOnlyForLoopback(string host, SslMode expected)
    {
        var previous = Environment.GetEnvironmentVariable("DATABASE_URL");
        try
        {
            Environment.SetEnvironmentVariable("DATABASE_URL", $"postgres://pizzas:pizzas@{host}:5432/pizzasfuriosas");
            var services = new ServiceCollection();
            services.AddPersistence(new ConfigurationBuilder().Build());
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var connection = new NpgsqlConnectionStringBuilder(context.Database.GetConnectionString());
            Assert.Equal(expected, connection.SslMode);
            Assert.Equal("pizzasfuriosas", connection.Database);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DATABASE_URL", previous);
        }
    }
}
