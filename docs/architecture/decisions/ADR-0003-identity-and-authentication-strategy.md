# ADR-0003: Identity and Authentication Strategy

## Status
Accepted

## Date
2026-11-03

## Context
Pavena requires secure user registration, password management, email verification, account lockout, and session handling. Custom authentication logic frequently introduces critical cryptographic vulnerabilities.

## Decision
1. Standardize on **ASP.NET Core Identity** (`IdentityUser<Guid>`).
2. Do not build a custom password hashing or reset system.
3. Use PBKDF2 with HMAC-SHA256 for password hashing.
4. Support independent individual registrations; university or institutional email is never mandatory.
5. Provide secure cookie-based authentication for browser applications and bearer tokens for API clients.
6. Enforce account lockout after 5 consecutive failed attempts.
7. Integrate security stamp validation to immediately revoke all active sessions upon password change or account suspension.

## Consequences
- **Positive:** Battle-tested security, compliance with OWASP guidelines, seamless integration with ASP.NET Core authorization policies.
- **Trade-off:** Standard Identity schema (`AspNetUsers`, etc.) is customized to cleanly relate with `UserProfile` and UUID keys.
