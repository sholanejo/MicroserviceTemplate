using MediatR;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.ValueObjects;
using BuildingBlocks.Common.Models;

namespace OrderService.Application.Commands;

// ── Command ──────────────────────────────────────────────────
public sealed record CreateOrderCommand(
    Guid CustomerId,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country,
    List<CreateOrderCommand.ItemDto> Items) : IRequest<Result<Guid>>
{
    public sealed record ItemDto(string Sku, string ProductName, int Quantity, decimal UnitPrice, string Currency);
}

// ── Handler ──────────────────────────────────────────────────
public sealed class CreateOrderCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var address = new Address(
            request.Street, request.City, request.State,
            request.PostalCode, request.Country);

        var order = Order.Create(request.CustomerId, address);

        foreach (var item in request.Items)
        {
            order.AddItem(item.Sku, item.ProductName, item.Quantity, item.UnitPrice, item.Currency);
        }

        order.Confirm();

        await orderRepository.AddAsync(order, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(order.Id);
    }
}
