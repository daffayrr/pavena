# Pavena — Phase 1 Automated Test Execution Results

**Date of Execution:** 2026-11-03  
**Target Solution:** `backend/Pavena.sln` (.NET 8.0 LTS / C# 12)  
**Database Test Engine:** MySQL Community Server 8.2 (Native on localhost:3306)  
**Test Framework:** xUnit 2.9.2, FluentAssertions 6.12.0, Microsoft.AspNetCore.Mvc.Testing 8.0.11  

---

## 1. Test Execution Summary

| Test Project | Total Tests | Passed | Failed | Skipped | Execution Time |
|---|:---:|:---:|:---:|:---:|:---:|
| `Pavena.UnitTests.dll` | 27 | 27 | 0 | 0 | 1.2 s |
| `Pavena.IntegrationTests.dll` | 10 | 10 | 0 | 0 | 4.1 s |
| **Combined Solution Total** | **37** | **37** | **0** | **0** | **5.3 s** |

---

## 2. Unit Test Breakdown (`Pavena.UnitTests`)

### 2.1 Domain Invariant Tests
- `ConnectionTests.CreateRequest_WithSameUsers_ThrowsDomainException` — **PASSED**  
  *Verifies: Self-connections are strictly rejected at the domain boundary.*
- `ConnectionTests.CreateRequest_WithValidUsers_CalculatesCanonicalOrder` — **PASSED**  
  *Verifies: Direction-invariant canonical pairing `canonical_user_a < canonical_user_b` is automatically generated.*
- `ConnectionTests.Accept_WhenPending_UpdatesStatusAndTimestamp` — **PASSED**  
  *Verifies: Valid state transition from Pending to Accepted.*
- `ConnectionTests.Reject_WhenPending_UpdatesStatusAndTimestamp` — **PASSED**  
  *Verifies: Valid state transition from Pending to Rejected.*
- `ConnectionTests.Accept_WhenNotPending_ThrowsDomainException` — **PASSED**  
  *Verifies: Invalid repeated state transition rejection.*
- `ResearchProjectTests.CreatePersonal_SetsOwnerUserAndNullOrg` — **PASSED**  
  *Verifies: Personal project has non-null User ID and null Org ID.*
- `ResearchProjectTests.CreateInstitutional_SetsOwnerOrgAndNullUser` — **PASSED**  
  *Verifies: Institutional project has non-null Org ID and null User ID.*
- `ResearchProjectTests.ValidateOwnership_WithBothOwners_ThrowsDomainException` — **PASSED**  
  *Verifies: Dual-ownership anomaly rejection.*
- `ResearchProjectTests.ValidateOwnership_WithNoOwners_ThrowsDomainException` — **PASSED**  
  *Verifies: Zero-ownership anomaly rejection.*
- `DocumentTests.Create_WithValidParameters_InitializesCorrectly` — **PASSED**  
  *Verifies: Document creation initializes immutable editor mode and ownership.*
- `DocumentTests.Create_WithEmptyTitle_ThrowsDomainException` — **PASSED**  
  *Verifies: Title validation at instantiation.*
- `DocumentTests.Create_WithEmptyOwner_ThrowsDomainException` — **PASSED**  
  *Verifies: Document owner requirement.*

### 2.2 Input Validation Tests
- `RegisterRequestValidatorTests.Validate_WithValidRequest_PassesValidation` — **PASSED**  
  *Verifies: Complete scholarly profile registration validation.*
- `RegisterRequestValidatorTests.Validate_WithInvalidEmail_FailsValidation` (3 test cases) — **PASSED**  
  *Verifies: Rejection of invalid email formats, missing domains, and space injection.*
- `RegisterRequestValidatorTests.Validate_WithWeakPassword_FailsValidation` (5 test cases) — **PASSED**  
  *Verifies: OWASP-compliant complexity rules (uppercase, lowercase, number, symbol, min 8 chars).*
- `RegisterRequestValidatorTests.Validate_WithInvalidUsername_FailsValidation` (3 test cases) — **PASSED**  
  *Verifies: Rejection of short handles, whitespace, and special characters.*

### 2.3 Resource Authorization Tests
- `ResourceAuthorizationHandlerTests.DocumentHandler_WhenUserIsOwner_AuthorizesAllOperations` — **PASSED**  
  *Verifies: Document owner granted Read, Update, and ManageCollaborators.*
- `ResourceAuthorizationHandlerTests.DocumentHandler_WhenUserIsEditor_AuthorizesReadAndUpdate_DeniesManageCollaborators` — **PASSED**  
  *Verifies: Document editor granted Read and Update, but denied collaborator administration.*
- `ResourceAuthorizationHandlerTests.DocumentHandler_WhenUserIsProjectMemberOnly_DeniesPrivateDocumentAccess` — **PASSED**  
  *Verifies: Invariant 5.2 — Project membership alone never bypasses private document ACLs.*

---

## 3. Integration Test Breakdown (`Pavena.IntegrationTests`)

### 3.1 MySQL Persistence & Database Constraint Tests
- `DatabaseConstraintIntegrationTests.MySQL_Enforces_CanonicalOrderCheckConstraint` — **PASSED**  
  *Verifies: Direct MySQL `CHECK (canonical_user_a < canonical_user_b)` rejection at the database engine level.*
- `DatabaseConstraintIntegrationTests.MySQL_Enforces_SingleProjectOwnerCheckConstraint` — **PASSED**  
  *Verifies: Direct MySQL `CHECK ((owner_user_id IS NOT NULL...) OR ...)` rejection at the database engine level.*
- `DatabaseConstraintIntegrationTests.MySQL_Enforces_UniqueNormalizedEmail` — **PASSED**  
  *Verifies: Case-insensitive email uniqueness index rejection.*

### 3.2 Authentication & Security API Tests
- `AuthApiIntegrationTests.Register_WithValidData_ReturnsCreated` — **PASSED**  
  *Verifies: Complete HTTP `POST /api/v1/auth/register` flow yielding 201 Created.*
- `AuthApiIntegrationTests.Register_WithDuplicateEmail_ReturnsBadRequest` — **PASSED**  
  *Verifies: HTTP 400 Bad Request with ProblemDetails on duplicate registration attempt.*
- `AuthApiIntegrationTests.Login_WithValidCredentials_ReturnsAccessTokenAndRefreshToken` — **PASSED**  
  *Verifies: Successful authentication returning JWT Bearer token and rotating refresh token.*
- `AuthApiIntegrationTests.Login_WithConsecutiveFailedAttempts_TriggersAccountLockout` — **PASSED**  
  *Verifies: Account lockout policy triggering after 5 failed attempts, locking out subsequent requests.*
- `AuthApiIntegrationTests.AdminUsersEndpoint_Anonymous_ReturnsUnauthorized` — **PASSED**  
  *Verifies: HTTP 401 Unauthorized for unauthenticated requests to `/api/v1/admin/users`.*
- `AuthApiIntegrationTests.AdminUsersEndpoint_StandardUser_ReturnsForbidden` — **PASSED**  
  *Verifies: HTTP 403 Forbidden for authenticated standard users attempting to access platform governance.*
- `AuthApiIntegrationTests.AdminUsersEndpoint_Superadmin_ReturnsOkWithUsersList` — **PASSED**  
  *Verifies: HTTP 200 OK for Superadmin role accessing user management.*
