using System.Text;
using System.Text.Json;
using BuildingBlocks.Messaging;
using InventoryService.Application.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace InventoryService.Infrastructure.Messaging;

public sealed class OrderEventsConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderEventsConsumer> _logger;
    private readonly RabbitMqSettings _settings;
    private IConnection? _connection;
    private IChannel? _channel;

    public OrderEventsConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqSettings> settings,
        ILogger<OrderEventsConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            UserName = _settings.Username,
            Password = _settings.Password,
            Port = _settings.Port
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync("orders", ExchangeType.Topic, durable: true,
            cancellationToken: stoppingToken);

        var queueDeclareResult = await _channel.QueueDeclareAsync(
            "inventory.order-events", durable: true, exclusive: false, autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(queueDeclareResult.QueueName, "orders", "order.confirmed",
            cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(queueDeclareResult.QueueName, "orders", "order.cancelled",
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(0, 10, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var routingKey = ea.RoutingKey;

                _logger.LogInformation("Received {RoutingKey} event", routingKey);

                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

                switch (routingKey)
                {
                    case "order.confirmed":
                        await HandleOrderConfirmed(mediator, body, stoppingToken);
                        break;
                    case "order.cancelled":
                        await HandleOrderCancelled(mediator, body, stoppingToken);
                        break;
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {RoutingKey}", ea.RoutingKey);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true, stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync("inventory.order-events", autoAck: false,
            consumer: consumer, cancellationToken: stoppingToken);

        _logger.LogInformation("Order events consumer started — listening on inventory.order-events");

        // Keep running until cancelled
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private static async Task HandleOrderConfirmed(ISender mediator, string body, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var orderId = root.GetProperty("OrderId").GetGuid();
        var items = root.GetProperty("Items").EnumerateArray()
            .Select(i => new ReserveInventoryCommand.ItemDto(
                i.GetProperty("Sku").GetString()!,
                i.GetProperty("Quantity").GetInt32()))
            .ToList();

        await mediator.Send(new ReserveInventoryCommand(orderId, items), ct);
    }

    private static async Task HandleOrderCancelled(ISender mediator, string body, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var orderId = root.GetProperty("OrderId").GetGuid();

        // In a real system, you'd look up what was reserved for this order.
        // For now, we publish a release event that downstream handlers can process.
        await mediator.Send(new ReleaseInventoryCommand(orderId, []), ct);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
