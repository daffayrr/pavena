---
name: testing-quality
description: Define and implement Pavena's unit, integration, end-to-end, security, database portability, and real-time collaboration tests.
---

# Testing & Quality Skill

## Testing pyramid

- Unit tests: domain invariants, permission policies, validation, state transitions.
- Integration tests: EF Core persistence, API endpoints, object storage adapters, identity.
- End-to-end tests: registration, networking, project creation, document collaboration workflows.
- Security tests: IDOR/BOLA, privilege escalation, tenant/resource isolation, upload validation.
- Load/resilience tests: collaboration rooms, reconnects, large documents, background workers.

## Required MVP test scenarios

### Identity
- Register, verify email, login, logout, reset password.
- Duplicate normalized email/username.
- Suspended account cannot perform protected actions.
- Admin MFA/policy enforcement.

### Social graph
- Connection request and acceptance.
- Duplicate/reverse connection prevention.
- Self-connection prevention.
- Follow/unfollow idempotency.
- Block restrictions.
- Feed respects visibility and blocks.

### Organizations
- User may have multiple memberships.
- Unaffiliated user can use core platform.
- Organization admin cannot access platform admin functions.
- Verification actions are audited.

### Research
- Exactly one project owner.
- Invitation acceptance/rejection/expiry.
- Removed member loses access.
- Unauthorized user cannot enumerate private project details.
- Task assignment permission is enforced.

### Documents
- Owner/editor/reviewer/viewer permissions.
- Project membership does not bypass document ACL.
- Unauthorized document ID returns safe denial.
- Snapshot and restore behavior.
- Export artifact access is protected.

### Collaboration
- Concurrent editing from two clients.
- Same-range concurrent edits.
- Network interruption and reconnect.
- Collaboration service restart and recovery.
- Revoked collaborator loses access.
- Expired or replayed collaboration token is rejected.
- Large update and snapshot compaction.

### LaTeX
- Valid source compiles.
- Invalid source returns safe logs.
- Timeout and memory limits enforced.
- Network access unavailable.
- Path traversal rejected.
- Shell escape restricted.
- Build artifacts are isolated by document/revision.

### Database portability
Run the same repository/application integration suite against MySQL and PostgreSQL where supported. Verify UUIDs, timestamps, decimals, unique constraints, binary data, transaction behavior, and query semantics.

## Test data

- Use synthetic test data only.
- Do not copy production personal data into development/test.
- Create reusable test builders/factories.
- Ensure tests do not depend on execution order.

## Quality gates

Before declaring a feature complete:
- Build succeeds.
- Relevant automated tests pass.
- Security cases are covered.
- No secrets are committed.
- Migrations are reviewed.
- API/UI behavior is documented.
- Known limitations are reported.
