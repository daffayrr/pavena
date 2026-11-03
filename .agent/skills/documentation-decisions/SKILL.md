---
name: documentation-decisions
description: Maintain Pavena's architecture decision records, technical documentation, ERD, data dictionary, API contracts, and implementation handover notes.
---

# Documentation & Decisions Skill

## Documentation standards

Keep documentation close to implementation and update it with code changes.

Recommended docs:
- `docs/product/product-overview.md`
- `docs/architecture/system-context.md`
- `docs/architecture/container-diagram.md`
- `docs/architecture/module-boundaries.md`
- `docs/architecture/decisions/ADR-0001-...md`
- `docs/database/erd.md`
- `docs/database/data-dictionary.md`
- `docs/database/migration-strategy.md`
- `docs/security/rbac-matrix.md`
- `docs/security/threat-model.md`
- `docs/api/openapi-guidelines.md`
- `docs/testing/test-strategy.md`
- `docs/deployment/runbook.md`

## ADR format

Each Architecture Decision Record should include:
- ID and title.
- Status: proposed, accepted, superseded, deprecated.
- Date.
- Context/problem.
- Decision.
- Alternatives considered.
- Consequences/trade-offs.
- Migration/rollback implications.

Create ADRs for decisions that are expensive to reverse, affect multiple modules, or introduce infrastructure.

## ERD and data dictionary

For each entity/table document:
- Purpose.
- Primary key.
- Foreign keys.
- Required/optional fields.
- Unique constraints.
- Indexes.
- Lifecycle/status values.
- Ownership and authorization rules.
- Deletion/retention policy.
- Provider-specific mapping notes.

Keep ERD consistent with actual EF Core configuration and migrations.

## API documentation

Document:
- Route and HTTP method.
- Authentication requirement.
- Required permission/policy.
- Request/response schema.
- Validation rules.
- Error responses.
- Pagination/filtering.
- Idempotency behavior.
- Rate limits where relevant.

## Agent handover

At the end of a work session, provide:
- What was implemented.
- Files changed.
- Tests/build commands executed and results.
- Migrations added.
- Open issues and risks.
- Next recommended task.

Do not state that tests passed if they were not run.
