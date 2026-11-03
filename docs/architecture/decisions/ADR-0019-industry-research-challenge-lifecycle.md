# ADR-0019: Industry Research Challenge Lifecycle, Ownership, and State Machine

## Status
Proposed (Milestone O Architecture Baseline)

## Date
2026-11-04

## Context
Pavena's Hilir (industry-led) pathway connects corporate, industrial, and institutional sponsors with global academic researchers. Unlike academic-led projects (Hulu), industry challenges represent commercial problem statements, technological bottlenecks, or applied research tenders that involve corporate budgets, intellectual property expectations, and formal proposal cycles.

To ensure credibility, security, and legal accountability:
1. Challenges must not be created by arbitrary or unverified accounts.
2. Draft challenges must undergo organizational or platform governance prior to public dissemination.
3. The lifecycle must follow a deterministic state machine to govern proposal submission windows, evaluations, and archiving.

## Decision

1. **Organizational Ownership:**
   - Every `IndustryChallenge` is owned by an `Organization` (`OrganizationId`), specifically of an industry/commercial type (`OrganizationType.Company`, `ResearchInstitution`, or verified partner).
   - Only active members holding `OrgMembershipRole.OrganizationAdmin` or authorized `Employee` roles in a verified organization (`OrganizationVerificationStatus.Verified`) may draft or manage challenges.

2. **Finite State Machine (FSM):**
   The challenge lifecycle is governed by the `ChallengeStatus` enum:
   - `Draft`: Initial composition by verified industry representative. Visible only to authoring organization members.
   - `SubmittedForApproval`: Formally submitted for organizational/platform review.
   - `Approved`: Review passed; scheduled or ready for publication.
   - `Published`: Visible in the discovery feed; countdown to opening date or immediately accepting proposals.
   - `OpenForProposals`: Active window where academic researchers and consortia can submit proposals.
   - `UnderReview`: Submission deadline elapsed; proposals undergoing peer review and industry evaluation.
   - `Awarded`: One or more research proposals accepted; transitioned into `ResearchProject` instances.
   - `Closed`: Lifecycle concluded naturally without active proposal calls.
   - `Cancelled`: Terminated by challenge owner or platform administrator with mandatory auditable rationale.

3. **Immutable Auditing:**
   - Every transition emits an immutable `ChallengeActivityLog` record detailing actor, previous state, new state, timestamp, and optional justification.

## Consequences

### Positive
- Strict corporate accountability prevents spam, fraudulent tenders, or unauthorized announcements.
- Predictable proposal windows allow academic institutions and university faculties to prepare compliant research submissions.
- Seamless traceability into downstream `ResearchProject` records (`SourceChallengeId`).

### Trade-offs
- Verification prerequisite requires industry organizations to complete institutional onboarding before posting challenges.
