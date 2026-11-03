# ADR-0002: Database Persistence and PostgreSQL Portability Strategy

## Status
Accepted

## Date
2026-11-03

## Context
Pavena is required to launch with MySQL 8.0 while maintaining full portability to migrate to PostgreSQL 16. Without strict discipline, MySQL-specific constructs (e.g. native JSON functions, specific collations, `BINARY(16)` byte order) leak into the application and complicate migration.

## Decision
1. Use **Entity Framework Core 8** as the sole persistence abstraction.
2. Initial deployment uses `Pomelo.EntityFrameworkCore.MySql` (8.0.2).
3. Target migration uses `Npgsql.EntityFrameworkCore.PostgreSQL` (8.0.4).
4. Strictly avoid raw SQL and dialect-specific functions in application logic.
5. Store UUIDs as standard `CHAR(36)` strings, timestamps as UTC `DATETIME(6)`, booleans as `TINYINT(1)` (PostgreSQL `boolean`), and binary CRDT data as `byte[]` (`LONGBLOB` in MySQL, `BYTEA` in PostgreSQL).
6. Maintain provider-specific EF Core migration directories (`Migrations/MySql` and `Migrations/PostgreSql`).

## Consequences
- **Positive:** Switching providers is achievable via configuration changes without altering domain or application logic.
- **Trade-off:** Advanced provider-specific features (such as Postgres JSONB indexing) are deferred until a dedicated migration plan is approved.
