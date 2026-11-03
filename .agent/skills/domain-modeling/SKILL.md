---
name: domain-modeling
description: Model Pavena's entities, ownership, relationships, lifecycle rules, and domain invariants.
---

# Domain Modeling Skill

## Modeling principles

- Model business concepts, not UI screens.
- Use explicit ownership and lifecycle states.
- Keep global identity separate from organization membership and resource-specific roles.
- Prefer normalized relational structures for core business data.
- Add denormalized read models only for measured query/performance needs.
- Use UUID identifiers consistently across services and databases.
- Store timestamps in UTC; localize only for display.

## Core domains and entities

### Identity & profile
- User
- UserProfile
- UserEducation
- UserExperience
- UserSkill
- UserInterest
- ExternalIdentity (only if social/SSO login is implemented)
- AccountVerification
- UserBlock

Use ASP.NET Core Identity for credential and account security where appropriate. Do not create a parallel password system.

### Platform governance
- PlatformRole
- Permission
- RolePermission
- UserPlatformRole
- AdminAuditLog
- UserReport
- ModerationAction
- UserSanction
- PlatformSetting

Platform roles include Superadmin and Platform Admin. These roles must never be inferred from organization membership.

### Social graph
- Connection
- Follow
- Post
- PostAttachment
- PostComment
- PostReaction
- FeedVisibility (if needed)
- Message / Conversation (later if included)

Connection is a mutual relationship with a request/accept lifecycle. Follow is one-way. Block is a safety relationship and must affect discoverability and interactions.

### Organizations
- Organization
- OrganizationType
- OrganizationMembership
- OrganizationMembershipRole
- OrganizationDepartment
- OrganizationVerification
- OrganizationFollow
- OrganizationPost

A user can have zero, one, or many organization memberships. Membership may have a contextual role such as student, lecturer, researcher, employee, or organization manager.

### Research
- ResearchProject
- ResearchMember
- ResearchInvitation
- ResearchTask
- ResearchMilestone
- ResearchFile
- ResearchOpportunity (later)
- ResearchApplication (later)

A project may be owned by exactly one user or one organization. Represent this with nullable `owner_user_id` and `owner_organization_id` plus a database/application invariant that exactly one is populated. If a future design uses a common ownership abstraction, document its trade-offs first.

### Documents
- Document
- DocumentCollaborator
- DocumentComment
- DocumentSuggestion (if suggestion mode is implemented)
- DocumentSnapshot
- DocumentVersion
- DocumentUpdate
- DocumentAttachment
- DocumentActivity

A document may belong to a research project, but project membership must not automatically grant document access unless an explicit policy says so.

### References and publications
- Reference
- ReferenceAuthor
- DocumentReference
- Publication
- PublicationAuthor
- PublicationReview
- CitationStyle

Keep reference metadata distinct from publication records. DOI is useful but not every reference has one.

## Required invariants

- A user cannot connect to themselves.
- A user cannot follow themselves unless product explicitly chooses to allow it; default is disallow.
- Only one active connection pair exists for two users, regardless of request direction.
- A blocked relationship prevents prohibited interactions.
- An organization membership must reference an existing user and organization.
- A project has exactly one owner.
- A document has exactly one owner.
- A document's editor mode is immutable after creation in the MVP unless a deliberate conversion workflow is built.
- A document collaborator has one effective permission per document, with explicit upgrade/downgrade behavior.
- Public visibility never overrides legal, account, moderation, or ownership constraints.
- Publication author order must be explicitly persisted; do not infer order from join timestamps.

## Modeling workflow

1. Identify aggregate boundaries and transaction requirements.
2. Define entity identity and ownership.
3. Define lifecycle states and allowed transitions.
4. Define cardinalities and uniqueness rules.
5. Define deletion/retention behavior.
6. Define authorization rules.
7. Produce ERD and data dictionary.
8. Add migration and integration tests.

Do not use generic `status` columns with undocumented string values. Define enumerations or controlled lookup values and document transitions.
