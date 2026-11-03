---
name: database-portability
description: Implement Pavena persistence with MySQL first and PostgreSQL migration readiness through EF Core.
---

# Database Portability Skill

## Goal

Pavena starts on MySQL and is intended to migrate to PostgreSQL. Keep business logic independent of provider-specific SQL and test both providers before migration.

## EF Core strategy

- Use EF Core as the primary ORM.
- Select a MySQL provider compatible with the exact EF Core version (Pomelo is a candidate).
- Use Npgsql for PostgreSQL.
- Pin compatible package versions; do not mix incompatible EF Core major versions.
- Keep provider registration in the composition root.
- Keep provider-specific migrations separate.
- Do not assume a migration generated for MySQL can run unchanged on PostgreSQL.

## Portability rules

- Prefer LINQ and EF Core APIs for ordinary queries.
- Isolate unavoidable raw SQL behind provider-aware infrastructure abstractions.
- Avoid MySQL-only functions, collations, generated-column assumptions, and implicit type conversions in business logic.
- Avoid PostgreSQL-only features until there is an ADR and a migration fallback.
- Use explicit decimal precision and string lengths.
- Define nullability and indexes intentionally.
- Use UTC timestamps consistently.
- Use stable enum storage (documented strings or numeric codes); do not rely on provider-specific enum mapping.
- Treat JSON support as a provider-specific implementation detail unless both provider mappings are tested.
- Do not assume case-insensitive unique email/username behavior is identical across providers. Define normalization and uniqueness strategy explicitly.
- Be deliberate with UUID storage and conversion. If MySQL uses `BINARY(16)`, test byte ordering and migration conversion.

## Binary data

Yjs update and snapshot data may be binary:
- MySQL: BLOB/LONGBLOB as appropriate.
- PostgreSQL: BYTEA.
Use a provider-neutral byte-array model in the application and validate EF Core mappings on both providers.

## Constraints and indexes

Create explicit constraints for:
- Normalized email and username.
- Connection pair uniqueness independent of direction.
- Follow pair uniqueness.
- Membership uniqueness.
- Project membership uniqueness.
- Document collaborator uniqueness.
- DOI uniqueness when DOI exists.
- External provider identity uniqueness.

Where a database constraint cannot express an invariant portably, enforce it transactionally in the application and test race conditions.

## Migrations

Maintain a clean migration process:
1. Model changes are reviewed.
2. MySQL migration is generated and tested.
3. PostgreSQL migration is generated and tested separately when supported.
4. Seed data is provider-neutral.
5. Migration tests verify schema and indexes.
6. Data migration scripts are repeatable or safely restartable.

## Migration readiness tests

Run integration tests against MySQL and PostgreSQL in CI when feasible. Include:
- CRUD and relationship loading.
- Transactions and concurrency.
- Unique constraint behavior.
- Date/time round-tripping.
- Decimal precision.
- UUID round-tripping.
- Binary update storage.
- Pagination and sorting.
- Search behavior and collation.

Do not claim database portability merely because EF Core is used.
