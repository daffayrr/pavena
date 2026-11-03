# ADR-0011: Phased Collaborative Document Editing (Persistence First, CRDT Second)

## Status
Accepted

## Date
2026-11-03

## Context
Scientific and technical research writing demands unyielding data integrity, version auditability, and format fidelity (supporting both LaTeX and rich text). Introducing WebSocket-based real-time CRDT (Yjs) servers before persistence, access control, and concurrency guarantees are established creates fragile foundations and risks data loss.

## Decision
1. **Phased Delivery Strategy:**
   - **Phase 4.1 (Milestone M & N Foundation):** Prioritize durable document metadata, version history snapshots in private object storage, optimistic concurrency control (`ConcurrencyStamp`), anchored review comments, and strict server-side authorization.
   - **Phase 4.2 (Milestone N Collaboration):** Evaluate real-time collaborative editing using SignalR / Yjs CRDT only after the underlying persistence and conflict-detection models are proven with rigorous concurrency tests.
2. **Explicit Document Locking & Concurrency:**
   - Documents use `ConcurrencyStamp` to prevent silent overwrites.
   - Versions are immutable once created and point to private object storage artifacts.

## Consequences
- **Positive:**
  - Zero risk of lost scientific work or unrecoverable edit conflicts during initial workspace rollout.
  - Clear audit trail of revision snapshots.
- **Trade-off:**
  - Live simultaneous multi-cursor editing is postponed until persistence guarantees are fully tested.
