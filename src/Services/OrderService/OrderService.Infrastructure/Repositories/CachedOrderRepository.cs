using System.Text.Json;
using BuildingBlocks.Common.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Application.Queries;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Repositories;

public sealed class CachedOrderRepository(
    OrderRepository inner,
    IDistributedCache cache,
    ILogger<CachedOrderRepository> logger) : IOrderRepository
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // Don't cache tracked entities — only cache DTOs on the read path
        return await inner.GetByIdAsync(id, ct);
    }

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await inner.AddAsync(order, ct);
        // Invalidate list cache when a new order is added
        await cache.RemoveAsync("orders:page:1:20", ct);
    }

    public async Task<PagedResult<OrderDto>> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var cacheKey = $"orders:page:{page}:{pageSize}";

        var cached = await cache.GetStringAsync(cacheKey, ct);
        if (cached is not null)
        {
            logger.LogDebug("Cache HIT for {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<PagedResult<OrderDto>>(cached)!;
        }

        logger.LogDebug("Cache MISS for {CacheKey}", cacheKey);
        var result = await inner.GetPagedAsync(page, pageSize, ct);

        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration }, ct);

        return result;
    }
}
