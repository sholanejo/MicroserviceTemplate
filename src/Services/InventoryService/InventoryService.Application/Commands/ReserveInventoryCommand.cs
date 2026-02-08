using BuildingBlocks.Common.Models;
using InventoryService.Application.Interfaces;
using InventoryService.Domain.Events;
using MediatR;

namespace InventoryService.Application.Commands;

public sealed record ReserveInventoryCommand(
    Guid OrderId,
    List<ReserveInventoryCommand.ItemDto> Items) : IRequest<Result<bool>>
{
    public sealed record ItemDto(string Sku, int Quantity);
}

public sealed class ReserveInventoryCommandHandler(
    IInventoryRepository repository,
    IUnitOfWork unitOfWork,
    IMediator mediator) : IRequestHandler<ReserveInventoryCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ReserveInventoryCommand request, CancellationToken ct)
    {
        var reservedItems = new List<(string Sku, int Quantity)>();

        foreach (var item in request.Items)
        {
            var inventory = await repository.GetBySkuAsync(item.Sku, ct);

            if (inventory is null)
            {
                // Rollback any already-reserved items
                await RollbackReservations(request.OrderId, reservedItems, ct);
                await mediator.Publish(new InventoryReservationFailedEvent(
                    request.OrderId, item.Sku, item.Quantity, 0, $"SKU '{item.Sku}' not found."), ct);
                return Result<bool>.Failure($"SKU '{item.Sku}' not found.");
            }

            if (inventory.QuantityAvailable < item.Quantity)
            {
                await RollbackReservations(request.OrderId, reservedItems, ct);
                await mediator.Publish(new InventoryReservationFailedEvent(
                    request.OrderId, item.Sku, item.Quantity, inventory.QuantityAvailable,
                    "Insufficient stock."), ct);
                return Result<bool>.Failure(
                    $"Insufficient stock for '{item.Sku}'. Available: {inventory.QuantityAvailable}.");
            }

            inventory.Reserve(request.OrderId, item.Quantity);
            reservedItems.Add((item.Sku, item.Quantity));
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    private async Task RollbackReservations(
        Guid orderId, List<(string Sku, int Quantity)> reserved, CancellationToken ct)
    {
        foreach (var (sku, quantity) in reserved)
        {
            var inventory = await repository.GetBySkuAsync(sku, ct);
            inventory?.Release(orderId, quantity);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
