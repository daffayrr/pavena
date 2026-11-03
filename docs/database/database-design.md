# Pavena — Database Design Specification

**Document Version:** 1.0  
**Phase:** Phase 1 Foundation Engineering  

---

## 1. Database Persistence Strategy

Pavena persistence is implemented with **Entity Framework Core 8** using **MySQL 8.0** as the initial deployment target, with full migration readiness for **PostgreSQL 16**.

### 1.1 Core Persistence Tenets
1. **Explicit Mappings:** Every entity has an isolated `IEntityTypeConfiguration<T>` in `Pavena.Infrastructure.Persistence.Configurations`.
2. **Provider Neutrality:** All querying uses standard LINQ to Entities. Dialect-specific raw SQL and MySQL-specific functions (`JSON_EXTRACT`, native UUID functions) are forbidden.
3. **Data Type Standardization:**
   - **UUIDs:** Modeled in C# as `System.Guid` and stored portably as `CHAR(36)` strings. This prevents endianness inversion bugs between MySQL `BINARY(16)` and PostgreSQL native `uuid`.
   - **Timestamps:** Modeled in C# as `DateTime` with UTC values and stored in MySQL as `DATETIME(6)` (PostgreSQL `timestamp with time zone` / `timestamptz`).
   - **Binary Blobs:** Modeled in C# as `byte[]` and stored in MySQL as `LONGBLOB` (PostgreSQL `BYTEA`).
   - **Booleans:** Modeled in C# as `bool` and stored in MySQL as `TINYINT(1)` (PostgreSQL `boolean`).
   - **Enums:** Stored as `VARCHAR(30)` string representations using EF Core `EnumToStringConverter` to avoid provider enum divergence.

---

## 2. Table Indexing Strategy

To support high-concurrency queries on academic profiles and feeds:

- `users (normalized_email)` — Unique Index for login lookup.
- `users (normalized_username)` — Unique Index for profile lookup.
- `user_profiles (display_name)` — B-Tree Index for prefix search.
- `organization_memberships (user_id, organization_id)` — Composite Unique Index.
- `connections (canonical_user_a, canonical_user_b)` — Composite Unique Index for directional invariance.
- `connections (requester_user_id, status)` & `connections (addressee_user_id, status)` — Lookup indexes for pending requests.
- `follows (follower_user_id, target_type, target_id)` — Composite Unique Index.
- `research_projects (slug)` — Unique Index.
- `research_projects (owner_user_id)` & `research_projects (owner_organization_id)` — Foreign key indexes.
- `document_collaborators (document_id, user_id)` — Composite Unique Index.
- `admin_audit_logs (timestamp_utc, admin_user_id)` — Time-series ordering index.
