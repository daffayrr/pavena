# ADR-0018: Immutable Versioning, CRDT Compaction, and Disaster Recovery

## Status
Accepted

## Date
2026-11-03

## Context
Collaborative editing produces continuous micro-updates. Operational CRDT synchronization cannot serve as a substitute for human-comprehensible version history, formal submission checkpoints, or disaster recovery.

## Decision
1. **Three-Tier State Persistence:**
   - **Tier 1 (Operational CRDT Logs):** Sequential binary updates stored durably to support live reconnects.
   - **Tier 2 (Compacted Snapshots):** Periodic Yjs state snapshots generated every 100 updates or 10 minutes of inactivity to bound replay cost upon initial room join.
   - **Tier 3 (Named Versions):** User-triggered immutable milestones (`DocumentVersion`) packaging a complete source snapshot, SHA-256 checksum, description, and author metadata.
2. **Non-Destructive Restoration:**
   - Restoring a historical version creates a *new* forward revision containing the historical content, preserving the entire audit trail without historical truncation.

## Consequences
- **Positive:**
  - Fast room joining via compacted state vector.
  - Complete scientific reproducibility and audit compliance.
- **Trade-off:**
  - Requires scheduled background cleanup of obsolete incremental CRDT logs after snapshot consolidation.
