# ADR-003: Outbox Pattern for Reliable Messaging

**Status:** Accepted  
**Date:** 2026-01-15  
**Decision Makers:** Timothy Nejo

## Context

When an order is confirmed, we need to both update the database and publish an event to RabbitMQ. These are two separate systems — if the DB write succeeds but the publish fails (or vice versa), we end up with inconsistent state. This is the classic dual-write problem.

## Decision

We implement the Transactional Outbox pattern:

1. Domain events are collected on the aggregate root during the business operation.
2. On `SaveChangesAsync`, outbox messages are written to an `OutboxMessages` table in the **same database transaction** as the domain state change.
3. After the transaction commits, events are dispatched via MediatR (for in-process handlers) and published to RabbitMQ (for cross-service handlers).
4. A background processor can retry any outbox messages that failed to publish.

## Consequences

**Positive:**
- Guarantees at-least-once delivery — if the DB transaction commits, the event will eventually be published.
- No distributed transactions (2PC) needed.
- Failed publishes are retryable from the outbox table.

**Negative:**
- Consumers must be idempotent (at-least-once means possible duplicates).
- Adds an `OutboxMessages` table and background processor complexity.
- Slight increase in DB write load (one extra insert per event).
