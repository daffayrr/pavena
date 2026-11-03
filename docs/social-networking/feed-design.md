# Chronological Feed & Keyset Pagination Design

The social feed delivers academic discussions and updates to authenticated users in reverse chronological order.

---

## 1. Feed Composition Rules

For an authenticated user $U$:
A post $P$ authored by user $A$ is eligible for $U$'s feed if and only if:
1. $P.\text{IsDeleted} = \text{false}$
2. There is no active block between $U$ and $A$ ($\text{UserBlock}(U, A) = \emptyset \land \text{UserBlock}(A, U) = \emptyset$).
3. One of the following visibility conditions holds:
   - $A = U$ (The current user's own posts, regardless of visibility).
   - $P.\text{Visibility} = \text{Public}$.
   - $P.\text{Visibility} = \text{Connections} \land \text{IsAcceptedConnection}(U, A) = \text{true}$.

---

## 2. Keyset (Cursor-Based) Pagination

### 2.1 Why Keyset Pagination?
Traditional `OFFSET` pagination causes two critical failures in social feeds:
1. **Performance Degradation:** `OFFSET 10000` scans and discards 10,000 rows.
2. **Pagination Drift (Skipped / Duplicate Posts):** When new posts are inserted while a user scrolls, the window shifts, causing items to be duplicated or missed.

### 2.2 Cursor Structure
The cursor is a Base64-encoded string representing the composite keyset:
`"{CreatedAtUtcTicks}:{PostId}"`
Example:
- Plaintext: `638977824000000000:a1b2c3d4-e5f6-7890-abcd-ef0123456789`
- Encoded: `NjM4OTc3ODI0MDAwMDAwMDAwOmExYjJjM2Q0LWU1ZjYtNzg5MC1hYmNkLWVmMDEyMzQ1Njc4OQ==`

### 2.3 SQL Query Strategy
Ordering: `ORDER BY p.CreatedAtUtc DESC, p.Id DESC`

Keyset condition:
```sql
WHERE (p.CreatedAtUtc < @cursorCreatedAt)
   OR (p.CreatedAtUtc = @cursorCreatedAt AND p.Id < @cursorId)
```

The service queries `pageSize + 1` rows:
- If rows returned > `pageSize`:
  - `hasMore = true`
  - `nextCursor = Encode(rows[pageSize - 1])`
  - Result slice = `rows.Take(pageSize)`
- Else:
  - `hasMore = false`
  - `nextCursor = null`
