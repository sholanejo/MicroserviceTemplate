using System.Text.Json;
using BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Common.Outbox;

/// <summary>
/// Generic outbox processor. Register with the concrete DbContext type
/// that has an OutboxMessages DbSet.
/// Usage: builder.Services.AddOutboxProcessor&lt;OrderDbContext&gt;();
/// </summary>
public sealed class OutboxProcessor<TContext> : BackgroundService where TContext : DbContext
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor<TContext>> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessor<TContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor started for {Context}", typeof(TContext).Name);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingMessages(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        // Find unprocessed outbox messages (batch of 20)
        var messages = await context.Set<OutboxMessageBase>()
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        if (messages.Count == 0) return;

        _logger.LogInformation("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                // Derive a routing key from the event type name
                var typeName = message.Type.Split(',')[0]; // Get just the type name
                var shortName = typeName.Split('.').Last(); // e.g. "OrderConfirmedEvent"
                var routingKey = ConvertToRoutingKey(shortName); // e.g. "order.confirmed"
                var exchange = DeriveExchange(shortName);

                await publisher.PublishAsync(
                    JsonSerializer.Deserialize<object>(message.Payload)!,
                    exchange,
                    routingKey,
                    ct);

                message.ProcessedAt = DateTime.UtcNow;
                _logger.LogInformation("Outbox message {Id} published to {Exchange}/{RoutingKey}",
                    message.Id, exchange, routingKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish outbox message {Id} — will retry", message.Id);
            }
        }

        await context.SaveChangesAsync(ct);
    }

    private static string ConvertToRoutingKey(string eventName)
    {
        // OrderConfirmedEvent → order.confirmed
        var name = eventName.Replace("Event", "");
        var parts = new List<string>();
        var current = "";

        foreach (var c in name)
        {
            if (char.IsUpper(c) && current.Length > 0)
            {
                parts.Add(current.ToLowerInvariant());
                current = "";
            }
            current += c;
        }

        if (current.Length > 0) parts.Add(current.ToLowerInvariant());

        return parts.Count >= 2
            ? $"{parts[0]}.{string.Join("-", parts.Skip(1))}"
            : parts[0];
    }

    private static string DeriveExchange(string eventName)
    {
        if (eventName.StartsWith("Order", StringComparison.OrdinalIgnoreCase)) return "orders";
        if (eventName.StartsWith("Inventory", StringComparison.OrdinalIgnoreCase)) return "inventory";
        return "events";
    }
}

/// <summary>
/// Base outbox message class that the processor queries.
/// Each service's OutboxMessage should match this schema.
/// </summary>
public class OutboxMessageBase
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public static class OutboxExtensions
{
    public static IServiceCollection AddOutboxProcessor<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddHostedService<OutboxProcessor<TContext>>();
        return services;
    }
}
