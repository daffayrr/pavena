# Pavena — Entity Catalog

**Document Version:** 1.0  
**Phase:** Phase 1 Foundation Engineering  

---

## 1. Identity & Profile Aggregate

### `User`
- **Responsibility:** Represents an authenticated user account within Pavena. Integrates with ASP.NET Core Identity (`IdentityUser<Guid>`).
- **Identifier:** `Guid Id` (UUID v4).
- **Core Properties:**
  - `string Email` (Required, unique normalized).
  - `string Username` (Required, unique normalized).
  - `string PasswordHash` (Required, securely hashed via ASP.NET Core Identity PBKDF2).
  - `bool IsEmailConfirmed` (Required, default `false`).
  - `UserStatus Status` (`Active`, `Suspended`, `Deactivated`).
  - `bool TwoFactorEnabled` (Required, default `false`).
  - `DateTime CreatedAtUtc`, `DateTime UpdatedAtUtc`.
- **Relationships:** 1:1 with `UserProfile`, 1:N with `OrganizationMembership`, 1:N with `UserPlatformRole`.
- **Deletion Behavior:** Soft delete or controlled GDPR pseudonymization.

### `UserProfile`
- **Responsibility:** Public and academic presence of a user.
- **Identifier:** `Guid UserId` (Foreign key to `User.Id`, primary key).
- **Core Properties:**
  - `string DisplayName` (Required, max 150 chars).
  - `string? Headline` (Optional, max 200 chars).
  - `string? Bio` (Optional, text).
  - `string? CountryCode` (Optional, 2-letter ISO).
  - `string? City` (Optional, max 100 chars).
  - `string PreferredLanguage` (Required, default `'en'`).
  - `string? AvatarUrl`, `string? BannerUrl` (Optional, MinIO storage URLs).
  - `ProfileVisibility Visibility` (`Public`, `NetworkOnly`, `Private`).
  - `DateTime UpdatedAtUtc`.

### `UserEducation`
- **Responsibility:** Represents academic degrees and institutions attended.
- **Identifier:** `Guid Id`.
- **Properties:** `Guid UserId`, `string InstitutionName`, `string? Degree`, `string? FieldOfStudy`, `int? StartYear`, `int? EndYear`.

### `UserExperience`
- **Responsibility:** Represents professional, research, or academic work appointments.
- **Identifier:** `Guid Id`.
- **Properties:** `Guid UserId`, `string OrganizationName`, `string RoleTitle`, `DateTime? StartDate`, `DateTime? EndDate`, `bool IsCurrent`.

### `UserSkill` & `UserInterest`
- **Responsibility:** Skills, tools, and scientific research topic tags.
- **Identifier:** `Guid Id`.
- **Properties:** `Guid UserId`, `string Name`.

---

## 2. Platform Governance Domain

### `PlatformRole`
- **Responsibility:** Global platform administration roles.
- **Identifier:** `int Id`.
- **Values:** `1 = Superadmin`, `2 = PlatformAdmin`.
- **Properties:** `string Name`, `string Description`.

### `UserPlatformRole`
- **Responsibility:** Junction associating users with platform administration roles.
- **Identifier:** Composite `(Guid UserId, int RoleId)`.
- **Properties:** `DateTime AssignedAtUtc`, `Guid? AssignedByUserId`.

### `AdminAuditLog`
- **Responsibility:** Immutable security audit trail of privileged administrative operations.
- **Identifier:** `Guid Id`.
- **Properties:** `Guid AdminUserId`, `string ActionType`, `string TargetResourceType`, `string TargetResourceId`, `string ReasonCode`, `string? DetailsJson`, `string? IpAddress`, `DateTime TimestampUtc`.

---

## 3. Organization Domain

### `Organization`
- **Responsibility:** Academic or commercial institutional entity.
- **Identifier:** `Guid Id`.
- **Properties:**
  - `string Name` (Required, max 200 chars).
  - `string Slug` (Required, unique, max 100 chars).
  - `OrganizationType Type` (`University`, `School`, `Company`, `ResearchInstitution`, `IndependentResearchOrganization`, `Other`).
  - `VerificationStatus Status` (`Unverified`, `PendingVerification`, `Verified`, `Rejected`).
  - `string? WebsiteUrl`, `string? LogoUrl`, `string? CountryCode`.

### `OrganizationMembership`
- **Responsibility:** Affiliation between a user and an organization.
- **Identifier:** `Guid Id`.
- **Properties:**
  - `Guid OrganizationId`, `Guid UserId`.
  - `OrgMembershipRole Role` (`Student`, `Lecturer`, `Teacher`, `Researcher`, `Employee`, `OrganizationAdmin`).
  - `string? Department`, `string? Title`, `bool IsActive`.

---

## 4. Social Graph Domain

### `Connection`
- **Responsibility:** Mutual user-to-user networking relationship.
- **Identifier:** `Guid Id`.
- **Properties:**
  - `Guid RequesterUserId`, `Guid AddresseeUserId`.
  - `Guid CanonicalUserA`, `Guid CanonicalUserB` (`min(Requester, Addressee)`, `max(...)`).
  - `ConnectionStatus Status` (`Pending`, `Accepted`, `Rejected`, `Cancelled`).
  - `DateTime RequestedAtUtc`, `DateTime? RespondedAtUtc`.

### `Follow`
- **Responsibility:** One-way follow relationship towards a user or organization.
- **Identifier:** `Guid Id`.
- **Properties:** `Guid FollowerUserId`, `FollowTargetType TargetType` (`User`, `Organization`), `Guid TargetId`.

### `UserBlock`
- **Responsibility:** Safety boundary preventing discovery and interaction between users.
- **Identifier:** `Guid Id`.
- **Properties:** `Guid BlockerUserId`, `Guid BlockedUserId`, `string? Reason`.

---

## 5. Research Workspaces Domain

### `ResearchProject`
- **Responsibility:** Collaborative research objective and workspace.
- **Identifier:** `Guid Id`.
- **Properties:**
  - `string Title`, `string Slug`, `string? Abstract`, `string? ResearchField`.
  - `Guid? OwnerUserId`, `Guid? OwnerOrganizationId`.
  - `ProjectVisibility Visibility` (`Private`, `InvitedOnly`, `PublicDiscovery`).
  - `ProjectStatus Status` (`Active`, `Completed`, `Archived`).
  - Invariant: Exactly one of `OwnerUserId` OR `OwnerOrganizationId` must be non-null.

### `ResearchMember`
- **Responsibility:** Workspace membership and contextual research role.
- **Identifier:** `Guid Id`.
- **Properties:** `Guid ProjectId`, `Guid UserId`, `ResearchRole Role` (`Owner`, `ProjectManager`, `Supervisor`, `Member`, `Reviewer`, `Observer`).

---

## 6. Document Domain

### `Document`
- **Responsibility:** Scientific document supporting collaborative authoring.
- **Identifier:** `Guid Id`.
- **Properties:**
  - `string Title`.
  - `EditorMode Mode` (`Wysiwyg`, `Latex`) — immutable after creation.
  - `Guid? ProjectId` (Optional association with research workspace).
  - `Guid OwnerUserId` (Explicit single document owner).
  - `DocumentVisibility Visibility`, `DocumentStatus Status`.

### `DocumentCollaborator`
- **Responsibility:** Explicit document authorization grant.
- **Identifier:** `Guid Id`.
- **Properties:** `Guid DocumentId`, `Guid UserId`, `DocumentPermissionRole Role` (`Owner`, `Editor`, `Reviewer`, `Viewer`).
