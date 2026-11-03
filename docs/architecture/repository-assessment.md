# Pavena — Repository & Architecture Assessment

**Date:** 2026-11-03  
**Status:** Approved Architectural Baseline  
**Scope:** Phase 1 Foundation Engineering  

---

## 1. Existing Repository Structure

An exhaustive inspection of the workspace `D:\coding\_toscaflow_\pavena` reveals the following initial state:

- **Root Directory Contents:**
  - `AGENTS.md` — Operating rules and principles for AI agents.
  - `README.md` — High-level product summary and agent skill inventory.
  - `.agent/skills/` — 14 modular skill definition files governing architecture, domain modeling, persistence, security, editors, APIs, frontend, testing, and operations.
- **Source Code Status:**
  - No existing solution file (`.sln`), C# project files (`.csproj`), or source code directories (`src/`, `backend/`).
  - No existing frontend applications (`frontend/`).
  - No database migrations or SQL baseline scripts.
  - No test projects or test suites.
- **Version Control Status:**
  - The repository was not yet initialized with Git (`fatal: not a git repository`).

---

## 2. Current Technology Stack & Host Environment

A probe of the host operating environment confirms the following toolchains:

| Toolchain / Runtime | Installed Version | Architectural Role / Recommendation |
|---|---|---|
| **.NET SDK** | `10.0.302` | Used for builds and compilation. |
| **.NET Runtimes** | `8.0.19` (LTS), `10.0.10` | **Target .NET 8.0 LTS** for application runtime. .NET 8 is an enterprise Long Term Support release with guaranteed stability and full compatibility with Pomelo MySQL and Npgsql PostgreSQL providers. |
| **C# Language Version** | C# 12 | Primary development language utilizing file-scoped namespaces, primary constructors, collection expressions, and required properties. |
| **Node.js** | `v22.21.0` | Runtime for future frontend and Hocuspocus CRDT collaboration service. |
| **npm** | `11.2.0` (`npm.cmd`) | Node package manager. |
| **Git** | `2.43.0.windows.1` | Source control management. |
| **Database Engines** | MySQL 8.0 (Primary initial), PostgreSQL 16 (Target migration) | Managed via local Docker Compose containers. |
| **Object Storage** | MinIO (S3-compatible) | Managed via local Docker Compose for file and attachment storage. |

---

## 3. Identified Gaps

1. **Version Control Baseline:** Lack of `.gitignore`, `.gitattributes`, and `.editorconfig`.
2. **Solution Scaffolding:** Need for an ASP.NET Core solution structured according to clean architecture principles (`backend/Pavena.sln` or `AcademicPlatform.sln`).
3. **Container Infrastructure:** Absence of local orchestration (`docker-compose.yml`) for MySQL 8.0 and MinIO.
4. **Documentation Baseline:** Required architecture documents, domain models, physical ERD, data dictionary, and ADRs must be established.
5. **Database Migration Pipeline:** Absence of EF Core migration assemblies configured for dual-provider awareness (MySQL primary, PostgreSQL verified).

---

## 4. Technical Risks & Mitigation

| Identified Risk | Severity | Root Cause | Mitigation Strategy |
|---|:---:|---|---|
| **Premature Architectural Bloat** | High | Introducing CQRS, MediatR, or event sourcing without operational justification. | Enforce clean, direct application service interfaces (`IAuthenticationService`, `IUserProfileService`, `IOrganizationService`). No MediatR or event sourcing. |
| **Database Dialect Lock-in** | High | Using MySQL-specific features (e.g. collations, generated columns, specific JSON functions) that break when migrating to PostgreSQL. | Restrict persistence to EF Core LINQ and standard Fluent API mappings. Store UUIDs as standard 36-character strings (`CHAR(36)`), UTC timestamps (`DATETIME(6)`), and binary data as `byte[]` (`LONGBLOB` in MySQL, `BYTEA` in PostgreSQL). |
| **Role Ambiguity & Privilege Escalation** | Critical | Conflating global platform administration with institutional or workspace roles. | Separate `PlatformRole` (`Superadmin`, `PlatformAdmin`) from `OrganizationMembershipRole` and resource-level roles. Platform roles never grant access to private research workspaces without explicit resource grants. |
| **Project & Document Ownership Violations** | Critical | Concurrency anomalies leading to multiple owners or zero owners. | Enforce invariants in Domain entities and reinforce with database `CHECK` constraints (e.g., `(owner_user_id IS NOT NULL AND owner_org_id IS NULL) OR ...`). |

---

## 5. Recommended Implementation Structure

Adhering to Section 18 of the Phase 1 specification and Pavena architecture guidelines, the solution will reside under `backend/`:

```text
backend/
├── Pavena.sln
├── src/
│   ├── Pavena.Domain/                  # Core domain entities, value objects, domain invariants (zero external dependencies)
│   ├── Pavena.Application/             # Use cases, application service interfaces, DTOs, validators, exceptions
│   ├── Pavena.Infrastructure/          # EF Core DbContext, entity configurations, ASP.NET Identity, email, storage
│   ├── Pavena.API/                     # ASP.NET Core Web API (Controllers, OpenAPI/Swagger, middleware, auth handlers)
│   └── Pavena.Worker/                  # Background worker (scheduled jobs, cleanup, compilation dispatcher)
└── tests/
    ├── Pavena.UnitTests/               # Domain invariant and application service unit tests
    ├── Pavena.IntegrationTests/        # EF Core persistence and API functional tests
    └── Pavena.ArchitectureTests/       # Dependency direction and boundary enforcement tests
```

---

## 6. Compatibility & Portability Considerations

1. **MySQL EF Core Provider:** Use `Pomelo.EntityFrameworkCore.MySql` (version `8.0.2` matching .NET 8 LTS).
2. **PostgreSQL EF Core Provider:** Use `Npgsql.EntityFrameworkCore.PostgreSQL` (version `8.0.4` matching .NET 8 LTS).
3. **Identity Provider:** Use `Microsoft.AspNetCore.Identity.EntityFrameworkCore` version `8.0.x`.
4. **Testing Framework:** Use xUnit, FluentAssertions, and Microsoft.AspNetCore.Mvc.Testing.
