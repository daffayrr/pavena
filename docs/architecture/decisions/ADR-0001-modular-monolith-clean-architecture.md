# ADR-0001: Modular Monolith and Clean Architecture on .NET 8 LTS

## Status
Accepted

## Date
2026-11-03

## Context
Pavena is an individual-first academic collaboration platform integrating social networking, research workspaces, and collaborative editors. A microservices architecture at this stage would introduce high operational complexity, distributed transaction hurdles, and deployment friction. A poorly structured monolithic application would create tight coupling and impede future extraction.

## Decision
We adopt a **Modular Monolith** architecture implemented with **Clean / Onion Architecture** on **ASP.NET Core (.NET 8.0 LTS, C# 12)**:
- **`Pavena.Domain`**: Core business entities and invariants, zero external dependencies.
- **`Pavena.Application`**: Use-case orchestration, service interfaces, DTOs, and input validation.
- **`Pavena.Infrastructure`**: Persistence (EF Core), storage, identity, and third-party integrations.
- **`Pavena.API`**: REST presentation layer, composition root, and authorization handlers.

Per project guidelines, we explicitly avoid complex mediator patterns (MediatR), CQRS indirection, or event sourcing in Phase 1, favoring cohesive application services (`IAuthenticationService`, `IOrganizationService`).

## Consequences
- **Positive:** Single deployment unit, straightforward transactions, zero distributed network latency, clear domain boundaries.
- **Trade-off:** In-process modules must strictly respect project boundaries to avoid architectural drift. Enforced via clean project references.
