using OrderService.Domain.Exceptions;
using OrderService.Domain.ValueObjects;
using BuildingBlocks.Common.Models;

namespace OrderService.Domain.Entities;

public sealed class OrderItem : Entity
{
    public Guid OrderId { get; private set; }
    public string Sku { get; private set; } = default!;
    public string ProductName { get; private set; } = default!;
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = default!;

    private OrderItem() { } // EF Core

    internal static OrderItem Create(
        Guid orderId, string sku, string productName,
        int quantity, decimal unitPrice, string currency)
    {
        return new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Sku = sku,
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = new Money(unitPrice, currency)
        };
    }

    internal void IncreaseQuantity(int additionalQuantity)
    {
        if (additionalQuantity <= 0)
            throw new OrderDomainException("Additional quantity must be positive.");

        Quantity += additionalQuantity;
    }
}
