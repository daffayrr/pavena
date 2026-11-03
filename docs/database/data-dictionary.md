# Pavena — Data Dictionary

**Document Version:** 1.0  
**Phase:** Phase 1 Foundation Engineering  

---

## 1. Table: `users`
Represents core identity and authentication state.

| Column | Data Type | Nullable | Default | Description |
|---|---|:---:|---|---|
| `id` | `CHAR(36)` | No | — | Primary Key (UUID v4 string). |
| `email` | `VARCHAR(255)` | No | — | Registered email address. |
| `normalized_email` | `VARCHAR(255)` | No | — | Upper-case email for case-insensitive unique lookup. |
| `username` | `VARCHAR(50)` | No | — | Public username/handle. |
| `normalized_username` | `VARCHAR(50)` | No | — | Upper-case username for case-insensitive unique lookup. |
| `password_hash` | `VARCHAR(255)` | No | — | PBKDF2 secure password hash from ASP.NET Identity. |
| `security_stamp` | `VARCHAR(100)` | No | — | Security stamp invalidated upon password/role changes. |
| `is_email_confirmed` | `TINYINT(1)` | No | `0` | Email verification flag (`0` = false, `1` = true). |
| `status` | `VARCHAR(20)` | No | `'Active'` | Account status: `'Active'`, `'Suspended'`, `'Deactivated'`. |
| `two_factor_enabled` | `TINYINT(1)` | No | `0` | Two-factor authentication status flag. |
| `created_at_utc` | `DATETIME(6)` | No | — | UTC creation timestamp. |
| `updated_at_utc` | `DATETIME(6)` | No | — | UTC last update timestamp. |

---

## 2. Table: `user_profiles`
Represents public academic profile information.

| Column | Data Type | Nullable | Default | Description |
|---|---|:---:|---|---|
| `user_id` | `CHAR(36)` | No | — | PK / FK to `users.id` (ON DELETE CASCADE). |
| `display_name` | `VARCHAR(150)` | No | — | Full display name. |
| `headline` | `VARCHAR(200)` | Yes | NULL | Academic headline / position (e.g. "PhD Candidate"). |
| `bio` | `TEXT` | Yes | NULL | Scholarly bio and research focus. |
| `country_code` | `CHAR(2)` | Yes | NULL | ISO 3166-1 alpha-2 country code. |
| `city` | `VARCHAR(100)` | Yes | NULL | City or locality. |
| `preferred_language` | `VARCHAR(10)` | No | `'en'` | UI language preference (`'en'`, `'id'`). |
| `avatar_url` | `VARCHAR(500)` | Yes | NULL | MinIO / S3 object key or URL for avatar. |
| `banner_url` | `VARCHAR(500)` | Yes | NULL | MinIO / S3 object key or URL for profile banner. |
| `visibility` | `VARCHAR(20)` | No | `'Public'` | Profile visibility: `'Public'`, `'NetworkOnly'`, `'Private'`. |
| `updated_at_utc` | `DATETIME(6)` | No | — | UTC timestamp of last profile modification. |

---

## 3. Table: `platform_roles` & `user_platform_roles`
Global administrative governance.

| Table | Column | Data Type | Nullable | Description |
|---|---|---|:---:|---|
| `platform_roles` | `id` | `INT` | No | PK: `1` = Superadmin, `2` = PlatformAdmin. |
| `platform_roles` | `name` | `VARCHAR(50)` | No | Unique role name. |
| `platform_roles` | `description`| `VARCHAR(255)`| Yes | Role description and responsibilities. |
| `user_platform_roles` | `user_id` | `CHAR(36)` | No | Composite PK / FK to `users.id`. |
| `user_platform_roles` | `role_id` | `INT` | No | Composite PK / FK to `platform_roles.id`. |
| `user_platform_roles` | `assigned_at_utc` | `DATETIME(6)` | No | Assignment timestamp in UTC. |
| `user_platform_roles` | `assigned_by_user_id`| `CHAR(36)` | Yes | FK to `users.id` identifying granting admin. |

---

## 4. Table: `organizations` & `organization_memberships`
Academic/commercial institutions and user affiliations.

| Table | Column | Data Type | Nullable | Description |
|---|---|---|:---:|---|
| `organizations` | `id` | `CHAR(36)` | No | PK (UUID v4 string). |
| `organizations` | `name` | `VARCHAR(200)` | No | Full organization name. |
| `organizations` | `slug` | `VARCHAR(100)` | No | Unique URL slug. |
| `organizations` | `org_type` | `VARCHAR(50)` | No | `'University'`, `'School'`, `'Company'`, `'ResearchInstitution'`, `'Other'`. |
| `organizations` | `verification_status`| `VARCHAR(20)`| No | `'Unverified'`, `'PendingVerification'`, `'Verified'`, `'Rejected'`. |
| `organization_memberships` | `id` | `CHAR(36)` | No | PK (UUID v4 string). |
| `organization_memberships` | `organization_id`| `CHAR(36)`| No | FK to `organizations.id` (ON DELETE CASCADE). |
| `organization_memberships` | `user_id` | `CHAR(36)` | No | FK to `users.id` (ON DELETE CASCADE). |
| `organization_memberships` | `membership_role`| `VARCHAR(50)`| No | Contextual role (`'Student'`, `'Lecturer'`, `'Researcher'`, `'Employee'`, `'OrganizationAdmin'`). |
| `organization_memberships` | `is_active` | `TINYINT(1)` | No | Active affiliation status flag. |

---

## 5. Table: `research_projects`
Collaborative research workspaces.

| Column | Data Type | Nullable | Description |
|---|---|:---:|---|
| `id` | `CHAR(36)` | No | PK (UUID v4 string). |
| `title` | `VARCHAR(250)` | No | Project title. |
| `slug` | `VARCHAR(120)` | No | Unique project URL slug. |
| `owner_user_id` | `CHAR(36)` | Yes | FK to `users.id` (mutually exclusive with `owner_organization_id`). |
| `owner_organization_id` | `CHAR(36)` | Yes | FK to `organizations.id` (mutually exclusive with `owner_user_id`). |
| `visibility` | `VARCHAR(20)` | No | `'Private'`, `'InvitedOnly'`, `'PublicDiscovery'`. |
| `status` | `VARCHAR(20)` | No | `'Draft'`, `'Active'`, `'Completed'`, `'Archived'`. |

---

## 6. Table: `documents` & `document_collaborators`
Collaborative authoring documents.

| Table | Column | Data Type | Nullable | Description |
|---|---|---|:---:|---|
| `documents` | `id` | `CHAR(36)` | No | PK (UUID v4 string). |
| `documents` | `title` | `VARCHAR(250)` | No | Document title. |
| `documents` | `editor_mode` | `VARCHAR(20)` | No | Immutable mode: `'Wysiwyg'` or `'Latex'`. |
| `documents` | `owner_user_id` | `CHAR(36)` | No | FK to `users.id` (single document owner). |
| `documents` | `project_id` | `CHAR(36)` | Yes | Optional FK to `research_projects.id`. |
| `document_collaborators` | `id` | `CHAR(36)` | No | PK (UUID v4 string). |
| `document_collaborators` | `document_id` | `CHAR(36)` | No | FK to `documents.id` (ON DELETE CASCADE). |
| `document_collaborators` | `user_id` | `CHAR(36)` | No | FK to `users.id` (ON DELETE CASCADE). |
| `document_collaborators` | `permission_role`| `VARCHAR(20)`| No | Scoped permission: `'Owner'`, `'Editor'`, `'Reviewer'`, `'Viewer'`. |
