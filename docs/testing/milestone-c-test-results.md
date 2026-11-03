# Milestone C Test Execution Report

- **Date:** 2026-11-03
- **Scope:** Connections, Following, Blocking Domain Logic, PostgreSQL Migration, and Resilient Transactions
- **Status:** **100% Passed (72 / 72 tests)**

---

## 1. Test Execution Summary

| Test Project | Total | Passed | Failed | Skipped | Duration |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **`Pavena.UnitTests`** | 58 | 58 | 0 | 0 | ~4s |
| **`Pavena.IntegrationTests`** | 14 | 14 | 0 | 0 | ~20s |
| **Total** | **72** | **72** | **0** | **0** | **~24s** |

---

## 2. Test Coverage Breakdown

### 2.1 Domain & State Machine Unit Tests (`Pavena.UnitTests`)
- `ConnectionTests` (13 tests):
  - Self-connection invariant rejection (`CreateRequest_WithSameUsers_ThrowsDomainException`).
  - Canonical ordering calculation (`CanonicalUserA < CanonicalUserB`).
  - Addressee acceptance and rejection transitions with UTC timestamping.
  - Actor authorization invariants: non-addressee cannot accept or reject; non-requester cannot cancel.
  - State machine lifecycle: transitions out of `Pending` prevent duplicate acceptance.
  - Re-request reactivation: resetting `Rejected` or `Cancelled` connections back to `Pending` with refreshed timestamps.
- `FollowTests` (3 tests):
  - Self-follow rejection.
  - User-to-user directional follow creation.
  - User-to-organization follow creation.
- `UserBlockTests` (2 tests):
  - Self-blocking rejection.
  - Block creation with optional reason.
- `SocialServicesTests` (18 tests):
  - ConnectionService request sending, block checking, and duplicate prevention.
  - FollowService idempotent follow/unfollow and status counts.
  - BlockService **atomic bilateral severing**: verifying that active connections and two-way follows are completely and atomically wiped out upon blocking.

### 2.2 Live Database & REST API Integration Tests (`Pavena.IntegrationTests`)
- `SocialApiIntegrationTests`:
  - `ConnectionLifecycle_Send_Accept_Status_Remove_WorksEndToEnd`: Complete lifecycle across independent authenticated HTTP clients on real database.
  - `Follow_Lifecycle_WorksCorrectly`: Follow, follower counts, following counts, and unfollow.
  - `BlockUser_AtomicallySeversConnectionAndFollowsBilateral`: Real database transaction execution under `MySqlRetryingExecutionStrategy`, verifying mutual connection severing, bilateral follow deletion, and block enforcement against future requests.
  - `MilestoneB_AcademicProfile_RemainsFullyFunctional`: Verification that profile retrieval and updating remain intact and fully operational.
- Existing Auth & Constraint Integration Tests:
  - 10 live database constraint and authentication tests continue to pass.
