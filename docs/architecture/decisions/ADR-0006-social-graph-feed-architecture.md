# ADR-0006: Social Graph Lifecycle, Blocking Interaction Policy, and Keyset Feed Pagination

- **Status:** Approved
- **Date:** 2026-11-03
- **Deciders:** Lead Software Architect, Application Security Engineer, Database Architect
- **Consulted Skills:** `pavena-core`, `social-networking`, `identity-rbac-security`, `api-backend`, `database-portability`

---

## 1. Context and Problem Statement

Pavena is transitioning from foundation engineering (Phase 1) to Phase 2: Social Networking, Academic Profiles, and Social Feed System.
The social graph requires rigorous rules for:
1. Mutual user-to-user connections with an unordered pair uniqueness constraint.
2. Independent, directional follow relationships.
3. User safety and blocking mechanisms that isolate hostile interactions.
4. Social posts supporting academic discourse with strictly enforced visibility levels (`Public`, `Connections`, `OnlyMe`).
5. Efficient, stable chronological social feed querying that prevents data leakage and pagination drift without premature architectural complexity (e.g. distributed caches or fan-out microservices).

---

## 2. Decision Drivers

- **Domain Integrity:** Enforce connection and follow state transitions strictly within the Domain model and Application services.
- **Race Condition & Duplicate Prevention:** Maintain canonical ordering (`CanonicalUserA < CanonicalUserB`) and unique indexes on MySQL and PostgreSQL.
- **Safety First:** Blocking must actively break existing relationships (sever connections and unfollow) and prevent any content discovery.
- **Deterministic Feed Pagination:** Keyset (cursor-based) pagination avoiding expensive `OFFSET` skips and preventing duplicate or skipped posts.
- **Zero Privacy Leaks:** Under no circumstances should non-public profile details (email, security hashes, tokens, private memberships) be exposed.

---

## 3. Considered Options

### 3.1 Connection State Transition Model
- **Option A (Physical Deletion on Severing):** Delete the connection row upon cancellation, rejection, or removal. Re-requesting simply creates a new pending row.
- **Option B (Persistent State Machine with Upsert):** Retain rows in `Rejected` or `Cancelled` status, and upon re-request update the status back to `Pending` with new requester/addressee timestamps.
- **Chosen:** **Option B with soft state transitions and clean deletion upon explicit unfriend/block.**
  - When user A requests user B: creates a row with `Pending`.
  - Recipient accepts -> `Accepted`.
  - Recipient rejects -> `Rejected`.
  - Requester cancels -> `Cancelled`.
  - If a user re-requests after previous rejection/cancellation: the existing record is updated back to `Pending` with the new requester/addressee roles and refreshed timestamp.
  - When either party explicitly removes an accepted connection (`DELETE /api/v1/connections/{userId}`): the connection record is physically deleted, returning both users to an unconnected state.
  - When either party blocks the other: the connection record is immediately deleted.

### 3.2 Blocking Interaction Severing Policy
- **Chosen Policy:** **Immediate Mutual Severing**.
  When User A blocks User B:
  1. Any existing Connection between A and B is immediately deleted.
  2. Any Follow relationships where A follows B OR B follows A are immediately deleted.
  3. Connection requests between A and B are prohibited in both directions.
  4. Follow requests/actions between A and B are prohibited in both directions.
  5. Feed queries and profile lookups strictly exclude blocked users in both directions.

### 3.3 Feed Query Strategy
- **Option A (Fan-out on Write / Push Model):** Materialize feed timelines in Redis/NoSQL on each post. (Rejected: premature complexity for MVP, violates ADR-0001).
- **Option B (Fan-out on Read / Pull Model with Keyset Pagination):** Single indexed SQL query combining candidate posts (public posts, connection posts, followed posts, own posts) filtered by block exclusions, ordered by `CreatedAtUtc DESC, Id DESC`, using cursor `(CreatedAtUtc, Id)`.
- **Chosen:** **Option B (Pull Model with Keyset Pagination)**. Direct DTO projection, `AsNoTracking()`, evaluated against relational indexes.

---

## 4. Consequences and Mitigations

- **Positive:**
  - Robust mathematical guarantees against duplicate connection records.
  - Zero possibility of ghost followers or ghost connections after blocking.
  - Feed queries remain fast and completely resilient against infinite-scroll drift when new posts are published.
- **Trade-offs / Mitigations:**
  - Database indexes must cover `(CreatedAtUtc, Id)` and `(AuthorUserId, CreatedAtUtc)`.
  - Application services must execute blocking operations within explicit database transactions to ensure atomic deletion of follows, connections, and block creation.
