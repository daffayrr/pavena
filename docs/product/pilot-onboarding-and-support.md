# Pilot Onboarding, Feedback & Product Readiness Guide

## 1. Pilot Program Overview
Pavena Phase 3.5 provides an academic social networking and researcher collaboration baseline. This document outlines onboarding procedures, feedback channels, known product boundaries, and regulatory compliance considerations for initial pilot deployment.

---

## 2. Pilot Researcher Onboarding Steps
1. **Self-Registration:**
   - Researchers sign up via `/auth/register` with their academic email and institutional affiliation.
   - Initial verification email token is generated.
2. **Profile Completion:**
   - Researchers complete their headline, academic biography, ORCID link, education history, and current institutional memberships.
   - Profile completeness indicator guides the user toward a 100% verified academic profile.
3. **Researcher Discovery & Networking:**
   - Search colleagues via `/network/discover` by name, institution, or research discipline.
   - Initiate bilateral connections with personalized collaboration notes.
4. **Publishing & Feed Sharing:**
   - Compose research updates, findings, preprints, and academic discussions via the Post Composer.
   - Set visibility rules (`Public`, `Connections`, `OnlyMe`) to control intellectual property dissemination.
   - Upload methodology drafts and datasets securely (up to 25 MB per attachment).

---

## 3. Feedback Collection & Issue Triage Workflow
- **In-App Feedback Mechanism:**
  - Integrated feedback widget allows researchers to submit comments, usability suggestions, and bug reports.
- **Triage Matrix:**
  - **P0 (Security & Data Leakage):** Addressed within 2 hours; hotfix deployment.
  - **P1 (Collaboration Blockers / Feed Synchronization):** Addressed within 24 hours.
  - **P2 (UI Polish / Academic Workflow Ergonomics):** Groomed for subsequent sprint release.

---

## 4. Known Boundaries & Limitations (Phase 3.5)
1. **Real-time Collaboration Engine (Yjs/WebSocket):** Deferred to Phase 4 (Milestone M / Real-time Collaborative Editor). Phase 3.5 focuses on social networking, feeds, asynchronous posts, and private storage.
2. **Full LaTeX PDF Cloud Compilation Pipeline:** Compilation microservice sandbox is scheduled for Phase 4. Currently, LaTeX source files are stored securely as research attachments.
3. **Federated OAuth / Institutional SAML / EduGAIN:** Direct integration scheduled for post-pilot enterprise rollout; current authentication operates via secure ASP.NET Core Identity with JWT and lockout policies.

---

## 5. Privacy Policy & Regulatory Readiness Checklist
- [x] **User Consent & Registration Terms:** Explicit acceptance required at registration.
- [x] **Data Minimization:** No personal data collected beyond academic profile requirements.
- [x] **Right to Erasure (Soft Delete & Data Purge):** User posts and profiles support soft-deletion and automated hard-purge policies.
- [x] **Zero Binary Exposure:** Private research datasets are never indexed or exposed to public search crawlers.
- [x] **Bilateral Privacy Protection:** User blocking immediately and completely hides research profiles, posts, and past interactions across both participants.
