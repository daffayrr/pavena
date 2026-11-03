# Pavena — PostgreSQL Migration Readiness Strategy

**Document Version:** 1.0  
**Phase:** Phase 1 Foundation Engineering  

---

## 1. Portability Principles

Pavena starts persistence on MySQL 8.0 and is architected for seamless migration to PostgreSQL 16. To guarantee zero application rewrite during database migration:

1. **Provider Isolation in Composition Root:** Database configuration is injected in `Pavena.Infrastructure` and resolved in `Program.cs`. Switching providers requires changing only connection configuration and registering `UseNpgsql()` instead of `UseMySql()`.
2. **Provider-Specific Migration Assemblies:** Migrations generated for MySQL (`Migrations/MySql`) are kept separate from PostgreSQL migrations (`Migrations/PostgreSql`).
3. **Data Type Translation Matrix:**

| Logical Domain Concept | MySQL 8.0 Physical Type | PostgreSQL 16 Physical Type | EF Core Neutral Mapping |
|---|---|---|---|
| **UUID Identifier** | `CHAR(36)` | `uuid` / `CHAR(36)` | `Guid` via string or native UUID converter |
| **UTC Timestamps** | `DATETIME(6)` | `TIMESTAMP WITH TIME ZONE` | `DateTime` with Kind `Utc` |
| **Booleans** | `TINYINT(1)` | `BOOLEAN` | `bool` |
| **Binary Blobs (CRDT)** | `LONGBLOB` | `BYTEA` | `byte[]` |
| **Long Text / Bio** | `TEXT` / `LONGTEXT` | `TEXT` | `string` |
| **Enums** | `VARCHAR(30)` | `VARCHAR(30)` | `EnumToStringConverter<T>` |

---

## 2. Preventing Provider-Specific Drift

1. **No Database-Specific Triggers or Functions:** Business invariants (e.g. project single-owner rule, canonical connection keys) are implemented in C# Domain aggregate roots and verified with standard SQL `CHECK` constraints supported identically across MySQL 8.0.16+ and PostgreSQL.
2. **Case Sensitivity Handling:** Case-insensitive email and username lookups are standardized through normalized columns (`NormalizedEmail`, `NormalizedUserName`) populated uniformly by ASP.NET Core Identity.
3. **Dual Integration Testing:** In Phase 6, the test suite (`Pavena.IntegrationTests`) will run against both a MySQL test container and a PostgreSQL test container to verify exact query and schema equivalence.
