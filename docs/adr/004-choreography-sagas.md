# ADR-004: Choreography-based Sagas

**Status:** Accepted  
**Date:** 2026-01-15  
**Decision Makers:** Timothy Nejo

## Context

When an order is confirmed, inventory must be reserved. If reservation fails, the order should be cancelled. This is a distributed transaction spanning two services with independent databases.

## Decision

We use choreography-based sagas where each service reacts to events and publishes its own events in response:

```
OrderService                    InventoryService
     │                                │
     ├── OrderConfirmed ──────────────▶│
     │                                ├── InventoryReserved ───▶ (success)
     │◀── InventoryReservationFailed ──┤
     ├── OrderCancelled               │
     │                                ├── InventoryReleased
```

Each service owns its step. Compensation (rollback) is triggered by failure events.

## Consequences

**Positive:**
- No central coordinator — services are truly decoupled.
- Easy to add new services that react to existing events.
- Each service can be deployed and scaled independently.

**Negative:**
- Harder to visualize the full saga flow (no single place to see the whole picture).
- Debugging failures requires correlating events across services (solved with correlation IDs + distributed tracing).
- Risk of cyclic dependencies if not carefully designed.

## Alternatives Considered

- **Orchestration-based saga**: A dedicated orchestrator service coordinates the flow. Easier to understand but creates a single point of failure and tighter coupling.
