# Pavena — Agent Skills Pack

Pavena is an individual-first academic and professional social network with collaborative research workspaces and scientific writing tools.

This pack is designed for Antigravity CLI agents. It provides reusable project guidance as Markdown skills. Copy the `.agent/skills/` directory into the root of the Pavena repository if it is not already there.

## Project baseline

- Product: Pavena
- Product category: Academic Collaboration Platform / Professional Academic Network
- Audience: independent users, students, teachers, lecturers, researchers, universities, schools, and companies
- Registration: open to individuals; organization affiliation is optional
- Networking: user-to-user connections, follows, feeds, and collaboration invitations
- Editors: WYSIWYG and LaTeX
- Backend: C# / ASP.NET Core
- ORM: Entity Framework Core
- Initial database: MySQL
- Planned database: PostgreSQL
- Architecture: modular monolith for the core application
- Collaboration: Yjs-based CRDT collaboration service, with Hocuspocus as a candidate implementation
- Deployment: SaaS, initially Docker Compose
- Platform administration: Superadmin and Platform Admin only; these are not organization roles

## Included skills

| Skill | Purpose |
|---|---|
| `pavena-core` | Product context, invariants, and agent operating rules |
| `system-architecture` | Modular monolith boundaries and technical architecture |
| `domain-modeling` | Domain entities, relationships, and ownership rules |
| `database-portability` | MySQL-first design with PostgreSQL migration readiness |
| `identity-rbac-security` | Authentication, authorization, privacy, and tenant isolation |
| `social-networking` | Profiles, connections, follows, feeds, and messaging |
| `research-workspaces` | Research projects, membership, invitations, tasks, and opportunities |
| `collaborative-editor` | WYSIWYG, Yjs, document permissions, snapshots, and versioning |
| `latex-workflow` | LaTeX editing, compilation isolation, and artifact handling |
| `api-backend` | ASP.NET Core API conventions and application-layer patterns |
| `frontend-experience` | Accessible, responsive, anti-AI-slop product UI |
| `testing-quality` | Unit, integration, E2E, security, and collaboration testing |
| `devops-deployment` | Docker, configuration, observability, backup, and release practices |
| `documentation-decisions` | ADRs, data dictionary, API docs, and implementation records |

## How agents should use this pack

1. Read `pavena-core/SKILL.md` before making project-wide decisions.
2. Read the relevant focused skill before implementing a module.
3. Inspect the existing repository before creating, moving, or replacing files.
4. Preserve established conventions unless there is a documented reason to change them.
5. Do not silently introduce new infrastructure, dependencies, or architectural patterns.
6. For material architectural changes, create an Architecture Decision Record (ADR).
7. Update tests and documentation alongside implementation.

## Suggested repository layout

```text
Pavena/
├── .agent/
│   └── skills/
│       ├── pavena-core/SKILL.md
│       ├── system-architecture/SKILL.md
│       ├── domain-modeling/SKILL.md
│       ├── database-portability/SKILL.md
│       ├── identity-rbac-security/SKILL.md
│       ├── social-networking/SKILL.md
│       ├── research-workspaces/SKILL.md
│       ├── collaborative-editor/SKILL.md
│       ├── latex-workflow/SKILL.md
│       ├── api-backend/SKILL.md
│       ├── frontend-experience/SKILL.md
│       ├── testing-quality/SKILL.md
│       ├── devops-deployment/SKILL.md
│       └── documentation-decisions/SKILL.md
├── backend/
├── frontend/
├── collaboration-server/
├── database/
├── infrastructure/
├── docs/
└── tests/
```

The layout is a recommendation, not permission to restructure an existing repository automatically.
