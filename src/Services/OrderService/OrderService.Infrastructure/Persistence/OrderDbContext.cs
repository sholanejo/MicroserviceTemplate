using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using BuildingBlocks.Common.Models;

namespace OrderService.Infrastructure.Persistence;

public sealed class OrderDbContext(
    DbContextOptions<OrderDbContext> options,
    IMediator mediator) : DbContext(options), IUnitOfWork
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(b =>
        {
            b.HasKey(o => o.Id);
            b.Property(o => o.Status).HasConversion<string>();
            b.OwnsOne(o => o.TotalAmount, m =>
            {
                m.Property(p => p.Amount).HasColumnName("TotalAmount").HasPrecision(18, 2);
                m.Property(p => p.Currency).HasColumnName("Currency").HasMaxLength(3);
            });
            b.OwnsOne(o => o.ShippingAddress, a =>
            {
                a.Property(p => p.Street).HasMaxLength(200);
                a.Property(p => p.City).HasMaxLength(100);
                a.Property(p => p.State).HasMaxLength(100);
                a.Property(p => p.PostalCode).HasMaxLength(20);
                a.Property(p => p.Country).HasMaxLength(100);
            });
            b.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId);
            b.HasIndex(o => o.CustomerId);
            b.HasIndex(o => o.CreatedAt);
        });

        modelBuilder.Entity<OrderItem>(b =>
        {
            b.HasKey(i => i.Id);
            b.Property(i => i.Sku).HasMaxLength(50);
            b.Property(i => i.ProductName).HasMaxLength(200);
            b.OwnsOne(i => i.UnitPrice, m =>
            {
                m.Property(p => p.Amount).HasColumnName("UnitPrice").HasPrecision(18, 2);
                m.Property(p => p.Currency).HasColumnName("UnitPriceCurrency").HasMaxLength(3);
            });
        });

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.HasKey(m => m.Id);
            b.Property(m => m.Type).HasMaxLength(500);
            b.HasIndex(m => m.ProcessedAt);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Dispatch domain events before saving (outbox pattern)
        var aggregates = ChangeTracker.Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        // Persist outbox messages in the same transaction
        foreach (var domainEvent in domainEvents)
        {
            var outbox = OutboxMessage.Create(domainEvent);
            OutboxMessages.Add(outbox);
        }

        var result = await base.SaveChangesAsync(ct);

        // Publish events after successful save
        foreach (var domainEvent in domainEvents)
            await mediator.Publish(domainEvent, ct);

        return result;
    }
}

// ── Outbox Message Entity ────────────────────────────────────
public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(object domainEvent)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = domainEvent.GetType().AssemblyQualifiedName!,
            Payload = System.Text.Json.JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            CreatedAt = DateTime.UtcNow
        };
    }
}
