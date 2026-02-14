using InventoryService.Application.Commands;
using InventoryService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class InventoryController(ISender mediator) : ControllerBase
{
    /// <summary>Check stock for a SKU.</summary>
    [HttpGet("{sku}")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStock(string sku, CancellationToken ct)
    {
        var result = await mediator.Send(new GetStockBySkuQuery(sku), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    /// <summary>Reserve inventory for an order.</summary>
    [HttpPost("reserve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reserve([FromBody] ReserveRequest request, CancellationToken ct)
    {
        var command = new ReserveInventoryCommand(
            request.OrderId,
            request.Items.Select(i => new ReserveInventoryCommand.ItemDto(i.Sku, i.Quantity)).ToList());

        var result = await mediator.Send(command, ct);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    /// <summary>Release reserved inventory.</summary>
    [HttpPost("release")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Release([FromBody] ReleaseRequest request, CancellationToken ct)
    {
        var command = new ReleaseInventoryCommand(
            request.OrderId,
            request.Items.Select(i => new ReleaseInventoryCommand.ItemDto(i.Sku, i.Quantity)).ToList());

        var result = await mediator.Send(command, ct);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
}

public sealed record ReserveRequest(Guid OrderId, List<ReserveRequest.ReserveItem> Items)
{
    public sealed record ReserveItem(string Sku, int Quantity);
}

public sealed record ReleaseRequest(Guid OrderId, List<ReleaseRequest.ReleaseItem> Items)
{
    public sealed record ReleaseItem(string Sku, int Quantity);
}
