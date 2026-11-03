---
name: identity-rbac-security
description: Implement Pavena authentication, platform RBAC, resource authorization, privacy, SaaS isolation, and security controls.
---

# Identity, RBAC & Security Skill

## Identity model

- One global user account per person.
- Open registration; organization affiliation is optional.
- Use ASP.NET Core Identity or a well-supported equivalent for password hashing, account lockout, reset, email confirmation, and security stamps.
- Never store plaintext passwords or reversible password encryption.
- Normalize email and username consistently.
- Require verified email before sensitive actions according to product policy.
- Provide MFA for platform administrators; strongly recommend MFA for all users.

## Role separation

### Platform roles
- Superadmin
- PlatformAdmin

These govern Pavena itself. They are not university/company roles.

### Contextual roles
- Organization membership roles: student, lecturer, teacher, researcher, employee, organization manager, etc.
- Research roles: project owner, project manager, member, supervisor, reviewer.
- Document roles: owner, editor, reviewer, commenter, viewer.

Do not put all these into one global `role` column.

## Authorization model

Use layered authorization:
1. Platform-level policy.
2. Account state and safety policy.
3. Social visibility and block policy.
4. Organization membership policy.
5. Project resource policy.
6. Document resource policy.

Use ASP.NET Core policy-based authorization and resource-based authorization handlers. Check permissions on every server request and WebSocket collaboration handshake.

## Suggested document permissions

- Owner: full control, manage collaborators, restore versions, delete/archive.
- Editor: edit content, comment, export if allowed.
- Reviewer: read, comment, suggest changes if enabled.
- Viewer: read only, export only if explicitly allowed.

Project membership does not automatically imply document access unless the document policy explicitly grants it.

## SaaS isolation

Pavena is individual-first, not organization-tenant-only. Do not force `organization_id` onto every table.

Use resource ownership and explicit relationships:
- Global/public resources.
- Personal resources.
- Organization resources.
- Project resources.
- Document resources.

A user's access must be derived from the resource's visibility, ownership, membership, explicit grants, and safety rules.

## Security controls

- HTTPS everywhere.
- Secure, HttpOnly, SameSite cookies for browser sessions where applicable.
- CSRF protection for cookie-authenticated state-changing requests.
- Rate limiting for login, registration, password reset, connection requests, messaging, and public APIs.
- Validate and encode user-generated content to prevent XSS.
- Protect against IDOR/BOLA by checking resource access, not just resource existence.
- Validate uploads by size, content type, extension, and malware scanning strategy.
- Store uploads outside the web root / in object storage.
- Use short-lived, scoped tokens for collaboration service access.
- Keep secrets in environment/secret management, never source control.
- Audit privileged actions and sensitive data access.
- Apply least privilege to database and service accounts.

## Admin protections

- MFA required for Superadmin and Platform Admin.
- Admin actions require audit records.
- Use reason codes for suspension, moderation, and sensitive changes.
- Consider dual approval for destructive or high-impact operations.
- Prevent an admin from silently escalating their own privileges.
- Superadmin account recovery must be controlled and documented.

## Privacy and user safety

- Support profile visibility controls.
- Support block and report workflows.
- Provide account export/deletion policy, subject to legal retention obligations.
- Avoid exposing private email, internal IDs, hidden affiliations, private projects, or private document metadata.
- Public profile and public publication data must be explicitly selected by the user.

## Security testing

Test unauthorized access across:
- Two unrelated users.
- Two organizations.
- Two research projects.
- Two private documents.
- Blocked users.
- Suspended accounts.
- Expired collaboration tokens.
- Admin vs non-admin routes.
