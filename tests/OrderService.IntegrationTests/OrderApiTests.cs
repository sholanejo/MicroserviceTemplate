using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Commands;
using OrderService.Application.Queries;
using OrderService.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace OrderService.IntegrationTests;

public sealed class OrderApiTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("orderdb_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private WebApplicationFactory<Program> _factory = default!;
    private HttpClient _client = default!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    // Replace the real DbContext with Testcontainer PostgreSQL
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<OrderDbContext>));
                    if (descriptor is not null) services.Remove(descriptor);

                    services.AddDbContext<OrderDbContext>(options =>
                        options.UseNpgsql(_postgres.GetConnectionString()));

                    // Remove background services that need RabbitMQ
                    var hostedServices = services
                        .Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService))
                        .ToList();
                    foreach (var svc in hostedServices) services.Remove(svc);

                    // Remove health checks that need external infra
                    var healthChecks = services
                        .Where(d => d.ServiceType.FullName?.Contains("HealthCheck") == true)
                        .ToList();
                    foreach (var hc in healthChecks) services.Remove(hc);
                    services.AddHealthChecks();

                    // Ensure DB is created
                    using var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
                    db.Database.EnsureCreated();
                });
            });

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task CreateOrder_ReturnsCreated_WithValidPayload()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Test St",
            City: "Lagos",
            State: "Lagos",
            PostalCode: "100001",
            Country: "Nigeria",
            Items:
            [
                new CreateOrderCommand.ItemDto("SKU-001", "Test Widget", 2, 25.00m, "USD")
            ]);

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", command);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var orderId = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, orderId);

        // Verify we can fetch the created order
        var getResponse = await _client.GetAsync($"/api/orders/{orderId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var order = await getResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(order);
        Assert.Equal("Confirmed", order!.Status);
        Assert.Equal(50.00m, order.TotalAmount);
        Assert.Single(order.Items);
    }

    [Fact]
    public async Task CreateOrder_ReturnsBadRequest_WithNoItems()
    {
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Test St",
            City: "Lagos",
            State: "Lagos",
            PostalCode: "100001",
            Country: "Nigeria",
            Items: []);

        var response = await _client.PostAsJsonAsync("/api/orders", command);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_ReturnsNotFound_ForNonExistentId()
    {
        var response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelOrder_ReturnsNoContent()
    {
        // Create an order first
        var createCommand = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            Street: "123 Test St",
            City: "Lagos",
            State: "Lagos",
            PostalCode: "100001",
            Country: "Nigeria",
            Items:
            [
                new CreateOrderCommand.ItemDto("SKU-002", "Gadget", 1, 15.00m, "USD")
            ]);

        var createResponse = await _client.PostAsJsonAsync("/api/orders", createCommand);
        var orderId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        // Cancel it
        var cancelResponse = await _client.PutAsJsonAsync(
            $"/api/orders/{orderId}/cancel",
            new { Reason = "Changed my mind" });

        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        // Verify status changed
        var getResponse = await _client.GetAsync($"/api/orders/{orderId}");
        var order = await getResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal("Cancelled", order!.Status);
    }

    [Fact]
    public async Task ListOrders_ReturnsPaginatedResults()
    {
        // Create a couple of orders
        for (var i = 0; i < 3; i++)
        {
            var cmd = new CreateOrderCommand(
                Guid.NewGuid(), "St", "City", "State", "00000", "Country",
                [new CreateOrderCommand.ItemDto($"SKU-{i:D3}", $"Item {i}", 1, 10m, "USD")]);
            await _client.PostAsJsonAsync("/api/orders", cmd);
        }

        var response = await _client.GetAsync("/api/orders?page=1&pageSize=2");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
