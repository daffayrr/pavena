# ADR-0014: CRDT-Based Real-Time Collaboration via Authenticated SignalR Transport

## Status
Accepted

## Date
2026-11-03

## Context
Scientific writing involves concurrent editing across multiple authors, research assistants, and supervisors. Traditional operational transformation (OT) requires centralized transformation servers, whereas Conflict-free Replicated Data Types (CRDT) guarantee convergence mathematically even under out-of-order delivery, latency, or offline reconnects.

## Decision
1. **CRDT Engine:**
   - Use **Yjs** as the client-side CRDT state engine bound to Monaco Editor (`y-monaco`).
   - Every client session generates a unique, cryptographically random `ClientId`.
2. **Transport Mechanism:**
   - Use **ASP.NET Core SignalR** (`/hubs/collaboration`) as the authenticated real-time transport.
   - SignalR handles authentication, authorization, hub rooms, update routing, and backpressure, but does NOT perform document text merging (delegated to Yjs).
3. **Session Lifecycle & Awareness:**
   - Server-side validation on every connect: verify JWT token, active project membership, and document access grant.
   - Real-time cursor positions, user selection ranges, and display presence are transmitted via ephemeral awareness broadcasts.
4. **Immediate Expulsion on Revocation:**
   - When a user's membership or document permission is revoked, the server immediately evicts the user from the SignalR room and terminates the connection.

## Consequences
- **Positive:**
  - Guaranteed eventual consistency without merge conflicts or lost keystrokes.
  - Native integration with ASP.NET Core authorization and existing token infrastructure.
- **Trade-off:**
  - Requires maintaining durable update logs on the server for reconnecting clients.
