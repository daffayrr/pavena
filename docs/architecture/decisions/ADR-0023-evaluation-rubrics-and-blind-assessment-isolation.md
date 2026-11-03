# ADR-0023: Multi-Dimensional Evaluation Rubrics and Blind Peer Assessment Isolation

## Status
Accepted

## Date
2026-11-04

## Context
Evaluating applied research proposals requires standardized, multi-criteria assessment rather than subjective pass/fail voting. Reviewers need a structured framework to evaluate scientific rigor, commercial impact, feasibility, and budget justification. 

Simultaneously, evaluation integrity demands double-blind or single-blind isolation: reviewers must evaluate proposals independently without anchoring bias or collusion from seeing other reviewers' scores before their own assessment is submitted and the evaluation window closes.

## Decision
1. **Configurable Structured Evaluation Rubrics:**
   Every `ProposalAssessment` evaluates eight standard academic-industrial criteria on a calibrated 1–10 scale:
   - `ResearchSignificance`: Novelty and relevance to state-of-the-art.
   - `ScientificQuality`: Soundness of scientific principles.
   - `MethodologicalRigor`: Robustness of experimental / computational design.
   - `Feasibility`: Probability of achieving milestones within timeline.
   - `TeamExpertise`: Alignment of investigator track record with challenge domain.
   - `ExpectedImpact`: Practical and commercial utility for industry sponsor.
   - `BudgetJustification`: Cost-effectiveness and capital allocation rationale.
   - `TimelineFeasibility`: Realism of work breakdown structure.

2. **Qualitative Sections:**
   Alongside scores, reviewers must provide:
   - Written comments
   - Key strengths
   - Critical weaknesses and identified risks
   - Actionable recommendations (RecommendAccept, RecommendRevision, RecommendReject)

3. **Blind Assessment Isolation:**
   - Reviewers assigned to a proposal can only view their own draft and submitted assessment.
   - Cross-reviewer assessment visibility is blocked at both Application and Presentation layers until the sponsor officially closes the peer evaluation phase.
   - Assessment immutability: Once submitted, assessments cannot be modified without an audited reopening request approved by the sponsor administrator.

## Consequences
- **Positive:**
  - High evaluation fidelity with transparent, evidence-backed scores.
  - Mitigation of reviewer bias and premature consensus.
- **Trade-off:**
  - Requires explicit authorization checks in queries fetching assessments to ensure blind isolation.
