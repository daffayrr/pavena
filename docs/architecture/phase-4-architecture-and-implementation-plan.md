# Pavena Phase 4 — Architecture Design & Implementation Plan
## Real-time Research Collaboration & Industry Partnership

> **Document Version:** 1.0.0  
> **Date:** 2026-11-03  
> **Status:** Approved  
> **Branch / Commit Baseline:** `master` / `a60ab66`  
> **Baseline Verification:** 174 Passed (134 Unit Tests, 40 Integration Tests, 0 Failed, 0 Skipped)

---

## 1. Executive Summary & Business Context

Pavena is a trusted academic and industry research collaboration platform designed to bridge the gap between academic intellectual capacity and industrial innovation demands.

Phase 4 operationalizes two symbiotic collaboration pathways that converge into a shared **Research Workspace**:

- **HULU — Research-Led Collaboration:** Researchers discover and connect, establish research teams, create research projects, collaborate on research activities, produce research outputs, and present research outcomes to industry.
- **HILIR — Industry-Led Collaboration:** Companies and industries publish research challenges. Researchers discover challenges, submit proposals, form research teams, participate in evaluation, and establish research partnerships.

Both pathways converge into a shared collaboration foundation under Milestone M.

---

## 2. Stage 0 — Repository Audit Summary

- Existing architecture adheres to Modular Monolith with ASP.NET Core, EF Core, and Blazor Server.
- `IObjectStorageService` and `IMalwareScanner` are implemented and operational.
- Existing base entities `ResearchProject`, `ResearchMember`, and `ResearchTask` exist in basic forms and are ready for Phase 4 evolution.
- Multi-provider migrations support PostgreSQL as primary and MySQL as development/pilot engine.
- Zero regressions across existing Milestone B–L and Phase 3.5 functionality.

---

## 3. Implementation Sequence
- **Stage 2: Milestone M — Research Workspace & Project Management (Current)**
- **Stage 3: Milestone N Foundations (Documents & Versions) & Milestone O (Industry Challenges)**
- **Stage 4: Milestone P — Proposals, Evaluations & Challenge Conversion**
- **Stage 5: Milestone N Advanced (Locking, Concurrency & Review Comments)**
- **Stage 6: Milestone Q — Agreements, IP Declarations & Partnership Governance**
- **Stage 7: Pilot Readiness — End-to-End Scenarios, Security Audit & Docs**
