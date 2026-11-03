# ADR-0004: Global Platform Roles vs. Contextual Roles

## Status
Accepted

## Date
2026-11-03

## Context
Academic platforms frequently suffer from role confusion: confusing a university administrator with a platform superadmin, or conflating an individual user's academic classification (e.g. lecturer, student) with global privileges.

## Decision
1. **Global Platform Roles:** Strictly limited to `Superadmin` and `PlatformAdmin` in `PlatformRoles` / `UserPlatformRoles`. These govern platform safety, compliance, verification, and moderation.
2. **Contextual Organizational Roles:** Stored in `OrganizationMemberships` (`OrgMembershipRole`: `Student`, `Lecturer`, `Teacher`, `Researcher`, `Employee`, `OrganizationAdmin`). An `OrganizationAdmin` manages their university/company profile only, with zero platform admin privileges.
3. **Workspace & Document Roles:** Stored in `ResearchMembers` (`ResearchRole`) and `DocumentCollaborators` (`DocumentPermissionRole`).
4. **Policy Enforcement:** Platform authorization handlers strictly check platform claims, never granting administrative rights based on organizational status.

## Consequences
- **Positive:** Eliminates privilege escalation vulnerabilities, isolates university domains, models real-world multi-institutional academic careers.
- **Trade-off:** Requires resource-based authorization handlers rather than a simple single `[Authorize(Roles = "...")]` attribute.
