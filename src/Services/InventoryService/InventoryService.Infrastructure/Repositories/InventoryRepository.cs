using InventoryService.Application.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Repositories;

public sealed class InventoryRepository(InventoryDbContext context) : IInventoryRepository
{
    public async Task<InventoryItem?> GetBySkuAsync(string sku, CancellationToken ct = default)
    {
        return await context.InventoryItems
            .FirstOrDefaultAsync(i => i.Sku == sku, ct);
    }

    public async Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.InventoryItems.FindAsync([id], ct);
    }

    public async Task AddAsync(InventoryItem item, CancellationToken ct = default)
    {
        await context.InventoryItems.AddAsync(item, ct);
    }

    public async Task<List<InventoryItem>> GetAllAsync(CancellationToken ct = default)
    {
        return await context.InventoryItems
            .OrderBy(i => i.Sku)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
