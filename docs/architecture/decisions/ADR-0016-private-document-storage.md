# ADR-0016: Private Object Storage for Research Documents and Artifacts

## Status
Accepted

## Date
2026-11-03

## Context
Scientific research files, multi-megabyte source packages, figures, compilation PDFs, and raw CRDT binary state updates must be persisted securely without bloating the transactional relational database (PostgreSQL / MySQL) or risking data corruption.

## Decision
1. **Metadata vs. Payload Separation:**
   - PostgreSQL/MySQL stores relational entities, metadata, access control records, sequencing counters, and cryptographic checksums.
   - All binary payloads (source files, images, PDFs, CRDT update snapshots) are stored in private object storage using `IObjectStorageService`.
2. **Key Masking and Ephemeral Access:**
   - Storage keys (e.g. `storage/documents/{docId}/artifacts/...`) are never exposed directly to client browsers.
   - Downloads and uploads are authorized through short-lived (15 minutes), HMAC-SHA256 signed tokens generated only after verifying user permissions.

## Consequences
- **Positive:**
  - Scalable storage architecture ready for S3/MinIO cloud deployments.
  - Lean relational database footprint and faster backups.
- **Trade-off:**
  - Two-phase cleanup needed when deleting documents to avoid orphaned storage objects.
