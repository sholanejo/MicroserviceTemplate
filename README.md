# 🏗️ .NET Microservices Template

A production-ready, open-source microservices template built with **.NET 8**, **Clean Architecture**, **CQRS/MediatR**, **RabbitMQ**, **PostgreSQL**, **Redis**, and **OpenTelemetry**. Designed as a reference architecture for building scalable distributed systems.

![.NET 8](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)
![Build](https://img.shields.io/github/actions/workflow/status/sholanejo/microservice-template/ci.yml?branch=main)

---

## Why This Exists

Most microservice templates are either too trivial (todo apps) or too bloated (100+ projects with no docs). This template sits in the sweet spot — **2 real services** with production patterns you'd actually use, fully documented architecture decisions, and a single `docker compose up` to run everything.

## Architecture Overview

```
┌─────────────┐     ┌─────────────────┐     ┌──────────────────┐
│  API Gateway │────▶│  Order Service   │────▶│ Inventory Service │
│   (YARP)     │     │  (.NET 8 API)   │     │  (.NET 8 API)    │
└─────────────┘     └────────┬────────┘     └────────┬─────────┘
                             │                        │
                    ┌────────▼────────┐      ┌───────▼────────┐
                    │   PostgreSQL    │      │   PostgreSQL    │
                    │  (Order DB)     │      │ (Inventory DB)  │
                    └─────────────────┘      └────────────────┘
                             │                        │
                    ┌────────▼────────────────────────▼────────┐
                    │              RabbitMQ                     │
                    │     (Async Messaging / Events)           │
                    └──────────────────────────────────────────┘
                             │
                    ┌────────▼─────────────────────────────────┐
                    │           Observability Stack             │
                    │  OpenTelemetry → Jaeger (traces)         │
                    │  Prometheus → Grafana (metrics)          │
                    └──────────────────────────────────────────┘
```

## Key Patterns Implemented

| Pattern | Where | Why |
|---|---|---|
| **Clean Architecture** | Each service | Separation of concerns, testability |
| **CQRS + MediatR** | Application layer | Decouples reads/writes, pipeline behaviors |
| **Outbox Pattern** | Infrastructure layer | Guarantees event delivery with DB transactions |
| **Repository Pattern** | Infrastructure layer | Abstracts data access, enables testing |
| **Domain Events** | Domain layer | Loose coupling between aggregates |
| **Saga (Choreography)** | Cross-service | Distributed transaction management |
| **Circuit Breaker** | HTTP clients (Polly) | Resilience for external calls |
| **Health Checks** | API layer | Readiness/liveness for orchestrators |

## Project Structure

```
src/
├── Services/
│   ├── OrderService/
│   │   ├── OrderService.API            → Controllers, middleware, DI setup
│   │   ├── OrderService.Application    → Commands, queries, MediatR handlers
│   │   ├── OrderService.Domain         → Entities, value objects, domain events
│   │   └── OrderService.Infrastructure → EF Core, RabbitMQ, repositories
│   └── InventoryService/
│       ├── InventoryService.API
│       ├── InventoryService.Application
│       ├── InventoryService.Domain
│       └── InventoryService.Infrastructure
├── BuildingBlocks/
│   ├── BuildingBlocks.Messaging        → RabbitMQ abstractions
│   ├── BuildingBlocks.Observability    → OpenTelemetry setup
│   └── BuildingBlocks.Common           → Shared exceptions, interfaces, models
tests/
├── OrderService.UnitTests
└── OrderService.IntegrationTests
docs/
└── adr/                                → Architecture Decision Records
```

## Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Run Everything

```bash
# Clone the repo
git clone https://github.com/sholanejo/MicroserviceTemplate.git
cd MicroserviceTemplate

# Start infrastructure + services
docker compose up -d

# Verify services are healthy
curl http://localhost:5100/health   # Order Service
curl http://localhost:5200/health   # Inventory Service
```

### Run Locally (Development)

```bash
# Start only infrastructure
docker compose up -d order-db inventory-db rabbitmq redis jaeger prometheus grafana

# Run services
cd src/Services/OrderService/OrderService.API
dotnet run

# In another terminal
cd src/Services/InventoryService/InventoryService.API
dotnet run
```

### Run Tests

```bash
dotnet test --verbosity normal
```

## API Endpoints

### Order Service (`:5100`)

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/orders` | Create a new order |
| `GET` | `/api/orders/{id}` | Get order by ID |
| `GET` | `/api/orders` | List orders (paginated) |
| `PUT` | `/api/orders/{id}/cancel` | Cancel an order |

### Inventory Service (`:5200`)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/inventory/{sku}` | Check stock for a SKU |
| `POST` | `/api/inventory/reserve` | Reserve inventory |
| `POST` | `/api/inventory/release` | Release reserved inventory |

## Architecture Decision Records

See [`docs/adr/`](docs/adr/) for documented decisions:

- [ADR-001: Why Clean Architecture](docs/adr/001-clean-architecture.md)
- [ADR-002: CQRS with MediatR](docs/adr/002-cqrs-mediatr.md)
- [ADR-003: Outbox Pattern for Reliable Messaging](docs/adr/003-outbox-pattern.md)
- [ADR-004: Choreography-based Sagas](docs/adr/004-choreography-sagas.md)

## Configuration

Environment variables (also in `docker-compose.yml`):

| Variable | Default | Description |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | see docker-compose | PostgreSQL connection string |
| `RabbitMQ__Host` | `localhost` | RabbitMQ hostname |
| `RabbitMQ__Username` | `guest` | RabbitMQ username |
| `RabbitMQ__Password` | `guest` | RabbitMQ password |
| `Redis__ConnectionString` | `localhost:6379` | Redis connection string |

## Observability

Once running, access:

- **Jaeger UI** (traces): http://localhost:16686
- **Grafana** (metrics): http://localhost:3000
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)

## Contributing

1. Fork the repo
2. Create a feature branch (`git checkout -b feature/amazing-pattern`)
3. Commit your changes
4. Push to the branch
5. Open a Pull Request

## License

MIT — use this however you want, commercially or otherwise.

---

Built by [Timothy Nejo](https://linkedin.com/in/shola-nejo) — Senior Software Engineer passionate about clean architecture and distributed systems.
