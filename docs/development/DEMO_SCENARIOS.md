# Pavena End-to-End Demo Walkthroughs & Scenarios

This document provides step-by-step instructions for demonstrating and testing the key product capabilities of the Pavena platform using the seeded demo data.

---

## Scenario 1: The HULU Pathway (Researcher-Led Collaboration)

### Objective
Demonstrate how researchers discover one another, form inter-institutional research projects, track milestones/tasks, and collaborate synchronously on LaTeX scientific manuscripts.

### Walkthrough Steps

1. **Log in as Project Lead:**
   - **URL:** `http://localhost:5000/auth/login` (or POST `/api/v1/auth/login`)
   - **Email:** `andi.pratama@und.ac.id`
   - **Password:** `Demo#Pass2026`

2. **Navigate to Research Workspace:**
   - Access project: **"Autonomous Eco-Drone Swarm for Tropical Peatland Reforestation"** (`/projects/eco-drone-swarm`).
   - Notice that Dr. Andi Pratama is `ProjectMemberRole.ProjectLead` and Dr. Siti Rahmawati (from ITC) is `ProjectMemberRole.CoResearcher`.

3. **Inspect Research Milestones & Tasks:**
   - **Milestone 1:** *"Sensor Array Architecture & Peatland Calibration"* -> Status: `Achieved` (Achieved 15 days ago).
   - **Milestone 2:** *"Autonomous Pathfinding in Dense Canopy"* -> Status: `InProgress`.
   - **Milestone 3:** *"Field Trial in Central Kalimantan Peatland"* -> Status: `Planned`.
   - Notice linked tasks with distinct priorities and assignees (e.g. task *"Calibrate methane and PM2.5 multi-gas payload"* marked `Done`).

4. **Launch Collaborative LaTeX Research Studio:**
   - Open manuscript: **"Peatland-Swarm-Monitoring-Framework"** (`/projects/eco-drone-swarm/documents`).
   - Observe Monaco Editor loaded with `main.tex` containing authentic LaTeX preamble, abstract, methodology, and citations.
   - The document is linked to SignalR `CollaborationHub` (`/hubs/collaboration`) for real-time keystroke and cursor synchronization.
   - Click **Compile PDF**: The LaTeX sandbox executes `pdflatex` or triggers the graceful PDF fallback if TeX Live is absent from the host.

---

## Scenario 2: The HILIR Pathway (Industry-Led Challenge & Conversion)

### Objective
Demonstrate how industry leaders publish R&D challenges with two-tier confidential attachments, enforce digital NDAs, review academic proposals, and award project contracts with end-to-end traceability.

### Walkthrough Steps

1. **Log in as Industry Sponsor:**
   - **Email:** `diana.kusuma@energi-hijau.co.id`
   - **Password:** `Demo#Pass2026`

2. **Inspect Published Industry Challenge:**
   - Access challenge: **"Advanced Perovskite-Silicon Tandem Photovoltaic Encapsulation"** (`/challenges/tropical-tandem-pv-encapsulation`).
   - Budget: `Rp 500,000,000` to `Rp 1,000,000,000` IDR.
   - Status: `OpenForProposals`.
   - Eligibility: `AccreditedResearchInstitutesOnly` (Universities and verified research institutes).

3. **Verify Two-Tier Attachment Access & NDA Gating:**
   - **Tier 1 (Public Briefing):** `Tropical_PV_Degradation_Benchmark_Summary.pdf` -> Freely downloadable by all visitors.
   - **Tier 2 (Confidential Data):** `Confidential_Industrial_Autoclave_Test_Data_v2.pdf` -> Requires executed digital NDA.
   - **Gating Demonstration:**
     - Log in as **Dr. Siti Rahmawati** (`siti.rahmawati@itc.ac.id`): She has executed the bilateral NDA and can immediately download the confidential file.
     - Log in as **Dr. Andi Pratama** (`andi.pratama@und.ac.id`): Access to the confidential download is **blocked** (HTTP 403 Forbidden) until he signs the agreement.

4. **Inspect Converted Project (Award Traceability):**
   - Access Proposal A: **"Hydrophobic Fluoropolymer Multi-Layer Barrier Encapsulation"** (`/proposals/fluoropolymer-tandem-pv-encapsulation`).
   - Status: `ConvertedToProject`.
   - Navigate to the resulting project: **"converted-fluoropolymer-tandem-pv"**.
   - Notice:
     - `OriginType`: `ProjectOriginType.IndustryChallenge`
     - `SourceChallengeId`: Points to the industry challenge.
     - `SourceProposalId`: Points to Proposal A.
     - Members include **Dr. Siti Rahmawati** (`ProjectLead`), **Diana Kusuma** (`IndustryPartner`), and **Dr. Maya Putri** (`CoResearcher`).

---

## Scenario 3: Proposal Review & Multi-Criteria Rubric Scoring

### Objective
Demonstrate structured peer review of commercial proposals with explicit Conflict of Interest (COI) declarations and multi-criteria evaluations.

### Walkthrough Steps

1. **Inspect Proposal B Under Active Review:**
   - Title: **"Graphene-Enhanced Dual-Layer Nanocomposite Barrier"** by Dr. Budi Santoso (`/proposals/graphene-nanocomposite-pv-barrier`).
   - Status: `UnderReview`.

2. **Reviewer 1 Assessment (Dr. Eko Wijaya - PRIN):**
   - **Login:** `eko.wijaya@prin.go.id`
   - COI Declaration: Explicitly declared `None` (`"Reviewer explicitly declared absence of conflict of interest."`).
   - Rubric Evaluation:
     - Technical Merit: `8.5 / 10`
     - Innovation: `9.0 / 10`
     - Methodology: `8.0 / 10`
     - Feasibility: `8.2 / 10`
     - Impact Potential: `8.7 / 10`
     - Composite Score: **84.6%**
     - Recommendation: `RecommendRevision`

3. **Reviewer 2 Assessment (Dr. Rina Dewi - ITC):**
   - **Login:** `rina.dewi@itc.ac.id`
   - COI Declaration: Explicitly declared `None`.
   - Rubric Evaluation:
     - Technical Merit: `9.0 / 10`
     - Methodology: `8.8 / 10`
     - Alignment: `9.2 / 10`
     - Composite Score: **88.8%**
     - Recommendation: `RecommendAccept`

---

## Scenario 4: Security Boundaries, COI & Blocking Enforcement

### Objective
Verify that security boundaries and conflict of interest rules are enforced strictly on the server:

1. **Interpersonal Blocking COI Exclusion:**
   - **Dr. Budi Santoso** has an active interpersonal block against **Dr. Hendra Gunawan**.
   - If the sponsor attempts to assign Dr. Hendra Gunawan to review Dr. Budi's proposal, the server rejects the assignment with `DomainException` to guarantee reviewer impartiality.

2. **Institutional Proximity Detection:**
   - **Dr. Andi Pratama** and **Dr. Maya Putri** both hold verified faculty memberships at **Universitas Nusantara Digital (`und.ac.id`)**.
   - Assigning Dr. Maya Putri to evaluate Dr. Andi's proposal triggers an institutional proximity warning.

3. **Platform Administrator Confidentiality Boundary:**
   - **Login:** `admin@pavena.local` / `Demo#Pass2026`
   - While the Platform Admin can suspend users or moderate public posts, they **CANNOT** read confidential proposals or download private research manuscripts.
   - An attempt to query `/api/v1/proposals/{proposalId}` for an unassigned proposal returns HTTP 403 / null, ensuring proprietary research and industrial IP remain strictly confidential.

---

## Scenario 5: Academic Social Graph & Feed Experience

### Objective
Demonstrate academic discovery, connections, follows, and visibility-scoped feed distribution.

### Walkthrough Steps

1. **Connections:**
   - Dr. Andi Pratama <-> Dr. Siti Rahmawati (`Accepted`)
   - Dr. Siti Rahmawati <-> Dr. Maya Putri (`Accepted`)
   - Dr. Budi Santoso -> Dr. Siti Rahmawati (`Pending` invitation awaiting response)

2. **One-Way Follows:**
   - Dr. Maya Putri follows Dr. Andi Pratama
   - Ir. Reza Gunadi (CTO) follows Dr. Andi Pratama
   - Diana Kusuma (VP R&D) follows Dr. Siti Rahmawati

3. **Feed Visibility Scope:**
   - **Public Post (Dr. Andi):** Visible to all platform visitors and search engines.
   - **Connections Post (Dr. Siti):** Visible exclusively to accepted 1st-degree connections.
   - **OnlyMe Post (Dr. Budi):** Visible strictly to Dr. Budi Santoso; completely hidden from public and connected feeds.
