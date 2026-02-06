using BuildingBlocks.Common.Models;
using InventoryService.Domain.Events;
using InventoryService.Domain.Exceptions;

namespace InventoryService.Domain.Entities;

public sealed class InventoryItem : AggregateRoot
{
    public string Sku { get; private set; } = default!;
    public string ProductName { get; private set; } = default!;
    public int QuantityOnHand { get; private set; }
    public int QuantityReserved { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public int QuantityAvailable => QuantityOnHand - QuantityReserved;

    private InventoryItem() { } // EF Core

    public static InventoryItem Create(string sku, string productName, int initialQuantity)
    {
        if (initialQuantity < 0)
            throw new InventoryDomainException("Initial quantity cannot be negative.");

        return new InventoryItem
        {
            Id = Guid.NewGuid(),
            Sku = sku,
            ProductName = productName,
            QuantityOnHand = initialQuantity,
            QuantityReserved = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Reserve(Guid orderId, int quantity)
    {
        if (quantity <= 0)
            throw new InventoryDomainException("Reserve quantity must be positive.");

        if (quantity > QuantityAvailable)
            throw new InventoryDomainException(
                $"Insufficient stock for SKU '{Sku}'. Available: {QuantityAvailable}, Requested: {quantity}.");

        QuantityReserved += quantity;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new InventoryReservedEvent(orderId, Sku, quantity));
    }

    public void Release(Guid orderId, int quantity)
    {
        if (quantity <= 0)
            throw new InventoryDomainException("Release quantity must be positive.");

        if (quantity > QuantityReserved)
            throw new InventoryDomainException(
                $"Cannot release {quantity} units — only {QuantityReserved} reserved for SKU '{Sku}'.");

        QuantityReserved -= quantity;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new InventoryReleasedEvent(orderId, Sku, quantity));
    }

    public void Restock(int quantity)
    {
        if (quantity <= 0)
            throw new InventoryDomainException("Restock quantity must be positive.");

        QuantityOnHand += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deduct(int quantity)
    {
        if (quantity <= 0)
            throw new InventoryDomainException("Deduct quantity must be positive.");

        if (quantity > QuantityReserved)
            throw new InventoryDomainException("Can only deduct previously reserved quantities.");

        QuantityOnHand -= quantity;
        QuantityReserved -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }
}
