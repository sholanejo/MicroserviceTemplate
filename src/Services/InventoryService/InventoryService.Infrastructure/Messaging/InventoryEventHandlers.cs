using BuildingBlocks.Messaging;
using InventoryService.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace InventoryService.Infrastructure.Messaging;

public sealed class InventoryReservedEventHandler(
    IEventPublisher publisher,
    ILogger<InventoryReservedEventHandler> logger)
    : INotificationHandler<InventoryReservedEvent>
{
    public async Task Handle(InventoryReservedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("Inventory reserved for order {OrderId} — SKU {Sku} x{Quantity}",
            notification.OrderId, notification.Sku, notification.Quantity);

        await publisher.PublishAsync(
            new { notification.OrderId, notification.Sku, notification.Quantity, Timestamp = DateTime.UtcNow },
            exchange: "inventory",
            routingKey: "inventory.reserved",
            ct);
    }
}

public sealed class InventoryReservationFailedEventHandler(
    IEventPublisher publisher,
    ILogger<InventoryReservationFailedEventHandler> logger)
    : INotificationHandler<InventoryReservationFailedEvent>
{
    public async Task Handle(InventoryReservationFailedEvent notification, CancellationToken ct)
    {
        logger.LogWarning("Inventory reservation FAILED for order {OrderId} — SKU {Sku}: {Reason}",
            notification.OrderId, notification.Sku, notification.Reason);

        await publisher.PublishAsync(
            new
            {
                notification.OrderId,
                notification.Sku,
                notification.RequestedQuantity,
                notification.AvailableQuantity,
                notification.Reason,
                Timestamp = DateTime.UtcNow
            },
            exchange: "inventory",
            routingKey: "inventory.reservation-failed",
            ct);
    }
}
