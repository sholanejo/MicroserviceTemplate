using BuildingBlocks.Common.Models;
using InventoryService.Application.Interfaces;
using InventoryService.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Persistence;

public sealed class InventoryDbContext(
    DbContextOptions<InventoryDbContext> options,
    IMediator mediator) : DbContext(options), IUnitOfWork
{
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>(b =>
        {
            b.HasKey(i => i.Id);
            b.Property(i => i.Sku).HasMaxLength(50);
            b.Property(i => i.ProductName).HasMaxLength(200);
            b.HasIndex(i => i.Sku).IsUnique();
        });

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.HasKey(m => m.Id);
            b.Property(m => m.Type).HasMaxLength(500);
            b.HasIndex(m => m.ProcessedAt);
        });

        // Seed sample inventory data
        modelBuilder.Entity<InventoryItem>().HasData(
            CreateSeed("SKU-001", "Widget", 100),
            CreateSeed("SKU-002", "Gadget", 50),
            CreateSeed("SKU-003", "Doohickey", 200));
    }

    private static object CreateSeed(string sku, string name, int qty) => new
    {
        Id = Guid.NewGuid(),
        Sku = sku,
        ProductName = name,
        QuantityOnHand = qty,
        QuantityReserved = 0,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = (DateTime?)null
    };

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var aggregates = ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        foreach (var domainEvent in domainEvents)
        {
            OutboxMessages.Add(OutboxMessage.Create(domainEvent));
        }

        var result = await base.SaveChangesAsync(ct);

        foreach (var domainEvent in domainEvents)
            await mediator.Publish(domainEvent, ct);

        return result;
    }
}

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(object domainEvent) => new()
    {
        Id = Guid.NewGuid(),
        Type = domainEvent.GetType().AssemblyQualifiedName!,
        Payload = System.Text.Json.JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
        CreatedAt = DateTime.UtcNow
    };
}
