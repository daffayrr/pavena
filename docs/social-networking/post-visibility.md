# Post Publishing & Visibility Authorization

Pavena supports text-based academic discussions and scholarly updates. Posts are protected by strict visibility tiers.

---

## 1. Post Visibility Tiers

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

## 2. Resource-Based Authorization Handlers

The `PostAuthorizationHandler` evaluates operations:

- **`PostOperations.Read`:**
  - Author -> Allowed.
  - Superadmin -> Allowed.
  - Soft-deleted -> Denied (returns 404 / NotFound).
  - Block active between Author and Viewer -> Denied.
  - Visibility == Public -> Allowed.
  - Visibility == Connections -> Allowed if `IsAcceptedConnection(Author, Viewer)` is true; otherwise Denied.
  - Visibility == OnlyMe -> Denied for non-authors.

- **`PostOperations.Edit`:**
  - Author only (`ViewerId == Post.AuthorUserId`).
  - Blocked if soft-deleted.

- **`PostOperations.Delete`:**
  - Author OR Superadmin/PlatformAdmin (content moderation).

---

## 3. Data Integrity & Concurrency

- Posts use soft deletion (`IsDeleted`, `DeletedAtUtc`).
- Direct lookups for soft-deleted posts return `404 Not Found` to prevent metadata leakage.
- Content size: Max 5,000 characters.
