# Pavena — Authentication Design Specification

**Document Version:** 1.0  
**Phase:** Phase 1 Foundation Engineering  

---

## 1. Authentication Architecture

Pavena uses **ASP.NET Core Identity** for credential management, password hashing, email verification, and security stamp validation.

```mermaid
sequenceDiagram
    autonumber
    actor User as Browser / API Client
    participant Auth as AuthController (/api/v1/auth)
    participant Svc as AuthenticationService
    participant Identity as UserManager & SignInManager
    participant DB as MySQL (users & Identity stores)

    Note over User,Auth: 1. Registration Flow
    User->>Auth: POST /register (Email, Username, Password, FullName)
    Auth->>Svc: RegisterAsync(dto)
    Svc->>Identity: CreateAsync(user, password)
    Identity->>DB: Insert into users
    Svc->>DB: Insert into user_profiles
    Svc-->>Auth: AccountCreated (UserId)
    Auth-->>User: 201 Created (Requires Email Verification)

    Note over User,Auth: 2. Authentication Flow (Browser / API)
    User->>Auth: POST /login (UsernameOrEmail, Password)
    Auth->>Svc: LoginAsync(credentials)
    Svc->>Identity: PasswordSignInAsync(user, password, lockoutOnFailure: true)
    Identity->>DB: Verify password hash & lockout count
    Svc-->>Auth: Success (Issue Auth Cookie or Scoped JWT)
    Auth-->>User: 200 OK (Set-Cookie: pavena_session; HttpOnly; SameSite=Strict; Secure)
```

---

## 2. Key Security Properties

1. **Password Policy:**
   - Minimum 8 characters.
   - Requires uppercase, lowercase, digit, and non-alphanumeric character.
   - Hashed using ASP.NET Core Identity PBKDF2 with HMAC-SHA256 and 100,000 iterations.
2. **Account Lockout:**
   - Triggered after 5 consecutive failed login attempts.
   - Lockout duration: 15 minutes.
3. **Session & Token Model:**
   - Web application sessions use secure, `HttpOnly`, `SameSite=Strict` cookies.
   - REST API clients receive short-lived access tokens with secure refresh token rotation.
4. **Information Leakage Prevention:**
   - Forgot-password and password-reset endpoints return generic success responses regardless of whether an email exists in the database.
5. **Security Stamp Invalidation:**
   - Changing a password, revoking a role, or account suspension immediately updates `security_stamp`, invalidating all active sessions across all devices.
