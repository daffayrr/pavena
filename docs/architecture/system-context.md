# Pavena — System Context & High-Level Architecture

**Document Version:** 1.0  
**Phase:** Phase 1 Foundation Engineering  

---

## 1. System Overview

Pavena is an individual-first Academic Collaboration Platform providing social networking, research workspaces, collaborative scientific writing, and publication discovery.

```mermaid
C4Context
    title System Context Diagram - Pavena Platform

    Person(individual, "Independent Scholar / User", "Student, lecturer, researcher, teacher, or professional.")
    Person(admin, "Platform Administrator", "Superadmin or Platform Admin managing platform governance.")
    System_Ext(org, "Affiliated Organization", "University, research institute, school, or enterprise.")

    Enterprise_Boundary(b0, "Pavena Platform Boundary") {
        System(pavena_web, "Pavena Web & API Application", "ASP.NET Core Modular Monolith providing REST APIs, authentication, and core domain services.")
        System(pavena_collab, "Collaboration Service", "Node.js / Hocuspocus CRDT real-time synchronization server.")
        System(pavena_worker, "Background Worker", "Isolated task runner for scheduled maintenance and LaTeX compilation.")
        SystemDb(pavena_db, "Primary Relational Database", "MySQL 8.0 (PostgreSQL migration-ready).")
        SystemDb(pavena_storage, "Object Storage", "MinIO / S3-compatible storage for uploads, avatars, and build artifacts.")
    }

    Rel(individual, pavena_web, "Interacts via HTTPS REST / Cookies")
    Rel(individual, pavena_collab, "Collaborative editing via Secure WebSocket")
    Rel(admin, pavena_web, "Administers platform via Admin Portal")
    Rel(pavena_web, pavena_db, "Reads/Writes via EF Core")
    Rel(pavena_web, pavena_storage, "Uploads/Generates signed URLs")
    Rel(pavena_collab, pavena_web, "Validates scoped JWT tokens via REST / JWKS")
    Rel(pavena_worker, pavena_db, "Pulls queued jobs")
    Rel(pavena_worker, pavena_storage, "Stores compiled PDFs and logs")
```

---

## 2. Key Architecture Decisions

1. **Modular Monolith over Microservices:** The entire core application resides in a single deployable ASP.NET Core process with strongly separated logical module boundaries.
2. **Direct Application Services over CQRS/MediatR:** Following explicit architectural instructions, domain orchestration uses standard C# interfaces and application services (`IAuthenticationService`, `IOrganizationService`) without mediator indirection or event sourcing.
3. **Decoupled Real-Time Architecture:** Real-time CRDT editing is offloaded to a dedicated Node.js Hocuspocus service, preventing memory-intensive CRDT states from taxing the main web process.
4. **Sandboxed LaTeX Worker:** LaTeX compilation runs in an isolated container environment with resource limits and zero outbound network access.
