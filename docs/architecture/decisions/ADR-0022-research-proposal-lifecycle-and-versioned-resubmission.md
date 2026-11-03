# ADR-0022: Research Proposal Aggregate, Versioned Resubmission, and Finite State Machine

## Status
Accepted

## Date
2026-11-04

## Context
In Pavena's Hilir Pathway (Industry-Led Collaboration), academic researchers and teams respond to published `IndustryChallenge` initiatives by formulating structured research proposals. Unlike informal pitch notes, an academic-industrial research proposal requires strict scientific rigor, verifiable team composition, domain classification, methodology design, detailed budget breakdowns, and milestone projections.

Furthermore, proposals are iterative: industrial sponsors often request refinements (e.g., scoping reductions, budget adjustments, or methodology clarification). Reviewers and sponsors must be able to audit prior versions alongside the current resubmission, without losing historical evaluations.

## Decision
1. **Dedicated Aggregate Root:**
   - Establish `ResearchProposal` as an independent domain aggregate root referencing exactly one source `IndustryChallenge` via `ChallengeId`.
   - A proposal does not inherit from or create a `ResearchProject` upon draft or submission. Separation of concerns is maintained until formal sponsor acceptance and contract award.

2. **Finite State Machine (FSM):**
   The proposal lifecycle strictly enforces valid transitions:
   - `Draft`: Initial composition by lead investigator and collaborators.
   - `Submitted`: Formally submitted for consideration; fields become read-only.
   - `EligibilityReview`: Automated and sponsor check against challenge constraints.
   - `UnderReview`: Assigned to independent expert peer reviewers.
   - `RevisionRequested`: Sponsor requests amendments; unlocks draft editing under incremented version.
   - `Resubmitted`: Resubmission of updated proposal with version increment.
   - `Accepted`: Formally approved by sponsoring organization for funding and execution.
   - `Rejected`: Declined by sponsoring organization with recorded rationale.
   - `Withdrawn`: Retracted by lead investigator prior to final decision.
   - `ConvertedToProject`: Promoted into an active `ResearchProject` workspace.

3. **Versioned Resubmissions:**
   - Every submission and resubmission captures an immutable `ProposalVersionSnapshot` storing the exact JSON state of the methodology, budget, and deliverables at that timestamp.
   - `SubmissionVersion` increments monotonically (v1, v2, ...), allowing side-by-side diffing and review tracking.

4. **Multi-Investigator Team Collaboration:**
   - Proposals support collaborative teams via `ProposalInvestigator` records with explicit roles (`LeadInvestigator`, `CoInvestigator`, `PostdoctoralResearcher`, `GraduateResearcher`).

## Consequences
- **Positive:**
  - Prevents illegal state transitions at the domain layer.
  - Complete historical fidelity through versioned snapshots.
  - Clear separation between challenge solicitation, proposal competition, and project execution.
- **Trade-off:**
  - Snapshot persistence introduces additional relational records, managed efficiently through structured JSON serialization.
