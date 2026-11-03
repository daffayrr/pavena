---
name: api-backend
description: Build Pavena's ASP.NET Core backend, REST API, application services, validation, error handling, and integration contracts.
---

# API & Backend Skill

## Backend conventions

- Use the ASP.NET Core version already selected by the repository; do not upgrade major versions without an ADR.
- Keep controllers/minimal API endpoints thin.
- Put use-case orchestration in Application.
- Put business invariants in Domain/Application, not controllers.
- Keep EF Core and external service details in Infrastructure.
- Use dependency injection and strongly typed options.
- Use async APIs for I/O; pass CancellationToken.
- Avoid blocking calls and `.Result`/`.Wait()`.
- Use DTOs rather than exposing EF entities directly.
- Validate input at the API boundary and enforce domain rules again where needed.

## API conventions

- Prefix routes with `/api/v1`.
- Use consistent naming and HTTP semantics.
- Use pagination for collection endpoints.
- Use stable sorting and cursor pagination for feeds where appropriate.
- Return consistent problem details/error structures.
- Do not expose stack traces or internal database messages.
- Use idempotency for operations where retries could create duplicates.
- Apply rate limits to sensitive endpoints.

## Suggested domains

- `/auth`
- `/users`
- `/connections`
- `/follows`
- `/posts`
- `/organizations`
- `/research/projects`
- `/documents`
- `/publications`
- `/notifications`
- `/admin`

## Validation

Validate:
- UUID formats.
- String lengths.
- URL schemes.
- Enum values.
- Date ranges.
- File size and type.
- Ownership constraints.
- Cross-field rules such as exactly one project owner.

Never rely solely on client-side validation.

## Error handling

Use centralized exception handling and Problem Details. Map known domain/application failures to appropriate HTTP status codes. Log unexpected errors with correlation IDs while returning safe messages to clients.

## Persistence

- Use explicit EF Core configurations.
- Avoid lazy loading unless there is a deliberate, documented reason.
- Prevent N+1 queries.
- Use transactions for multi-entity state changes.
- Use optimistic concurrency for important editable records.
- Do not keep database transactions open during long external calls.
- Use outbox pattern when reliable event delivery is required.

## Security

Every endpoint accessing a resource must authorize the current user against that resource. Avoid IDOR/BOLA. Do not assume that knowing a UUID grants access.

## API documentation

Maintain OpenAPI documentation. Document authentication, permission requirements, request/response examples, error cases, pagination, and deprecation behavior.
