# ADR-0017: Scoped Document-Level Authorization & Explicit Access Grants

## Status
Accepted

## Date
2026-11-03

## Context
In collaborative research projects, sensitive manuscripts (such as unpublished patent drafts, peer-review responses, or competitive challenge proposals) cannot be exposed indiscriminately to all project members. Project membership is a necessary prerequisite, but not a sufficient condition, for accessing private documents.

## Decision
1. **Document-Level Permissions:**
   - Every `ResearchDocument` defines a `Visibility` level:
     - `ProjectMembers`: Accessible to all active members of the containing `ResearchProject`.
     - `RestrictedGrants`: Accessible ONLY to project members with an explicit `DocumentAccessGrant`.
     - `Public`: Viewable by the scientific community once formally published.
2. **Explicit Roles & Hierarchy:**
   - Project Leads retain administrative oversight over all project documents.
   - Collaborator roles: `Owner`, `Editor`, `Reviewer`, `Viewer`.
   - Access grants support expiration dates and explicit revocation.
3. **Concealment of Unauthorized Documents:**
   - Unauthorized access attempts to restricted documents return HTTP 404 (Not Found) rather than 403 (Forbidden) to prevent document title and existence enumeration.

## Consequences
- **Positive:**
  - Fine-grained confidentiality matching academic institution and industry IP requirements.
  - Full defense-in-depth across REST endpoints, SignalR hubs, and storage downloads.
- **Trade-off:**
  - Additional query joins required during document access evaluation.
