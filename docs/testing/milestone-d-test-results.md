# Milestone D Test Execution Report

- **Date:** 2026-11-03
- **Scope:** Posts, Attachments, Status Lifecycles, PostVisibility Resource-Based Authorization, Dual-Provider Migrations (PostgreSQL + MySQL)
- **Status:** **100% Passed (102 / 102 tests)**

---

## 1. Test Execution Summary

| Test Project | Total | Passed | Failed | Skipped | Duration |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **`Pavena.UnitTests`** | 80 | 80 | 0 | 0 | ~6s |
| **`Pavena.IntegrationTests`** | 22 | 22 | 0 | 0 | ~28s |
| **Total** | **102** | **102** | **0** | **0** | **~34s** |

---

## 2. Test Coverage Breakdown

### 2.1 Post Domain & Authorization Unit Tests (`Pavena.UnitTests`)
- `PostTests` (12 tests):
  - Draft post creation and initial values.
  - Empty content validation for published posts (`DomainException`).
  - Maximum content limit enforcement (5,000 characters).
  - Draft to Published transition (`Publish()`).
  - Non-author publication denial.
  - Archive transition (`Archive()`).
  - Author content and visibility updates.
  - Non-author edit denial.
  - Soft-delete status and timestamps.
  - Attachment validation: size limits (max 25 MB per attachment), count limits (max 10 attachments), required storage key and filename.
- `PostAuthorizationHandlerTests` (10 tests):
  - Author can view own post in any status (Draft, Published, Archived) and any visibility (Public, Connections, OnlyMe).
  - Non-author cannot view Draft post (Denied).
  - Non-author cannot view Archived post (Denied).
  - Non-author can view Published Public post when not blocked (Allowed).
  - Non-author cannot view Public post if author blocked viewer or viewer blocked author (Denied).
  - Non-author can view Connections post ONLY when mutual accepted connection exists (Allowed).
  - Follower who is NOT an accepted mutual connection CANNOT view Connections post (Denied).
  - Stranger cannot view Connections post (Denied).
  - Non-author cannot view OnlyMe post (Denied).
  - Non-author cannot edit post (Denied).
  - Deletion policy: Regular non-author cannot delete; Superadmin moderation can delete.

### 2.2 Live Database & REST API Integration Tests (`Pavena.IntegrationTests`)
- `PostApiIntegrationTests` (8 new tests):
  - `PostLifecycle_CreateDraft_Publish_Get_Update_Archive_Delete_WorksEndToEnd`: Complete lifecycle across real database with attachments, verification that deleted post returns `404 Not Found`.
  - `Visibility_Public_AccessibleToOtherUser`: Independent viewer can access public post.
  - `Visibility_Connections_AccessibleOnlyToAcceptedConnections_NotFollowers`:
    - Stranger gets 404.
    - Follower (unconnected) gets 404 (verification that follow does not grant connections access).
    - Connected peer gets 200 OK.
  - `Visibility_OnlyMe_AccessibleOnlyToAuthor`: Author gets 200 OK, other users get 404 Not Found.
  - `Security_IDOR_Protection_NonAuthorCannotUpdateOrDelete`: Attacker attempting to edit or delete victim's post gets 404 Not Found.
  - `Security_AuthorSpoofing_Prevention`: Client attempting to inject foreign `AuthorUserId` is ignored; post binds strictly to authenticated principal.
  - `Block_HidesPublicPost`: Blocking author immediately hides author's public posts from blocked viewer (returns 404).
  - `VisibilityChange_ImmediatelyAffectsAccess`: Changing post from Public to Connections immediately blocks unauthorized viewers without cache lag.
- Existing Milestone B (Profiles), Milestone C (Connections/Follows/Blocks), and Milestone A/Phase 1 tests (14 tests) all re-executed and passed with zero regressions.
