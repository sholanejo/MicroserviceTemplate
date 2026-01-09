using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace BuildingBlocks.Messaging;

// ── Configuration ────────────────────────────────────────────
public sealed class RabbitMqSettings
{
    public string Host { get; set; } = "localhost";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public int Port { get; set; } = 5672;
}

// ── Abstractions ─────────────────────────────────────────────
public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, string exchange, string routingKey, CancellationToken ct = default)
        where T : class;
}

// ── Implementation ───────────────────────────────────────────
public sealed class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    private RabbitMqEventPublisher(IConnection connection, IChannel channel, ILogger<RabbitMqEventPublisher> logger)
    {
        _connection = connection;
        _channel = channel;
        _logger = logger;
    }

    public static async Task<RabbitMqEventPublisher> CreateAsync(
        IOptions<RabbitMqSettings> settings, ILogger<RabbitMqEventPublisher> logger)
    {
        var config = settings.Value;
        var factory = new ConnectionFactory
        {
            HostName = config.Host,
            UserName = config.Username,
            Password = config.Password,
            Port = config.Port
        };

        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        return new RabbitMqEventPublisher(connection, channel, logger);
    }

    public async Task PublishAsync<T>(T @event, string exchange, string routingKey, CancellationToken ct = default)
        where T : class
    {
        await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);

        var json = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = Guid.NewGuid().ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await _channel.BasicPublishAsync(exchange, routingKey, mandatory: false, props, body, ct);

        _logger.LogInformation("Published {EventType} to {Exchange}/{RoutingKey}",
            typeof(T).Name, exchange, routingKey);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }
}
