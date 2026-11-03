# ADR-0012: Strict Reviewer Conflict-of-Interest (COI) & Proposal Confidentiality

## Status
Accepted with Enhancements

## Date
2026-11-03

## Context
Commercial industry challenges and high-value research proposals involve proprietary intellectual property and competitive advantages. Allowing unvetted peers, rival researchers, or conflicted reviewers to access confidential proposals or participate in evaluation destroys platform trust and incurs legal exposure.

## Decision
1. **Concealment by Default (HTTP 404):**
   - Unaccepted or under-review proposals are strictly hidden from general public queries and third-party researchers. Unauthorized requests receive HTTP 404 (preventing discovery of research ideas).
2. **Automated Conflict-of-Interest (COI) Rejection:**
   - The domain service prohibits reviewer assignments if:
     - The candidate reviewer is a member of the proposal's investigator team.
     - The candidate reviewer belongs to the same primary academic department/institution as the lead investigator (configurable with auditable exceptions).
     - A bilateral block relationship exists between the reviewer and any member of the proposal team.
3. **Explicit Auditable Reviewer Declarations:**
   - Before downloading proprietary proposal attachments, reviewers must submit a cryptographic confirmation affirming the absence of any undisclosed conflict of interest.
   - Any waiver or exception to institutional affiliation restrictions requires explicit written authorization recorded in the system audit log.

## Consequences
- **Positive:**
  - Robust legal and scientific compliance for industry and university partners.
  - Transparent, tamper-evident evaluation audit trail.
