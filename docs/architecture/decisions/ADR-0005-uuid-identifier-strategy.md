# ADR-0005: UUID Identifier and Persistence Strategy

## Status
Accepted

## Date
2026-11-03

## Context
Entities across Pavena require globally unique identifiers to prevent enumeration attacks and support distributed creation (e.g. offline clients, sync services). MySQL and PostgreSQL handle UUIDs differently: MySQL defaults to `BINARY(16)` with differing endianness conventions, while PostgreSQL has a native `uuid` type.

## Decision
1. Represent all entity IDs in C# as `System.Guid` (UUID v4).
2. Store UUIDs portably in MySQL as `CHAR(36)` strings formatted with hyphens (`D` format, e.g. `3fa85f64-5717-4562-b3fc-2c963f66afa6`).
3. For PostgreSQL migration, EF Core can map `System.Guid` directly to native `uuid` without byte reordering issues.
4. Auto-incrementing integers are reserved solely for fixed lookup tables (`PlatformRoles.Id`).

## Consequences
- **Positive:** Immune to endianness bugs, eliminates IDOR enumeration vulnerabilities, identical string representation across REST APIs and logs.
- **Trade-off:** `CHAR(36)` occupies 36 bytes in MySQL compared to 16 bytes for binary storage, but the performance and portability guarantees vastly outweigh the storage cost for our bounded contexts.
