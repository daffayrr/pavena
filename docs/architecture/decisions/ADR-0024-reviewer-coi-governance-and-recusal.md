# ADR-0024: Reviewer Conflict of Interest (COI) Governance, Recusal Framework, and Sponsor Decision Separation

## Status
Accepted

## Date
2026-11-04

## Context
A major vulnerability in research proposal evaluation is conflict of interest (COI). If reviewers have financial ties, supervisory relationships, direct co-investigator connections, or institutional competition with applicants, evaluation objectivity collapses. 

However, in specialized disciplines, categorical disqualification of all scholars from the same university or campus (e.g., massive multi-campus university systems like UC or University of London) could deplete the available reviewer pool. The platform must enforce strict baseline COI rules while providing configurable, auditable policies for departmental and institutional associations.

Furthermore, peer reviewer recommendations must be strictly decoupled from sponsor decisions: peer reviewers advise on scientific merits, but only authorized sponsor administrators make legally-binding contractual funding awards.

## Decision
1. **Mandatory Domain COI Rules:**
   - **Self-Review Prevention:** A reviewer cannot evaluate any proposal where they are listed as Lead Investigator or Co-Investigator.
   - **Bilateral Blocking:** Reviewers cannot be assigned to proposals where a bilateral user block exists between the reviewer and any member of the investigator team.
   - **Active Assignment Validation:** Reviewer assignment is scoped strictly to the assigned proposal; reviewers have zero read access to other competing proposals submitted to the same challenge.

2. **Configurable Institutional & Departmental Relationships:**
   - Detect shared verified organization memberships between reviewers and proposal investigators.
   - System flags institutional proximity as potential COI.
   - If institutional review policy allows same-institution evaluation (e.g. distinct departments/faculties), the reviewer must submit an explicit, auditable COI declaration confirming the absence of direct supervisory, financial, or joint-grant conflicts before gaining proposal access.

3. **Recusal & Reassignment Framework:**
   - A reviewer can self-recuse (`Recused`) at any point prior to assessment completion with a recorded rationale.
   - Sponsoring organizations can reassign or revoke assignments with full audit logging.

4. **Decoupled Sponsor Decision Authority:**
   - Reviewer assessments provide recommendations (`RecommendAccept`, `RecommendRevision`, `RecommendReject`).
   - Final decisions (`Accept`, `Reject`, `RequestRevision`) remain exclusively within the remit of authorized `OrganizationAdmin` members of the challenge sponsor.

## Consequences
- **Positive:**
  - High compliance standard satisfying institutional and commercial audit requirements.
  - Transparent audit trail protecting researchers and sponsors against bias claims.
- **Trade-off:**
  - Assignment service requires multi-table relational lookups across memberships and blocks to validate COI compliance.
