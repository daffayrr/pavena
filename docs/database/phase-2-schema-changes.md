# Phase 2 Database Schema Changes

This document details the database schema additions for Phase 2: Social Networking, Profiles, and Feed.

---

## 1. New Entities & Tables

### 1.1 `user_links`
External scholarly and professional links.
- `Id` (CHAR(36), PK)
- `UserId` (CHAR(36), FK to `AspNetUsers.Id`, ON DELETE CASCADE)
- `Title` (VARCHAR(100), NOT NULL)
- `Url` (VARCHAR(500), NOT NULL)
- `CreatedAtUtc` (DATETIME(6), NOT NULL)
- Indexes: `idx_user_links_user_id` on `UserId`

### 1.2 `posts`
Academic posts and discussions.
- `Id` (CHAR(36), PK)
- `AuthorUserId` (CHAR(36), FK to `AspNetUsers.Id`, ON DELETE CASCADE)
- `Content` (TEXT / LONGTEXT, NOT NULL)
- `Visibility` (VARCHAR(20), NOT NULL) — values: `Public`, `Connections`, `OnlyMe`
- `IsDeleted` (TINYINT(1), NOT NULL, DEFAULT 0)
- `DeletedAtUtc` (DATETIME(6), NULL)
- `CreatedAtUtc` (DATETIME(6), NOT NULL)
- `UpdatedAtUtc` (DATETIME(6), NOT NULL)
- Indexes:
  - `idx_posts_author_created` on `(AuthorUserId, CreatedAtUtc DESC)`
  - `idx_posts_visibility_created` on `(Visibility, CreatedAtUtc DESC)`
  - `idx_posts_feed_composite` on `(CreatedAtUtc DESC, Id DESC)`

---

## 2. Updated Entities & Enums

### 2.1 `PostVisibility` Enum
```csharp
public enum PostVisibility
{
    Public = 1,
    Connections = 2,
    OnlyMe = 3
}
```

### 2.2 Connection Lifecycle Enhancements
Reactivate existing row upon re-request after rejection/cancellation.

---

## 3. PostgreSQL Portability Safeguards
- No database engine-specific extensions.
- Timestamps mapped to `timestamp with time zone` (Npgsql) and `datetime(6)` (MySQL).
- String booleans and text mappings adhere to existing provider conventions.
