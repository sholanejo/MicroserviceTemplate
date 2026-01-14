using OrderService.Domain.Enums;
using OrderService.Domain.Events;
using OrderService.Domain.Exceptions;
using OrderService.Domain.ValueObjects;
using BuildingBlocks.Common.Models;

namespace OrderService.Domain.Entities;

public sealed class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = [];

    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; } = Money.Zero("USD");
    public Address ShippingAddress { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { } // EF Core

    public static Order Create(Guid customerId, Address shippingAddress)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            ShippingAddress = shippingAddress,
            CreatedAt = DateTime.UtcNow
        };

        order.AddDomainEvent(new OrderCreatedEvent(order.Id, customerId));
        return order;
    }

    public void AddItem(string sku, string productName, int quantity, decimal unitPrice, string currency)
    {
        if (Status != OrderStatus.Pending)
            throw new OrderDomainException("Cannot modify a non-pending order.");

        if (quantity <= 0)
            throw new OrderDomainException("Quantity must be greater than zero.");

        var existingItem = _items.FirstOrDefault(i => i.Sku == sku);
        if (existingItem is not null)
        {
            existingItem.IncreaseQuantity(quantity);
        }
        else
        {
            _items.Add(OrderItem.Create(Id, sku, productName, quantity, unitPrice, currency));
        }

        RecalculateTotal();
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new OrderDomainException($"Cannot confirm order in '{Status}' status.");

        if (_items.Count == 0)
            throw new OrderDomainException("Cannot confirm an order with no items.");

        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new OrderConfirmedEvent(Id, _items.Select(i =>
            new OrderConfirmedEvent.OrderItemDto(i.Sku, i.Quantity)).ToList()));
    }

    public void Cancel(string reason)
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered)
            throw new OrderDomainException($"Cannot cancel order in '{Status}' status.");

        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new OrderCancelledEvent(Id, reason));
    }

    public void MarkShipped()
    {
        if (Status != OrderStatus.Confirmed)
            throw new OrderDomainException("Only confirmed orders can be shipped.");

        Status = OrderStatus.Shipped;
        UpdatedAt = DateTime.UtcNow;
    }

    private void RecalculateTotal()
    {
        var currency = _items.FirstOrDefault()?.UnitPrice.Currency ?? "USD";
        var total = _items.Sum(i => i.UnitPrice.Amount * i.Quantity);
        TotalAmount = new Money(total, currency);
    }
}
