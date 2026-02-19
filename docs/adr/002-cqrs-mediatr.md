# ADR-002: CQRS with MediatR

**Status:** Accepted  
**Date:** 2026-01-15  
**Decision Makers:** Timothy Nejo

## Context

Our services handle both complex write operations (order creation with inventory checks, event publishing) and simple read operations (list orders, get by ID). Mixing these in a single service class leads to bloated classes and makes it hard to optimize reads and writes independently.

## Decision

We adopt CQRS (Command Query Responsibility Segregation) using MediatR as the in-process mediator.

- **Commands** modify state and return a result (`Result<T>`).
- **Queries** read state and return DTOs.
- **Pipeline behaviors** handle cross-cutting concerns (logging, validation) without polluting handlers.

Each command/query is a self-contained file with its request, handler, and (optionally) validator.

## Consequences

**Positive:**
- Each handler has a single responsibility — easy to test, easy to reason about.
- Pipeline behaviors eliminate repetitive try/catch/log/validate boilerplate.
- Read models can be optimized independently (e.g., raw SQL, caching) without affecting write logic.

**Negative:**
- More files per feature (request + handler + validator).
- MediatR adds a layer of indirection — stack traces are less obvious.
- For trivial CRUD, the ceremony exceeds the benefit.
