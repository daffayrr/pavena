# Pavena — Global Academic Research Collaboration Platform

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Modular_Monolith-blue)](https://docs.microsoft.com/aspnet/core)
[![Blazor](https://img.shields.io/badge/Frontend-Blazor_InteractiveServer-7014e8)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![EF Core Dual DB](https://img.shields.io/badge/Database-MySQL_|_PostgreSQL-00758f?logo=mysql)](https://docs.microsoft.com/ef/core)
[![SignalR](https://img.shields.io/badge/RealTime-SignalR_LaTeX_Studio-green)](https://docs.microsoft.com/aspnet/core/signalr)
[![Tests Passing](https://img.shields.io/badge/Tests-246_Passed_(100%25)-success)](#test-suite-and-verification)

Pavena is a global academic research collaboration platform designed to bridge the gap between academic research and commercial industrialization. It connects **independent researchers, university faculty, research institutes, and technology companies** across two core business pathways:

1. **HULU (Research-Led):** Academic researchers connect, form multidisciplinary teams, manage research projects, author collaborative scientific manuscripts in real time, and present innovations to industry.
2. **HILIR (Industry-Led):** Enterprises publish real-world industrial challenges with confidential attachments and digital NDAs, solicit structured research proposals, conduct peer reviews with conflict-of-interest protections, and convert accepted proposals into active collaborative projects with full audit traceability.

---

## Architecture & Technology Stack

- **Backend:** C# / .NET 8.0 ASP.NET Core Modular Monolith
- **Frontend UI:** Blazor Web App (InteractiveServer rendering) + Tailwind CSS + Monaco Editor
- **Persistence:** Entity Framework Core 8 with dual database support (MySQL 8 initial baseline, PostgreSQL 16 target production migration) via dynamic `ProviderAwareMigrationsAssembly`
- **Real-Time Collaboration:** SignalR `CollaborationHub` for LaTeX document editing rooms and keystroke synchronization
- **Object Storage:** Private object storage abstraction (`IObjectStorageService`) with HMAC-SHA256 signed upload/download tokens and malware binary scanning
- **Document Authoring:** Real-time LaTeX Research Studio with sandboxed multi-engine compilation (`pdflatex`, `xelatex`, `lualatex`) and transparent fallback PDF generation
- **Security & Authorization:** ASP.NET Core Identity with PBKDF2 hashing, lockout protection, JWT Bearer tokens, and resource-based authorization handlers ensuring that Platform Admins never receive implicit read access to confidential commercial proposals or private research documents.

---

## Quick Start (Local Workstation)

### 1. Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker & Docker Compose](https://www.docker.com/) (or local MySQL 8.0+ / PostgreSQL 15+)
- [Node.js 20+](https://nodejs.org/) (for client asset preview)

### 2. Configure Environment
Copy `.env.example` to `.env`:
```bash
cp .env.example .env
```

### 3. Start Database Containers
```bash
docker compose -f docker/docker-compose.yml up -d
```
*Starts MySQL on port 3306, PostgreSQL on port 5432, and MinIO on ports 9000/9001.*

### 4. Run Deterministic Demo Data Seeder
Populate realistic academic personas, HULU projects, HILIR challenges, proposals, reviews, and social graph:
```bash
dotnet run --project backend/src/Pavena.API -- seed-demo
```
> Universal demo password: `Demo#Pass2026`

### 5. Launch the Platform

#### Terminal 1 — Backend REST API & SignalR:
```bash
dotnet run --project backend/src/Pavena.API
```
- API listening on: `http://localhost:5181` / `https://localhost:7158`
- Interactive OpenAPI / Swagger UI: `http://localhost:5181/swagger`
- Real-time Collaboration Hub: `ws://localhost:5181/hubs/collaboration`
- Readiness Probe: `http://localhost:5181/health/ready`

#### Terminal 2 — Blazor Web Frontend:
```bash
dotnet run --project backend/src/Pavena.Web
```
- Web Application listening on: `http://localhost:5000` / `https://localhost:5001`
- Open your browser at: `http://localhost:5000`

---

## Detailed Developer Documentation

- [**docs/development/LOCAL_SETUP.md**](file:///D:/coding/_toscaflow_/pavena/docs/development/LOCAL_SETUP.md) — Comprehensive guide covering prerequisites, MySQL/PostgreSQL migration switching, port allocations, and troubleshooting.
- [**docs/development/DEMO_ACCOUNTS.md**](file:///D:/coding/_toscaflow_/pavena/docs/development/DEMO_ACCOUNTS.md) — Full persona directory with emails, roles, affiliations, and permission boundaries.
- [**docs/development/DEMO_SCENARIOS.md**](file:///D:/coding/_toscaflow_/pavena/docs/development/DEMO_SCENARIOS.md) — Step-by-step walkthroughs for HULU, HILIR, Review Rubrics, COI Enforcement, and Social Feeds.

---

## Test Suite & Verification

The solution maintains 100% test pass rate with 0 errors across 246 tests:

```bash
# Run all unit and integration tests across the solution
dotnet test backend/Pavena.sln

# Run unit tests only (190 tests)
dotnet test backend/tests/Pavena.UnitTests/Pavena.UnitTests.csproj

# Run integration tests only (56 tests)
dotnet test backend/tests/Pavena.IntegrationTests/Pavena.IntegrationTests.csproj
```

---

## Agent Guidance & Operating Rules

This repository contains reusable skills for AI coding agents located in `.agent/skills/`.

Before making architectural modifications, agents must inspect the codebase, preserve established conventions, and adhere to:
- `.agent/skills/pavena-core/SKILL.md` (Product context and operating invariants)
- `.agent/skills/identity-rbac-security/SKILL.md` (Security and confidentiality boundaries)
- `.agent/skills/database-portability/SKILL.md` (Dual MySQL/PostgreSQL compatibility)
