using BuildingBlocks.Common.Models;
using InventoryService.Application.Interfaces;
using MediatR;

namespace InventoryService.Application.Queries;

public sealed record InventoryDto(
    Guid Id,
    string Sku,
    string ProductName,
    int QuantityOnHand,
    int QuantityReserved,
    int QuantityAvailable);

public sealed record GetStockBySkuQuery(string Sku) : IRequest<Result<InventoryDto>>;

public sealed class GetStockBySkuQueryHandler(
    IInventoryRepository repository) : IRequestHandler<GetStockBySkuQuery, Result<InventoryDto>>
{
    public async Task<Result<InventoryDto>> Handle(GetStockBySkuQuery request, CancellationToken ct)
    {
        var item = await repository.GetBySkuAsync(request.Sku, ct);
        if (item is null)
            return Result<InventoryDto>.Failure($"SKU '{request.Sku}' not found.");

        return Result<InventoryDto>.Success(new InventoryDto(
            item.Id, item.Sku, item.ProductName,
            item.QuantityOnHand, item.QuantityReserved, item.QuantityAvailable));
    }
}
