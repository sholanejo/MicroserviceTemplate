using OrderService.Application.Queries;
using OrderService.Domain.Entities;
using BuildingBlocks.Common.Models;

namespace OrderService.Application.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    Task<PagedResult<OrderDto>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
