# Post Publishing & Visibility Authorization

Pavena supports text-based academic discussions, scholarly updates, and scientific pre-print sharing. Posts are protected by strict visibility tiers, lifecycle states, and resource-based authorization.

---

## 1. Domain Entities & Limits

### 1.1 `Post` Entity
- `Id` (Guid, PK)
- `AuthorUserId` (Guid, FK to `User`)
- `Content` (String, Max 5,000 characters)
- `Visibility` (`Public`, `Connections`, `OnlyMe`)
- `Status` (`Draft`, `Published`, `Archived`)
- `PublishedAtUtc` (DateTime?, null for drafts)
- `EditedAtUtc` (DateTime?, recorded when content or visibility changes)
- `IsDeleted` (Boolean, soft-deletion)
- `DeletedAtUtc` (DateTime?)
- `ConcurrencyStamp` (String, optimistic concurrency token)

### 1.2 `PostAttachment` Entity
- `Id` (Guid, PK)
- `PostId` (Guid, FK to `Post`)
- `FileName` (String, Max 255 chars)
- `ContentType` (String, Max 100 chars)
- `FileSizeBytes` (Long, Max 25 MB per attachment)
- `StorageKey` (String, Max 500 chars) — Private object storage URI/key (e.g. MinIO / S3 / Azure Blob). **No raw binary data is stored within the database table.**
- `CreatedAtUtc` (DateTime)

**Limits:**
- Max attachments count: **10 files per post**.
- Max attachment size: **25 MB per file** (`26,214,400 bytes`).
- Max content length: **5,000 characters**.

---

## 2. Post Visibility Tiers

The domain defines `PostVisibility`:

1. **`Public` (1):**
   - Visible to all authenticated users.
   - Respects user block policies (hidden if author blocked viewer or viewer blocked author).
   - Ineligible if author's account is suspended.

2. **`Connections` (2):**
   - Visible **only** to the author and the author's accepted mutual connections.
   - Non-connected followers cannot view or retrieve `Connections` posts.
   - Strictly enforced on direct retrieval (`GET /posts/{id}`) and in feed generation.

3. **`OnlyMe` (3):**
   - Visible **only** to the post author.
   - Excluded from all other users' feeds and lookup queries.
   - Accessible only by Superadmin under legal/moderation compliance workflows.

---

## 3. Post Status Lifecycle

```
       [Create Draft] ──► (Draft)
                            │
                            │ Publish()
                            ▼
     [Create Published] ──► (Published) ──► Archive() ──► (Archived)
                            │
                            │ SoftDelete()
                            ▼
                         (Deleted)
```

- **`Draft`**: Visible exclusively to the author. Content can be empty during drafting. Cannot be retrieved by any other user.
- **`Published`**: Visible according to its `PostVisibility` policy. Content cannot be empty.
- **`Archived`**: Excluded from active feeds and public discovery. Only accessible by the author in their archive management view.
- **`Deleted`**: Soft-deleted (`IsDeleted = true`). Excluded from all queries. Direct requests return `404 Not Found` to prevent metadata leakage.

---

## 4. Resource-Based Authorization Handlers

The `PostAuthorizationHandler` evaluates operations:

- **`ResourceOperation.Read`:**
  - Author -> Allowed for any status (`Draft`, `Published`, `Archived`) and any visibility.
  - Soft-deleted -> Denied (returns 404).
  - Draft / Archived -> Denied for all non-authors.
  - Bilateral block active between Author and Viewer -> Denied (returns 404).
  - Visibility == Public -> Allowed for all authenticated non-blocked users.
  - Visibility == Connections -> Allowed if `IsAcceptedConnection(Author, Viewer)` is true; otherwise Denied (returns 404). Follow does NOT grant access.
  - Visibility == OnlyMe -> Denied for non-authors (returns 404).
  - Superadmin -> Allowed for moderation compliance.

- **`ResourceOperation.Update` / `Publish` / `Archive`:**
  - Author only (`ViewerId == Post.AuthorUserId`).
  - Denied if soft-deleted.

- **`ResourceOperation.Delete`:**
  - Author OR Superadmin / PlatformAdmin (platform moderation).

---

## 5. Security & Privacy Guarantees

1. **Information Hiding (Zero Leaks):** Any unauthorized attempt to view, edit, or delete a post returns `404 Not Found` rather than `403 Forbidden`, preventing attackers from determining whether a post ID exists.
2. **Author Identity Integrity:** `AuthorUserId` is derived strictly from the authenticated JWT `ClaimsPrincipal` on the server, preventing client-side identity spoofing.
3. **Database Portability:** Configured with dual-provider migrations (`MySql` and `PostgreSql`) isolated via `ProviderAwareMigrationsAssembly`.
