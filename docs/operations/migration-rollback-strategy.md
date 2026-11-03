# EF Core Database Migration & Rollback Strategy

## Purpose
This document establishes the safe execution, validation, and zero-downtime rollback protocols for database schema changes across PostgreSQL and MySQL database providers.

---

## 1. Provider-Aware Architecture

Pavena uses a dual-provider architecture mediated by `ProviderAwareMigrationsAssembly`:
- **PostgreSQL Migrations Directory:** `Pavena.Infrastructure/Persistence/Migrations/PostgreSql/`
- **MySQL Migrations Directory:** `Pavena.Infrastructure/Persistence/Migrations/MySql/`

Each provider maintains an independent, namespace-isolated history in `__EFMigrationsHistory`.

---

## 2. Standard Forward Migration Procedure

1. Verify local build and clean schema snapshot:
   ```bash
   dotnet build backend/Pavena.sln
   ```
2. For PostgreSQL (Production):
   ```bash
   dotnet ef database update \
     --project backend/src/Pavena.Infrastructure \
     --startup-project backend/src/Pavena.API \
     --connection "${POSTGRES_CONNECTION_STRING}"
   ```
3. For MySQL (Development/Pilot):
   ```bash
   dotnet ef database update \
     --project backend/src/Pavena.Infrastructure \
     --startup-project backend/src/Pavena.API \
     --connection "${MYSQL_CONNECTION_STRING}"
   ```

---

## 3. Rollback Procedure

When a migration introduces unexpected issues during deployment, execute a rollback to the designated previous migration:

### 3.1 Identify Target Previous Migration
Review migration history:
- For MySQL:
  - `20261103050018_InitialCreate`
  - `20261103065300_AddPostsAndAttachmentsMySql`
  - `20261103073500_AddPostCategoryMySql`
- For PostgreSQL:
  - `20261103050000_InitialCreatePostgreSql`
  - `20261103065000_AddPostsAndAttachmentsPostgreSql`
  - `20261103073000_AddPostCategoryPostgreSql`

### 3.2 Execute Rollback
To roll back the `AddPostCategory` migration to `AddPostsAndAttachments`:
```bash
# Rollback MySQL to AddPostsAndAttachmentsMySql
dotnet ef database update 20261103065300_AddPostsAndAttachmentsMySql \
  --project backend/src/Pavena.Infrastructure \
  --startup-project backend/src/Pavena.API

# Rollback PostgreSQL to AddPostsAndAttachmentsPostgreSql
dotnet ef database update 20261103065000_AddPostsAndAttachmentsPostgreSql \
  --project backend/src/Pavena.Infrastructure \
  --startup-project backend/src/Pavena.API
```

---

## 4. Zero-Downtime Migration Principles
1. **Never Drop Columns in Same Release:** Deprecate column in application layer first, stop writing to it, and drop it in a subsequent release.
2. **Additive Columns with Defaults:** Newly added non-nullable columns must always specify a safe database default value (e.g. `DEFAULT 'GeneralAcademic'`).
3. **Index Creation:** In production PostgreSQL, large table indexes must be created concurrently (`CREATE INDEX CONCURRENTLY`) to prevent locking table writes.
