---
name: pavena-core
description: Core product context, non-negotiable architecture decisions, and operating rules for every agent working on Pavena.
---

# Pavena Core Skill

## Mission

Build Pavena: an individual-first academic and professional social network where people discover one another, connect, share research activity, form research teams, and co-author scientific documents.

Pavena combines:
1. Professional/academic networking.
2. Research project workspaces.
3. Collaborative scientific writing.
4. Organization presence and affiliation.
5. Publication and research discovery.

Pavena is not primarily a university information system, LMS, journal publishing platform, or organization-only intranet.

## Product principles

- **Individual-first:** every person has an independent account and profile.
- **Open registration:** institutional affiliation is optional.
- **Network-first:** users connect with users; organizations are complementary entities.
- **Collaboration-first:** research projects and documents are shared through explicit invitations and permissions.
- **Privacy by default:** private work must never become public accidentally.
- **International-ready:** support Indonesian and English interfaces, international names, time zones, and research metadata.
- **Modular monolith first:** do not introduce microservices without a measured need.
- **Portable persistence:** MySQL first, PostgreSQL planned; avoid database-specific business logic.
- **Security at the server:** frontend visibility is never an authorization boundary.

## Confirmed baseline

| Concern | Decision |
|---|---|
| Product name | Pavena |
| Delivery | Web-based SaaS |
| Users | Independent individuals and organization-affiliated users |
| Organizations | Universities, schools, companies, research institutions, other |
| Platform roles | Superadmin, Platform Admin |
| Organization roles | Separate from platform roles |
| Networking | Connections (mutual approval) and follows (one-way) |
| Editors | WYSIWYG and LaTeX |
| Backend | C# / ASP.NET Core |
| ORM | Entity Framework Core |
| Initial DB | MySQL |
| Target DB | PostgreSQL |
| Architecture | Modular monolith |
| Realtime editor | Yjs/CRDT; Hocuspocus is a candidate collaboration server |
| Deployment | SaaS; Docker Compose suitable for initial deployment |

## Non-negotiable distinctions

- Superadmin and Platform Admin are platform-level roles only.
- An organization administrator is not a platform administrator.
- Student, lecturer, teacher, researcher, and company employee are profile classifications or contextual memberships, not mutually exclusive global account roles.
- Organization affiliation is optional and a user may have multiple affiliations.
- A connection does not grant access to private documents.
- Following a person or organization does not grant access to private resources.
- Project membership and document access are separate permissions.
- WYSIWYG and LaTeX are different document modes; do not force both into one content representation.
- SignalR may support app notifications/presence, but is not by itself the CRDT collaboration engine.
- Do not execute user-supplied LaTeX in the main web process.

## MVP priorities

P0:
- Registration, login, email verification, password reset.
- User profiles.
- Connections, follows, feed, posts, comments, search.
- Organization profiles, follows, affiliations, verification workflow.
- Research projects, members, invitations, tasks.
- WYSIWYG editor and LaTeX editor foundation.
- Document collaborators, comments, autosave, version/snapshot support.
- Platform admin moderation and verification.
- Server-side authorization and audit logging.

P1:
- Notifications, publication metadata, export improvements.

P2:
- Advanced reference manager, research marketplace, advanced analytics, AI features, billing.

## Agent operating rules

Before changing code:
1. Inspect the repository, existing framework version, conventions, tests, and current implementation.
2. Identify the module and boundaries affected.
3. Read relevant skill files.
4. State assumptions when requirements are ambiguous; do not invent product behavior.
5. Prefer incremental changes over broad rewrites.

During implementation:
- Keep controllers/endpoints thin.
- Put business rules in application/domain layers.
- Keep persistence details in Infrastructure.
- Use dependency injection.
- Validate inputs server-side.
- Apply authorization to every protected resource.
- Add tests for new behavior and security boundaries.
- Avoid unnecessary dependencies.
- Never commit secrets, credentials, private keys, or production data.

After implementation:
- Run relevant tests/build/lint if available.
- Report changed files, behavior, tests run, and known limitations.
- Update docs and ADRs when decisions change.

## Definition of done

A feature is not done merely because the UI renders. It must include:
- Defined acceptance criteria.
- Server-side validation and authorization.
- Persistence behavior and error handling.
- Tests appropriate to risk.
- Accessible and responsive UI where applicable.
- Documentation for non-obvious behavior.
