using MediatR;

namespace OrderService.Domain.Events;

public sealed record OrderCreatedEvent(Guid OrderId, Guid CustomerId) : INotification;

public sealed record OrderConfirmedEvent(
    Guid OrderId,
    List<OrderConfirmedEvent.OrderItemDto> Items) : INotification
{
    public sealed record OrderItemDto(string Sku, int Quantity);
}

public sealed record OrderCancelledEvent(Guid OrderId, string Reason) : INotification;
