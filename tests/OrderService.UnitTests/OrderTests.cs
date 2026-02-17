using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Exceptions;
using OrderService.Domain.ValueObjects;
using Xunit;

namespace OrderService.UnitTests;

public class OrderTests
{
    private static Order CreateTestOrder()
    {
        var address = new Address("123 Main St", "Lagos", "LA", "100001", "Nigeria");
        return Order.Create(Guid.NewGuid(), address);
    }

    [Fact]
    public void Create_ShouldReturnPendingOrder_WithDomainEvent()
    {
        // Act
        var order = CreateTestOrder();

        // Assert
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Single(order.DomainEvents); // OrderCreatedEvent
    }

    [Fact]
    public void AddItem_ShouldAddItem_AndRecalculateTotal()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        order.AddItem("SKU-001", "Widget", 2, 25.00m, "USD");

        // Assert
        Assert.Single(order.Items);
        Assert.Equal(50.00m, order.TotalAmount.Amount);
        Assert.Equal("USD", order.TotalAmount.Currency);
    }

    [Fact]
    public void AddItem_WithExistingSku_ShouldIncreaseQuantity()
    {
        // Arrange
        var order = CreateTestOrder();
        order.AddItem("SKU-001", "Widget", 2, 25.00m, "USD");

        // Act
        order.AddItem("SKU-001", "Widget", 3, 25.00m, "USD");

        // Assert
        Assert.Single(order.Items);
        Assert.Equal(5, order.Items.First().Quantity);
        Assert.Equal(125.00m, order.TotalAmount.Amount);
    }

    [Fact]
    public void AddItem_WithZeroQuantity_ShouldThrow()
    {
        var order = CreateTestOrder();

        Assert.Throws<OrderDomainException>(() =>
            order.AddItem("SKU-001", "Widget", 0, 25.00m, "USD"));
    }

    [Fact]
    public void Confirm_WithItems_ShouldTransitionToConfirmed()
    {
        // Arrange
        var order = CreateTestOrder();
        order.AddItem("SKU-001", "Widget", 1, 10.00m, "USD");

        // Act
        order.Confirm();

        // Assert
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(2, order.DomainEvents.Count); // Created + Confirmed
    }

    [Fact]
    public void Confirm_WithNoItems_ShouldThrow()
    {
        var order = CreateTestOrder();

        Assert.Throws<OrderDomainException>(() => order.Confirm());
    }

    [Fact]
    public void Cancel_PendingOrder_ShouldTransitionToCancelled()
    {
        // Arrange
        var order = CreateTestOrder();
        order.AddItem("SKU-001", "Widget", 1, 10.00m, "USD");

        // Act
        order.Cancel("Customer changed mind");

        // Assert
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_ShippedOrder_ShouldThrow()
    {
        // Arrange
        var order = CreateTestOrder();
        order.AddItem("SKU-001", "Widget", 1, 10.00m, "USD");
        order.Confirm();
        order.MarkShipped();

        // Assert
        Assert.Throws<OrderDomainException>(() => order.Cancel("Too late"));
    }

    [Fact]
    public void AddItem_ToConfirmedOrder_ShouldThrow()
    {
        // Arrange
        var order = CreateTestOrder();
        order.AddItem("SKU-001", "Widget", 1, 10.00m, "USD");
        order.Confirm();

        // Assert
        Assert.Throws<OrderDomainException>(() =>
            order.AddItem("SKU-002", "Gadget", 1, 15.00m, "USD"));
    }

    [Fact]
    public void MarkShipped_OnlyFromConfirmed_ShouldWork()
    {
        // Arrange
        var order = CreateTestOrder();
        order.AddItem("SKU-001", "Widget", 1, 10.00m, "USD");
        order.Confirm();

        // Act
        order.MarkShipped();

        // Assert
        Assert.Equal(OrderStatus.Shipped, order.Status);
    }

    [Fact]
    public void MarkShipped_FromPending_ShouldThrow()
    {
        var order = CreateTestOrder();
        order.AddItem("SKU-001", "Widget", 1, 10.00m, "USD");

        Assert.Throws<OrderDomainException>(() => order.MarkShipped());
    }
}
