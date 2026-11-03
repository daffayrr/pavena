# Pavena — MySQL 8.0 Physical Schema

**Document Version:** 1.0  
**Phase:** Phase 1 Foundation Engineering  

```sql
-- =============================================================================
-- Pavena Physical DDL: MySQL 8.0 Engine Specification
-- Default Character Set: utf8mb4, Default Collation: utf8mb4_0900_ai_ci
-- =============================================================================

CREATE TABLE IF NOT EXISTS users (
    id CHAR(36) NOT NULL,
    email VARCHAR(255) NOT NULL,
    normalized_email VARCHAR(255) NOT NULL,
    username VARCHAR(50) NOT NULL,
    normalized_username VARCHAR(50) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    security_stamp VARCHAR(100) NOT NULL,
    is_email_confirmed TINYINT(1) NOT NULL DEFAULT 0,
    status VARCHAR(20) NOT NULL DEFAULT 'Active',
    two_factor_enabled TINYINT(1) NOT NULL DEFAULT 0,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NOT NULL,
    CONSTRAINT pk_users PRIMARY KEY (id),
    CONSTRAINT uq_users_normalized_email UNIQUE (normalized_email),
    CONSTRAINT uq_users_normalized_username UNIQUE (normalized_username)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS user_profiles (
    user_id CHAR(36) NOT NULL,
    display_name VARCHAR(150) NOT NULL,
    headline VARCHAR(200) NULL,
    bio TEXT NULL,
    country_code CHAR(2) NULL,
    city VARCHAR(100) NULL,
    preferred_language VARCHAR(10) NOT NULL DEFAULT 'en',
    avatar_url VARCHAR(500) NULL,
    banner_url VARCHAR(500) NULL,
    visibility VARCHAR(20) NOT NULL DEFAULT 'Public',
    updated_at_utc DATETIME(6) NOT NULL,
    CONSTRAINT pk_user_profiles PRIMARY KEY (user_id),
    CONSTRAINT fk_user_profiles_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS platform_roles (
    id INT NOT NULL,
    name VARCHAR(50) NOT NULL,
    description VARCHAR(255) NULL,
    CONSTRAINT pk_platform_roles PRIMARY KEY (id),
    CONSTRAINT uq_platform_roles_name UNIQUE (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS user_platform_roles (
    user_id CHAR(36) NOT NULL,
    role_id INT NOT NULL,
    assigned_at_utc DATETIME(6) NOT NULL,
    assigned_by_user_id CHAR(36) NULL,
    CONSTRAINT pk_user_platform_roles PRIMARY KEY (user_id, role_id),
    CONSTRAINT fk_upr_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_upr_role FOREIGN KEY (role_id) REFERENCES platform_roles (id) ON DELETE RESTRICT,
    CONSTRAINT fk_upr_assigned_by FOREIGN KEY (assigned_by_user_id) REFERENCES users (id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS organizations (
    id CHAR(36) NOT NULL,
    name VARCHAR(200) NOT NULL,
    slug VARCHAR(100) NOT NULL,
    org_type VARCHAR(50) NOT NULL,
    verification_status VARCHAR(20) NOT NULL DEFAULT 'Unverified',
    description TEXT NULL,
    website_url VARCHAR(255) NULL,
    logo_url VARCHAR(500) NULL,
    country_code CHAR(2) NULL,
    city VARCHAR(100) NULL,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NOT NULL,
    CONSTRAINT pk_organizations PRIMARY KEY (id),
    CONSTRAINT uq_organizations_slug UNIQUE (slug)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS organization_memberships (
    id CHAR(36) NOT NULL,
    organization_id CHAR(36) NOT NULL,
    user_id CHAR(36) NOT NULL,
    membership_role VARCHAR(50) NOT NULL,
    department VARCHAR(100) NULL,
    title VARCHAR(100) NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NOT NULL,
    CONSTRAINT pk_org_memberships PRIMARY KEY (id),
    CONSTRAINT uq_org_membership_user UNIQUE (organization_id, user_id),
    CONSTRAINT fk_om_org FOREIGN KEY (organization_id) REFERENCES organizations (id) ON DELETE CASCADE,
    CONSTRAINT fk_om_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS connections (
    id CHAR(36) NOT NULL,
    requester_user_id CHAR(36) NOT NULL,
    addressee_user_id CHAR(36) NOT NULL,
    canonical_user_a CHAR(36) NOT NULL,
    canonical_user_b CHAR(36) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Pending',
    requested_at_utc DATETIME(6) NOT NULL,
    responded_at_utc DATETIME(6) NULL,
    CONSTRAINT pk_connections PRIMARY KEY (id),
    CONSTRAINT uq_connections_canonical_pair UNIQUE (canonical_user_a, canonical_user_b),
    CONSTRAINT fk_conn_requester FOREIGN KEY (requester_user_id) REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_conn_addressee FOREIGN KEY (addressee_user_id) REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT chk_conn_no_self CHECK (requester_user_id <> addressee_user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS follows (
    id CHAR(36) NOT NULL,
    follower_user_id CHAR(36) NOT NULL,
    target_type VARCHAR(20) NOT NULL,
    target_id CHAR(36) NOT NULL,
    created_at_utc DATETIME(6) NOT NULL,
    CONSTRAINT pk_follows PRIMARY KEY (id),
    CONSTRAINT uq_follows_pair UNIQUE (follower_user_id, target_type, target_id),
    CONSTRAINT fk_follows_follower FOREIGN KEY (follower_user_id) REFERENCES users (id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS user_blocks (
    id CHAR(36) NOT NULL,
    blocker_user_id CHAR(36) NOT NULL,
    blocked_user_id CHAR(36) NOT NULL,
    reason VARCHAR(255) NULL,
    created_at_utc DATETIME(6) NOT NULL,
    CONSTRAINT pk_user_blocks PRIMARY KEY (id),
    CONSTRAINT uq_user_blocks_pair UNIQUE (blocker_user_id, blocked_user_id),
    CONSTRAINT fk_ub_blocker FOREIGN KEY (blocker_user_id) REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_ub_blocked FOREIGN KEY (blocked_user_id) REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT chk_user_blocks_no_self CHECK (blocker_user_id <> blocked_user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS research_projects (
    id CHAR(36) NOT NULL,
    title VARCHAR(250) NOT NULL,
    slug VARCHAR(120) NOT NULL,
    abstract TEXT NULL,
    research_field VARCHAR(100) NULL,
    owner_user_id CHAR(36) NULL,
    owner_organization_id CHAR(36) NULL,
    visibility VARCHAR(20) NOT NULL DEFAULT 'Private',
    status VARCHAR(20) NOT NULL DEFAULT 'Active',
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NOT NULL,
    CONSTRAINT pk_research_projects PRIMARY KEY (id),
    CONSTRAINT uq_research_projects_slug UNIQUE (slug),
    CONSTRAINT fk_rp_owner_user FOREIGN KEY (owner_user_id) REFERENCES users (id) ON DELETE SET NULL,
    CONSTRAINT fk_rp_owner_org FOREIGN KEY (owner_organization_id) REFERENCES organizations (id) ON DELETE SET NULL,
    CONSTRAINT chk_rp_single_owner CHECK (
        (owner_user_id IS NOT NULL AND owner_organization_id IS NULL) OR
        (owner_user_id IS NULL AND owner_organization_id IS NOT NULL)
    )
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS documents (
    id CHAR(36) NOT NULL,
    title VARCHAR(250) NOT NULL,
    editor_mode VARCHAR(20) NOT NULL,
    project_id CHAR(36) NULL,
    owner_user_id CHAR(36) NOT NULL,
    visibility VARCHAR(20) NOT NULL DEFAULT 'Private',
    status VARCHAR(20) NOT NULL DEFAULT 'Active',
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NOT NULL,
    CONSTRAINT pk_documents PRIMARY KEY (id),
    CONSTRAINT fk_doc_project FOREIGN KEY (project_id) REFERENCES research_projects (id) ON DELETE SET NULL,
    CONSTRAINT fk_doc_owner FOREIGN KEY (owner_user_id) REFERENCES users (id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS document_collaborators (
    id CHAR(36) NOT NULL,
    document_id CHAR(36) NOT NULL,
    user_id CHAR(36) NOT NULL,
    permission_role VARCHAR(20) NOT NULL,
    granted_by_user_id CHAR(36) NOT NULL,
    granted_at_utc DATETIME(6) NOT NULL,
    CONSTRAINT pk_doc_collaborators PRIMARY KEY (id),
    CONSTRAINT uq_doc_collaborators_pair UNIQUE (document_id, user_id),
    CONSTRAINT fk_dc_document FOREIGN KEY (document_id) REFERENCES documents (id) ON DELETE CASCADE,
    CONSTRAINT fk_dc_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_dc_granter FOREIGN KEY (granted_by_user_id) REFERENCES users (id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS admin_audit_logs (
    id CHAR(36) NOT NULL,
    admin_user_id CHAR(36) NOT NULL,
    action_type VARCHAR(100) NOT NULL,
    target_resource_type VARCHAR(50) NOT NULL,
    target_resource_id VARCHAR(50) NOT NULL,
    reason_code VARCHAR(50) NOT NULL,
    details_json TEXT NULL,
    ip_address VARCHAR(45) NULL,
    timestamp_utc DATETIME(6) NOT NULL,
    CONSTRAINT pk_admin_audit_logs PRIMARY KEY (id),
    CONSTRAINT fk_aal_admin FOREIGN KEY (admin_user_id) REFERENCES users (id) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
```
