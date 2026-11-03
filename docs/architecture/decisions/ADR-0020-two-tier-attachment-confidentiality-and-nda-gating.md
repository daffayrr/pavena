# ADR-0020: Two-Tier Attachment Confidentiality & NDA Access Gating

## Status
Proposed (Milestone O Architecture Baseline)

## Date
2026-11-04

## Context
Industry research challenges often involve proprietary engineering constraints, patented background intellectual property, dataset samples, or trade secrets that cannot be publicly exposed to the general internet. However, completely hiding the challenge prevents academic researchers from discovering the collaboration opportunity.

## Decision

1. **Two-Tier Attachment Classification (`AttachmentConfidentialityLevel`):**
   - **Tier 1 (`PublicBriefing`):** Overview documents, high-level whitepapers, formatting templates, and general terms accessible to any authorized viewer of the challenge.
   - **Tier 2 (`ConfidentialNdaRequired`):** Proprietary engineering specifications, schema definitions, internal benchmark datasets, and detailed IP assignment schedules.

2. **Non-Disclosure Agreement (NDA) & Access Gate:**
   - Any researcher seeking access to Tier 2 attachments must complete a digital access agreement (NDA/Terms of Confidentiality).
   - The platform verifies that:
     1. The researcher has an active, email-verified Pavena academic profile.
     2. The researcher's institution meets the `InstitutionalEligibility` criteria of the challenge.
     3. An explicit timestamped cryptographic acknowledgement is stored in the system audit trail.
     4. If the challenge owner configured manual approval (`RequiresManualNdaApproval = true`), access remains pending until the industry representative approves the applicant.

3. **Storage Delivery Security:**
   - All attachments are stored in private object storage (`IObjectStorageService`).
   - Download URLs are short-lived (15 minutes), HMAC-SHA256 signed tokens that verify user authorization server-side before streaming bytes.
   - Direct physical storage paths are never exposed to clients.

## Consequences

### Positive
- Allows enterprises to safely publish challenging industrial problems without leaking proprietary trade secrets.
- Full legal traceability of every researcher and institution that accessed confidential materials.

### Trade-offs
- Introduces an explicit verification step for researchers before reviewing deep technical specs.
