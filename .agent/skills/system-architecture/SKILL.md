---
name: system-architecture
description: Design and evolve Pavena's ASP.NET Core modular monolith, module boundaries, integration patterns, and deployment topology.
---

# System Architecture Skill

## Default architecture

Use a modular monolith for the primary Pavena application. Keep deployable components few and boundaries explicit.

Recommended logical layers:
- Domain: entities, value objects, domain rules, domain events.
- Application: use cases, commands/queries, DTOs, ports, authorization requirements.
- Infrastructure: EF Core, identity persistence, storage adapters, email, external APIs.
- Web/API: HTTP endpoints, MVC/Razor UI, middleware, request/response mapping.
- Worker: asynchronous jobs such as exports, email, snapshots, cleanup.
- Collaboration service: separate Yjs/Hocuspocus process when real-time collaboration is implemented.

Do not split modules into microservices merely because they are separate folders.

## Suggested bounded contexts

1. Identity & Profiles
2. Social Graph
3. Organizations
4. Research Projects
5. Documents & Collaboration
6. References & Publications
7. Notifications & Messaging
8. Platform Governance
9. File/Media Storage

Each module owns its business rules and application use cases. Other modules should use public application contracts or domain events rather than reaching into internal tables/classes.

## Dependency direction

- Domain must not depend on Application, Infrastructure, Web, or external frameworks.
- Application may depend on Domain and abstractions.
- Infrastructure implements Application/Domain abstractions.
- Web/API depends on Application and composition-root wiring.
- Avoid circular dependencies between modules.

## Integration patterns

- Use synchronous application interfaces for immediate same-process use cases.
- Use domain/application events for decoupled side effects.
- Use a transactional outbox when reliable asynchronous event delivery is needed.
- Do not publish an event before its database transaction commits.
- Make background jobs idempotent.
- Use explicit contracts for communication between ASP.NET Core and the collaboration service.

## Realtime boundaries

- SignalR may be used for notifications, online presence, and application-level events.
- Yjs handles CRDT document state and concurrent edits.
- Hocuspocus is a candidate Yjs WebSocket server; validate authentication, authorization, persistence, and scale requirements before adopting.
- The collaboration service must validate a short-lived, scoped token or equivalent authorization proof issued by the ASP.NET Core application.
- Never trust a client-supplied document ID as proof of access.

## Storage boundaries

- Relational database stores metadata, permissions, references, version records, and necessary CRDT update/snapshot data.
- Object storage stores large files and generated artifacts.
- Store object keys, checksums, content type, size, and ownership metadata in the database.
- Do not store large uploads as ordinary relational text columns without a justified reason.

## Configuration

- Use strongly typed options and environment-based configuration.
- Keep secrets outside source control.
- Separate development, test, staging, and production settings.
- Use health checks for database, object storage, and collaboration service.
- Add correlation IDs and structured logs.

## Architecture decisions

Create an ADR for changes to:
- Framework/runtime major version.
- Module boundaries.
- Database/provider strategy.
- Collaboration protocol or server.
- Authentication/session model.
- Object storage.
- Search infrastructure.
- Deployment topology.

Do not introduce Kubernetes, Redis, Kafka, Elasticsearch, or a microservice fleet in the MVP without a demonstrated requirement.
