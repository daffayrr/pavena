# Following Rules & Asymmetric Graph

Pavena allows users to follow individuals or organizations independently of mutual connections.

---

## 1. Domain Invariants

1. **Anti-Self Follow:** A user cannot follow themselves (`FollowerUserId <> TargetId` when `TargetType == User`).
2. **Uniqueness:** A user can follow a target at most once. Enforced by unique constraint `(FollowerUserId, TargetType, TargetId)`.
3. **Independence:** Following does not grant access to private or connections-only resources.
4. **Idempotency:** Attempting to follow an already followed target or unfollow an un-followed target is handled cleanly without corruption.
5. **Severing on Block:** If User A blocks User B, both `(A follows B)` and `(B follows A)` are permanently deleted.

---

## 2. API Endpoints

- `POST /api/v1/users/{userId}/follow` — Follow a user.
- `DELETE /api/v1/users/{userId}/follow` — Unfollow a user.
- `GET /api/v1/users/{userId}/followers` — Paginated list of users following target user.
- `GET /api/v1/users/{userId}/following` — Paginated list of users/organizations target user follows.
- `GET /api/v1/users/{userId}/follow-status` — Returns `{ isFollowing: bool, isFollowedBy: bool, followersCount: int, followingCount: int }`.
