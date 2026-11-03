# Pavena — Authorization Design Specification

**Document Version:** 1.0  
**Phase:** Phase 1 Foundation Engineering  

---

## 1. Dual-Boundary Authorization Model

Pavena enforces authorization through two distinct mechanisms:

1. **Policy-Based Authorization (`[Authorize(Policy = "...")]`):**
   - Evaluates caller identity claims and platform roles.
   - Example Policies: `RequireSuperadminRole`, `RequirePlatformAdminRole`, `RequireVerifiedEmail`.

2. **Resource-Based Authorization (`IAuthorizationService.AuthorizeAsync`):**
   - Evaluates whether the caller has rights to a specific instance of a resource.
   - Prevents Insecure Direct Object Reference (IDOR / BOLA) vulnerabilities.
   - Resource Handlers:
     - `ResearchProjectAuthorizationHandler` evaluates `ProjectRequirement` against `ResearchProject` and `ResearchMember`.
     - `DocumentAuthorizationHandler` evaluates `DocumentRequirement` against `Document` and `DocumentCollaborator`.
     - `OrganizationAuthorizationHandler` evaluates `OrganizationRequirement` against `OrganizationMembership`.

---

## 2. Server-Side Authorization Invariant

- Frontend UI visibility checks are for display ergonomics only and are **never** treated as an authorization boundary.
- Every API endpoint accessing a resource checks:
  1. Is the current user authenticated and in `Active` status?
  2. Does the requested resource exist?
  3. Does the user have explicit rights (owner, member, collaborator, or public visibility)?
- Unauthorized requests return safe HTTP `403 Forbidden` or `404 Not Found` (to avoid resource enumeration).
