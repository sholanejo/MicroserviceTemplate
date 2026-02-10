using InventoryService.Domain.Entities;

namespace InventoryService.Application.Interfaces;

public interface IInventoryRepository
{
    Task<InventoryItem?> GetBySkuAsync(string sku, CancellationToken ct = default);
    Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(InventoryItem item, CancellationToken ct = default);
    Task<List<InventoryItem>> GetAllAsync(CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
