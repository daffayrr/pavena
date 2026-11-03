# ADR-0009: Private Object Storage Architecture & Signed Download Access

## Status
Accepted

## Date
2026-11-03

## Context
Pavena supports academic documents, lab datasets, LaTeX manuscripts, figures, and publication pre-prints.
Non-negotiable security requirements dictate:
1. **Zero Binary Storage in Relational Databases:** Binary file streams, large objects (BLOBs), or base64 strings must NEVER be stored in MySQL or PostgreSQL transactional tables.
2. **Private Bucket Only:** Public buckets must NEVER be enabled for scholarly attachments or research documents.
3. **Mandatory Server Authorization:** Every file upload and download must be authenticated and authorized according to post/document resource access rules before issuance of access tokens or presigned URLs.
4. **Malware & Format Integrity:** Every file upload must be validated against dangerous extensions (executable scripts, binaries) and passed through a malware inspection integration point.
5. **Short-Lived Signed Tokens:** Access to private objects is strictly mediated through short-lived presigned/signed URLs or authorized streaming proxies (e.g. 15-minute maximum validity).

## Decision
1. **Interface Definition:**
   - Define `IObjectStorageService` in `Pavena.Application.Common.Interfaces` providing:
     - `ValidateFileMetadata(string fileName, string contentType, long sizeBytes)`
     - `GenerateUploadTicketAsync(...)`
     - `GenerateDownloadUrlAsync(...)`
     - `DeleteObjectAsync(...)`
   - Define `IMalwareScanner` in `Pavena.Application.Common.Interfaces` providing:
     - `ScanAsync(Stream stream, string fileName, CancellationToken cancellationToken)`
2. **Implementation Strategy:**
   - Implement `PrivateObjectStorageService` in `Pavena.Infrastructure.Storage`.
   - Support S3/MinIO compatible object storage with private bucket configuration (`pavena-research-private`).
   - Implement a cryptographic HMAC-SHA256 presigned token scheme for development and private storage validation, ensuring 100% testability in isolated integration environments without requiring cloud network egress.
   - Implement `MalwareScanner` with standard threat signature inspection and extension blacklisting (`.exe`, `.dll`, `.bat`, `.cmd`, `.sh`, `.vbs`, `.ps1`, `.msi`).
3. **Storage Controller & Access Flow:**
   - Implement `StorageController` in `Pavena.API.Controllers`:
     - `POST /api/v1/storage/upload-ticket`: Validates academic format, size (max 25 MB), extension integrity, and issues a short-lived cryptographically signed storage ticket.
     - `GET /api/v1/posts/{postId}/attachments/{attachmentId}/download`: Enforces post read permissions via `PostAuthorizationHandler`; only authorized peers receive a short-lived signed download URL (15 minutes).
     - `DELETE /api/v1/posts/{postId}/attachments/{attachmentId}`: Enforces author ownership and triggers storage object deletion.

## Consequences
- **Positive:**
  - Complete isolation and zero data leakage for unpublished scientific artifacts and data.
  - Relational database remains lightweight, performant, and easily backed up.
  - Production-ready portability between self-hosted MinIO, AWS S3, Google Cloud Storage, or Azure Blob.
- **Trade-off:**
  - File retrieval involves an authorization check before redirecting to the signed object stream.
