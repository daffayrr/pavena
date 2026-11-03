# Pavena Local Environment Setup & Developer Guide

This document provides complete instructions for bootstrapping, configuring, migrating, seeding, and running the Pavena platform in a local development environment.

---

## 1. System Requirements & Prerequisites

Ensure the following tools are installed on your host workstation:

| Component | Minimum Version | Recommended / Verified | Purpose |
| :--- | :--- | :--- | :--- |
| **.NET SDK** | `8.0.x` | `8.0` / `10.0.302 (net8.0 target)` | Core backend compilation and runtime |
| **Node.js** | `18.x` | `20.x` or `22.x` | Monaco editor and client assets |
| **Database Option A (Default)** | MySQL `8.0` | MySQL `8.0.36` or `8.2.0` | Initial persistence baseline |
| **Database Option B (Target)** | PostgreSQL `15+` | PostgreSQL `16-alpine` | Target production migration database |
| **Docker & Docker Compose** | `20.10+` | Docker Desktop / Docker Engine | Local containerized databases and MinIO |
| **LaTeX Engine (Optional)** | `pdflatex` / `xelatex` | TeX Live 2023+ or MiKTeX | Genuine PDF compilation sandbox (fallback provided if absent) |

---

## 2. Solution Structure & Runtime Port Allocation

The repository is organized as an ASP.NET Core Modular Monolith:

```
pavena/
├── backend/
│   ├── src/
│   │   ├── Pavena.Domain/               # Pure domain entities, business rules, enums (0 external dependencies)
│   │   ├── Pavena.Application/          # Application interfaces, DTOs, FluentValidation
│   │   ├── Pavena.Infrastructure/       # EF Core 8, Pomelo MySQL, Npgsql, Identity, Storage, Seeding
│   │   ├── Pavena.API/                  # ASP.NET Core REST API, SignalR Hubs, Swagger, Composition Root
│   │   └── Pavena.Web/                  # Blazor Web App InteractiveServer frontend UI
│   └── tests/
│       ├── Pavena.UnitTests/            # Unit tests (190 tests)
│       └── Pavena.IntegrationTests/     # Integration tests with real database (56 tests)
├── docker/
│   └── docker-compose.yml               # Development containers for MySQL, PostgreSQL, MinIO
└── docs/
    └── development/                     # Developer setup, demo accounts, and scenarios
```

### Network Port Allocation

| Service | Protocol | Local URL / Binding | Description |
| :--- | :--- | :--- | :--- |
| **Pavena Web App** | HTTP / HTTPS | `http://localhost:5000`<br>`https://localhost:5001` | Blazor Web App (InteractiveServer) |
| **Pavena REST API** | HTTP / HTTPS | `http://localhost:5181`<br>`https://localhost:7158` | ASP.NET Core REST API & SignalR |
| **Swagger UI** | HTTP | `http://localhost:5181/swagger` | Interactive OpenAPI documentation |
| **SignalR Collaboration** | WSS / WS | `http://localhost:5181/hubs/collaboration` | Real-time multi-user LaTeX editing |
| **Health Checks** | HTTP | `http://localhost:5181/health`<br>`http://localhost:5181/health/ready` | Liveness and database readiness probes |
| **MySQL Database** | TCP | `localhost:3306` | Default dev database (`pavena_db`) |
| **PostgreSQL Database** | TCP | `localhost:5432` | Alternative dev database (`pavena_db`) |
| **MinIO Console** | HTTP | `http://localhost:9001` | S3 Object Storage Management UI |

---

## 3. Environment Configuration

1. Copy `.env.example` to `.env` in the repository root:
   ```bash
   cp .env.example .env
   ```

2. Review the settings in `backend/src/Pavena.API/appsettings.Development.json` or `.env`.

### Switching Database Providers

Pavena features a **Dual Provider Database Engine** with dynamic migration discovery (`ProviderAwareMigrationsAssembly`):

#### To use MySQL (Default):
```json
{
  "DatabaseProvider": "MySQL",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=pavena_db;User=root;Password=abc_123;CharSet=utf8mb4;"
  }
}
```

#### To use PostgreSQL:
```json
{
  "DatabaseProvider": "PostgreSQL",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=pavena_db;Username=pavena_user;Password=pavena_secure_password;Include Error Detail=true;"
  }
}
```

---

## 4. Starting Database Infrastructure via Docker Compose

To start local MySQL, PostgreSQL, and MinIO in the background:

```bash
docker compose -f docker/docker-compose.yml up -d
```

Verify that containers are healthy:
```bash
docker compose -f docker/docker-compose.yml ps
```

---

## 5. Applying Database Migrations

The platform automatically applies pending EF Core migrations on application startup. However, you can also run migrations manually via the .NET CLI:

### For MySQL:
```bash
dotnet ef database update \
  --project backend/src/Pavena.Infrastructure \
  --startup-project backend/src/Pavena.API \
  --context PavenaDbContext
```

### For PostgreSQL:
Set `"DatabaseProvider": "PostgreSQL"` in your configuration or environment, then execute:
```bash
dotnet ef database update \
  --project backend/src/Pavena.Infrastructure \
  --startup-project backend/src/Pavena.API \
  --context PavenaDbContext
```

---

## 6. Deterministic Demo Data Seeding

Pavena includes an **idempotent, deterministic demo data seeder** that populates realistic academic and industry personas, research projects, LaTeX documents, industry challenges, proposals, reviews, and social interactions without violating domain invariants.

> [!CAUTION]
> The demo data seeder explicitly refuses to execute if `ASPNETCORE_ENVIRONMENT=Production` to prevent accidental contamination of live systems.

### Execute Seeder via CLI:
```bash
dotnet run --project backend/src/Pavena.API -- seed-demo
```

### Expected Output:
```
info: Pavena.Infrastructure.Persistence.DatabaseSeeder[0]
      Bootstrapped initial Superadmin account: superadmin@pavena.local
info: Pavena.Infrastructure.Seeding.DemoDataSeeder[0]
      Starting deterministic demo data seeding (Environment: Development)...
info: Pavena.Infrastructure.Seeding.DemoDataSeeder[0]
      Created demo user: andi.pratama@und.ac.id
info: Pavena.Infrastructure.Seeding.DemoDataSeeder[0]
      Seeded HULU ResearchProject: Autonomous Eco-Drone Swarm (eco-drone-swarm)
info: Pavena.Infrastructure.Seeding.DemoDataSeeder[0]
      Seeded collaborative LaTeX document and saved physical main.tex into object storage.
info: Pavena.Infrastructure.Seeding.DemoDataSeeder[0]
      Seeded IndustryChallenge: Advanced Perovskite-Silicon Tandem (tropical-tandem-pv-encapsulation)
info: Pavena.Infrastructure.Seeding.DemoDataSeeder[0]
      Converted Proposal A to ResearchProject ID ... with full traceability.
info: Pavena.Infrastructure.Seeding.DemoDataSeeder[0]
      Seeded Reviewer 1 assessment by Dr. Eko Wijaya (Score: 84.6%)
info: Pavena.Infrastructure.Seeding.DemoDataSeeder[0]
      Seeded Reviewer 2 assessment by Dr. Rina Dewi (Score: 88.8%)
info: Pavena.Infrastructure.Seeding.DemoDataSeeder[0]
      Demo data seeding completed successfully and idempotently.
```

The command terminates cleanly with exit code `0`. Running the command multiple times produces the identical deterministic state.

---

## 7. Running the Applications

### 1. Launch the Backend REST API
Open a terminal and run:
```bash
dotnet run --project backend/src/Pavena.API
```
- API listening on: `http://localhost:5181` / `https://localhost:7158`
- Interactive OpenAPI / Swagger UI: `http://localhost:5181/swagger`
- Real-time Collaboration Hub: `ws://localhost:5181/hubs/collaboration`
- Readiness Probe: `http://localhost:5181/health/ready`

### 2. Launch the Blazor Web App Frontend
Open a separate terminal and run:
```bash
dotnet run --project backend/src/Pavena.Web
```
- Web Application listening on: `http://localhost:5000` / `https://localhost:5001`
- Open your browser and navigate to: `http://localhost:5000`

---

## 8. Running the Automated Test Suite

Pavena maintains 100% test passing status across unit and integration tests:

```bash
# Run all solution tests (Unit + Integration)
dotnet test backend/Pavena.sln

# Run only unit tests (Domain invariants, validators, authorization)
dotnet test backend/tests/Pavena.UnitTests/Pavena.UnitTests.csproj

# Run only integration tests (Database, endpoints, real SignalR)
dotnet test backend/tests/Pavena.IntegrationTests/Pavena.IntegrationTests.csproj
```

---

## 9. Troubleshooting & FAQ

### 1. Database Connection Refused (`SocketException`)
- Verify that your local MySQL service or Docker container is active: `Get-Service MySQL*` or `docker ps`.
- Check credentials: user `root`, password `abc_123` (or container user `pavena_user`, password `pavena_secure_password`).
- Ensure port `3306` (MySQL) or `5432` (PostgreSQL) is not blocked by Windows Defender Firewall.

### 2. "Superadmin already taken" warning during boot
- This is normal and expected on subsequent boots: `DatabaseSeeder` detects that the superadmin account already exists and skips recreation without error.

### 3. LaTeX Compilation: "LaTeX compiler not found in PATH"
- The platform includes a transparent fallback PDF generator when `pdflatex` or `xelatex` is not installed on the developer machine.
- To enable native compilation, install TeX Live (`tlmgr install collection-latexrecommended`) and ensure `pdflatex` is accessible via terminal.
