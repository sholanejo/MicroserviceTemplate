using BuildingBlocks.Messaging;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Events;

namespace OrderService.Infrastructure.Messaging;

public sealed class OrderConfirmedEventHandler(
    IEventPublisher publisher,
    ILogger<OrderConfirmedEventHandler> logger)
    : INotificationHandler<OrderConfirmedEvent>
{
    public async Task Handle(OrderConfirmedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("Order {OrderId} confirmed — publishing inventory reservation request",
            notification.OrderId);

        var integrationEvent = new
        {
            notification.OrderId,
            Items = notification.Items.Select(i => new { i.Sku, i.Quantity }),
            Timestamp = DateTime.UtcNow
        };

        await publisher.PublishAsync(
            integrationEvent,
            exchange: "orders",
            routingKey: "order.confirmed",
            ct);
    }
}

public sealed class OrderCancelledEventHandler(
    IEventPublisher publisher,
    ILogger<OrderCancelledEventHandler> logger)
    : INotificationHandler<OrderCancelledEvent>
{
    public async Task Handle(OrderCancelledEvent notification, CancellationToken ct)
    {
        logger.LogInformation("Order {OrderId} cancelled — publishing inventory release",
            notification.OrderId);

        await publisher.PublishAsync(
            new { notification.OrderId, notification.Reason, Timestamp = DateTime.UtcNow },
            exchange: "orders",
            routingKey: "order.cancelled",
            ct);
    }
}
