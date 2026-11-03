# ADR-0025: Transactional & Idempotent Project Conversion with Hulu/Hilir Origin Traceability

## Status
Accepted

## Date
2026-11-04

## Context
When a sponsoring organization accepts a research proposal and funds the project, the collaboration transitions from a bidding phase into an active execution phase. According to ADR-0010, Pavena maintains a single unified `ResearchProject` aggregate root for both Hulu (curiosity-driven) and Hilir (industry-sponsored) pathways.

The conversion process must guarantee:
1. Exactly one `ResearchProject` is created per accepted proposal.
2. Concurrent duplicate conversion requests (e.g., race conditions, network retries) must be idempotent and never generate duplicate projects.
3. Complete bidirectional traceability is maintained (`ResearchProject.SourceProposalId`, `ResearchProject.SourceChallengeId`, and `ResearchProposal.ConvertedProjectId`).
4. Confidential challenge materials are not blindly exposed or copied without proper security grants.

## Decision
1. **Pre-condition Enforcement:**
   Conversion to `ResearchProject` is permitted if and only if the `ResearchProposal.Status` is `Accepted`. Unaccepted, withdrawn, or rejected proposals cannot be converted.

2. **Transactional & Idempotent Conversion Service:**
   - Execute project creation and proposal state transition inside an atomic database transaction.
   - Idempotency guard: If `proposal.ConvertedProjectId` is already populated, the service returns the existing project ID immediately without re-creating.
   - Use optimistic concurrency token (`ConcurrencyStamp`) to prevent race conditions during concurrent approval clicks.

3. **Domain Mapping & Team Membership Preservation:**
   - Invoke `ResearchProject.CreateFromProposal(title, slug, leadUserId, challengeId, proposalId, abstract, field, objectives)`.
   - Set `OriginType = ProjectOriginType.IndustryChallenge`.
   - Transfer proposal investigators into `ResearchMember` records:
     - Lead Researcher $\rightarrow$ `ProjectLead`
     - Co-Investigators $\rightarrow$ `Contributor` / `Researcher`
   - Sponsor organization representatives can be linked as institutional observers or advisors.

4. **Document & Storage Boundary Preservation:**
   - Proposal attachments remain tied to the proposal aggregate.
   - New project documents start in a clean private document workspace (via Milestone M/N capabilities) without automatic duplicate storage cloning.

## Consequences
- **Positive:**
  - Guaranteed single-project creation with zero risk of duplicate project workspaces.
  - Full traceability across the entire Hilir lifecycle from Challenge $\rightarrow$ Proposal $\rightarrow$ Project Workspace $\rightarrow$ LaTeX Collaborative Studio.
- **Trade-off:**
  - Requires transactional boundary across `ResearchProposals`, `IndustryChallenges`, and `ResearchProjects`.
