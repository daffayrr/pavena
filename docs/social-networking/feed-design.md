# Chronological Feed & Keyset Pagination Design

The social feed delivers academic discussions and updates to authenticated users in reverse chronological order (`PublishedAtUtc DESC, Id DESC`).

---

## 1. Feed Composition Rules

### 1.1 My Network Feed (`GET /api/v1/feed`)
For an authenticated user $U$:
A post $P$ authored by user $A$ is eligible for $U$'s network feed if and only if:
1. $P.\text{Status} = \text{Published} \land P.\text{IsDeleted} = \text{false}$.
2. There is no active bilateral block between $U$ and $A$ ($\text{UserBlock}(U, A) = \emptyset \land \text{UserBlock}(A, U) = \emptyset$).
3. One of the following visibility/relation conditions holds:
   - $A = U$ (The current user's own posts, including `Public`, `Connections`, and `OnlyMe`).
   - $P.\text{Visibility} \in \{\text{Public}, \text{Connections}\} \land \text{IsAcceptedConnection}(U, A) = \text{true}$.
   - $P.\text{Visibility} = \text{Public} \land \text{IsFollowing}(U, A) = \text{true}$. (Note: Following **never** grants access to `Connections`-only posts).

### 1.2 Discover Feed (`GET /api/v1/feed/discover`)
For an authenticated user $U$:
A post $P$ authored by user $A$ is eligible for $U$'s discover feed if and only if:
1. $P.\text{Status} = \text{Published} \land P.\text{IsDeleted} = \text{false} \land P.\text{Visibility} = \text{Public}$.
2. There is no active bilateral block between $U$ and $A$.
3. Draft or Archived posts are never displayed.

---

## 2. Keyset (Cursor-Based) Pagination

### 2.1 Why Keyset Pagination?
Traditional `OFFSET` pagination causes two critical failures in high-volume social feeds:
1. **Performance Degradation:** `OFFSET 10000` scans and discards 10,000 rows.
2. **Pagination Drift (Skipped / Duplicate Posts):** When new posts are inserted while a user scrolls, the window shifts, causing items to be duplicated or missed.

### 2.2 Tamper-Proof Cursor Specification
The feed cursor is an opaque, URL-safe Base64 string containing:
- Format Version (`1`)
- UTC Timestamp in .NET Ticks (`long`)
- Post UUID (`Guid`)
- HMAC-SHA256 signature calculated over `Version|Ticks|PostId` using a secure server-side key.

Format:
`Base64Url("1|{Ticks}|{PostId:D}|{HmacHex}")`

Guarantees:
- **Tampering Prevention:** Any client-side manipulation of ticks or post ID invalidates the HMAC signature and triggers a `400 Bad Request`.
- **Deterministic ordering:** Ticks combined with UUID tie-breaker guarantees strictly monotonic pagination with no skipped or duplicated items.
- **Timezone Resilience:** Normalizes database timestamps to UTC regardless of provider kind (`Unspecified` vs `Utc`).

### 2.3 SQL Query Strategy & Indexing
Ordering: `ORDER BY p.PublishedAtUtc DESC, p.Id DESC`

Keyset condition:
```sql
WHERE (p.PublishedAtUtc < @cursorPublishedAtUtc)
   OR (p.PublishedAtUtc = @cursorPublishedAtUtc AND p.Id < @cursorId)
```

Composite Index in PostgreSQL and MySQL:
- `idx_posts_feed_composite (PublishedAtUtc, Id)`

The service queries `pageSize + 1` rows:
- If rows returned > `pageSize`:
  - `hasMore = true`
  - `nextCursor = FeedCursorCodec.Encode(rows[pageSize - 1].PublishedAtUtc, rows[pageSize - 1].Id)`
  - Result slice = `rows.Take(pageSize)`
- Else:
  - `hasMore = false`
  - `nextCursor = null`
