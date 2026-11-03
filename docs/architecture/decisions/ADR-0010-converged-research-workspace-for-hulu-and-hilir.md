# ADR-0010: Converged Research Workspace for HULU and HILIR

## Status
Accepted

## Date
2026-11-03

## Context
Pavena addresses two distinct research collaboration lifecycles:
1. **HULU (Research-Led):** Academic researchers initiate curiosity-driven exploration, discover colleagues, form teams, author scientific documents, and pitch to industry partners.
2. **HILIR (Industry-Led):** Enterprises publish research challenges, academic teams submit proposals, peer evaluations occur, and accepted proposals transition into active research partnerships.

Separating these pathways into disparate operational models would lead to code duplication across project teams, tasks, documents, and membership governance.

## Decision
1. **Unified Aggregate Root:**
   Use a single unified `ResearchProject` aggregate root for both HULU and HILIR initiatives.
2. **Origin Classification:**
   Introduce `ProjectOriginType` enum with values:
   - `ResearcherInitiated` (HULU pathway)
   - `IndustryChallenge` (HILIR pathway)
   - `InstitutionalInitiated` (University-wide or faculty-sponsored initiatives)
3. **Traceability:**
   Support optional nullable foreign keys / identifiers:
   - `SourceChallengeId`: references the originating `IndustryChallenge`
   - `SourceProposalId`: references the accepted `ResearchProposal`
4. **Lifecycle & Research Stage Separation:**
   Distinguish operational `ProjectStatus` (`Draft`, `Planning`, `Active`, `OnHold`, `Completed`, `Archived`) from scientific maturity `ResearchStage` (`Concept`, `MethodologyDesign`, `DataCollection`, `Analysis`, `Review`, `IndustryPitchReady`, `Translation`).

## Consequences
- **Positive:**
  - Common task management, milestone tracking, document repository, and membership permissions across both pathways.
  - Seamless promotion of accepted challenge proposals into structured workspaces.
- **Trade-off:**
  - `ResearchProject` must accommodate optional originating references and industry partner memberships.
