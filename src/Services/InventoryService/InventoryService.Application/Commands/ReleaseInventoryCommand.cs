using BuildingBlocks.Common.Models;
using InventoryService.Application.Interfaces;
using MediatR;

namespace InventoryService.Application.Commands;

public sealed record ReleaseInventoryCommand(
    Guid OrderId,
    List<ReleaseInventoryCommand.ItemDto> Items) : IRequest<Result<bool>>
{
    public sealed record ItemDto(string Sku, int Quantity);
}

public sealed class ReleaseInventoryCommandHandler(
    IInventoryRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<ReleaseInventoryCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ReleaseInventoryCommand request, CancellationToken ct)
    {
        foreach (var item in request.Items)
        {
            var inventory = await repository.GetBySkuAsync(item.Sku, ct);
            if (inventory is null)
                return Result<bool>.Failure($"SKU '{item.Sku}' not found.");

            inventory.Release(request.OrderId, item.Quantity);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
