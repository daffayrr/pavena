# Pavena — Domain Invariants & Business Rules

**Document Version:** 1.0  
**Phase:** Phase 1 Foundation Engineering  

---

## 1. Identity & Profile Invariants

1. **Normalized Uniqueness:** User emails and usernames must be unique case-insensitively via normalized upper-case columns (`NormalizedEmail`, `NormalizedUserName`).
2. **One Primary Profile:** A user account has exactly one primary `UserProfile`. Profile records cannot exist without a corresponding parent `User`.
3. **Optional Affiliation:** Users are not required to specify an organization affiliation during registration or profile lifecycle.
4. **Account State Gating:** Users with `Status = Suspended` or `Status = Deactivated` are prevented from performing state-modifying actions, logging in, or participating in collaboration rooms.

---

## 2. Platform Governance Invariants

1. **Role Boundary Isolation:** `Superadmin` and `PlatformAdmin` roles are strictly platform-level. An organization administrator (`OrgMembershipRole.OrganizationAdmin`) does not possess platform admin privileges.
2. **Immutable Audit Trails:** `AdminAuditLog` records are append-only. No application service, endpoint, or platform admin can update or delete audit records.
3. **Dual Approval / Reason Code:** Sensitive platform admin actions (e.g., account suspension, organization rejection) require a structured `reason_code`.

---

## 3. Social Graph Invariants

1. **No Self-Connections:** A user cannot send a connection request to themselves:
   $$\text{RequesterUserId} \neq \text{AddresseeUserId}$$
2. **Canonical Pair Uniqueness:** Regardless of who initiates the request, only one connection record may exist between two users. This is enforced by generating canonical keys:
   $$\text{CanonicalUserA} = \min(U_1, U_2), \quad \text{CanonicalUserB} = \max(U_1, U_2)$$
   with a database unique constraint on `(CanonicalUserA, CanonicalUserB)`.
3. **One-Way Follows:** A follow relationship is one-way, idempotent, and prevents self-following.
4. **Block Safety Boundary:** If user $A$ blocks user $B$, user $B$ cannot view $A$'s profile, send connection requests, follow $A$, or view $A$'s public feed posts.

---

## 4. Research Workspaces Invariants

1. **Exclusive Single Project Ownership:** A `ResearchProject` must be owned by either an individual user OR an institutional organization, exactly one:
   $$\text{OwnerUserId IS NOT NULL} \iff \text{OwnerOrganizationId IS NULL}$$
2. **Unique Project Membership:** A user can have at most one active membership record per research project.
3. **Scoped Roles:** Research roles (`ProjectManager`, `Supervisor`, `Member`, etc.) apply strictly within the boundary of that specific project.

---

## 5. Document & Collaboration Invariants

1. **Single Document Owner:** A `Document` has exactly one `OwnerUserId`.
2. **Independent Document Authorization:** Project membership does not implicitly grant document editing or viewing permissions. Access requires explicit entry in `DocumentCollaborator` or public document visibility.
3. **Immutable Editor Mode:** Once a document is created with `EditorMode.Wysiwyg` or `EditorMode.Latex`, the editor mode cannot be altered.
4. **Single Effective Collaborator Permission:** A user possesses exactly one role in `DocumentCollaborator` per document.
