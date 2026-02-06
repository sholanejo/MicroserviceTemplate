using MediatR;

namespace InventoryService.Domain.Events;

public sealed record InventoryReservedEvent(Guid OrderId, string Sku, int Quantity) : INotification;

public sealed record InventoryReleasedEvent(Guid OrderId, string Sku, int Quantity) : INotification;

public sealed record InventoryReservationFailedEvent(
    Guid OrderId, string Sku, int RequestedQuantity, int AvailableQuantity, string Reason) : INotification;
