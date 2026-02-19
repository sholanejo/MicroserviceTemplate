# ADR-001: Clean Architecture

**Status:** Accepted  
**Date:** 2026-01-15  
**Decision Makers:** Timothy Nejo

## Context

We need a consistent project structure across microservices that enforces separation of concerns, enables unit testing without infrastructure dependencies, and makes onboarding new engineers straightforward.

## Decision

Each service follows Clean Architecture with four layers:

- **Domain** — Entities, value objects, domain events, business rules. Zero external dependencies.
- **Application** — Use cases (commands/queries), interfaces, DTOs. Depends only on Domain.
- **Infrastructure** — EF Core, RabbitMQ, external APIs. Implements Application interfaces.
- **API** — Controllers, middleware, DI composition root. References all layers.

The dependency rule is strict: inner layers never reference outer layers. Infrastructure details are always behind interfaces defined in Application.

## Consequences

**Positive:**
- Domain logic is testable in isolation (no database, no message broker).
- Swapping infrastructure (e.g., PostgreSQL → DynamoDB) requires changes only in Infrastructure.
- Clear boundaries make code review faster — reviewers know where to look.

**Negative:**
- More projects per service (4 vs 1-2 in simpler approaches).
- Mapping between layers (Entity → DTO) adds boilerplate.
- Overkill for trivially simple CRUD services.

## Alternatives Considered

- **Vertical Slices**: Less boilerplate but harder to enforce boundaries in a team setting.
- **Single project with folders**: Simpler but inevitably degrades into spaghetti references.
