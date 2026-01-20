using MediatR;
using OrderService.Application.Interfaces;
using BuildingBlocks.Common.Models;

namespace OrderService.Application.Queries;

// ── Response DTO ─────────────────────────────────────────────
public sealed record OrderDto(
    Guid Id,
    Guid CustomerId,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt,
    List<OrderItemDto> Items);

public sealed record OrderItemDto(
    string Sku,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

// ── Query ────────────────────────────────────────────────────
public sealed record GetOrderByIdQuery(Guid OrderId) : IRequest<Result<OrderDto>>;

public sealed class GetOrderByIdQueryHandler(
    IOrderRepository orderRepository) : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, ct);
        if (order is null)
            return Result<OrderDto>.Failure($"Order {request.OrderId} not found.");

        var dto = new OrderDto(
            order.Id,
            order.CustomerId,
            order.Status.ToString(),
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            order.CreatedAt,
            order.Items.Select(i => new OrderItemDto(
                i.Sku, i.ProductName, i.Quantity, i.UnitPrice.Amount)).ToList());

        return Result<OrderDto>.Success(dto);
    }
}

// ── Paginated List Query ─────────────────────────────────────
public sealed record GetOrdersQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<OrderDto>>;

public sealed class GetOrdersQueryHandler(
    IOrderRepository orderRepository) : IRequestHandler<GetOrdersQuery, PagedResult<OrderDto>>
{
    public async Task<PagedResult<OrderDto>> Handle(GetOrdersQuery request, CancellationToken ct)
    {
        return await orderRepository.GetPagedAsync(request.Page, request.PageSize, ct);
    }
}
