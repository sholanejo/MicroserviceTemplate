using Microsoft.EntityFrameworkCore;
using OrderService.Application.Interfaces;
using OrderService.Application.Queries;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Persistence;
using BuildingBlocks.Common.Models;

namespace OrderService.Infrastructure.Repositories;

public sealed class OrderRepository(OrderDbContext context) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await context.Orders.AddAsync(order, ct);
    }

    public async Task<PagedResult<OrderDto>> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .AsNoTracking();

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderDto(
                o.Id,
                o.CustomerId,
                o.Status.ToString(),
                o.TotalAmount.Amount,
                o.TotalAmount.Currency,
                o.CreatedAt,
                o.Items.Select(i => new OrderItemDto(
                    i.Sku, i.ProductName, i.Quantity, i.UnitPrice.Amount)).ToList()))
            .ToListAsync(ct);

        return new PagedResult<OrderDto>(items, totalCount, page, pageSize);
    }
}
