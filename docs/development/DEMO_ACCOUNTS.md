# Pavena Demo Accounts & Persona Directory

All demo personas are seeded deterministically with real-world academic, research institute, and industrial profiles.

> [!IMPORTANT]
> **Universal Demo Password:** `Demo#Pass2026`
> *(Applies to all accounts below except the initial Superadmin)*

---

## 1. Quick Authentication Reference

| # | Persona & Display Name | Email / Login | Platform Role | Affiliation | Primary Function |
| :- | :--- | :--- | :--- | :--- | :--- |
| **0** | **Superadmin** | `superadmin@pavena.local`<br>*(Pass: `PavenaAdmin#2026`)* | `PlatformRole.Superadmin` | Platform Governance | System bootstrapping & emergency governance |
| **1** | **Platform Admin** | `admin@pavena.local` | `PlatformRole.PlatformAdmin` | Platform Operations | Platform moderation & policy enforcement |
| **2** | **Dr. Andi Pratama** | `andi.pratama@und.ac.id` | None (User) | Univ. Nusantara Digital (UND) | **HULU Project Lead** (Drone Swarm, LaTeX author) |
| **3** | **Dr. Siti Rahmawati** | `siti.rahmawati@itc.ac.id` | None (User) | Inst. Teknologi Cakrawala (ITC) | **HILIR Award Winner** (NDA signed, Converted Project) |
| **4** | **Dr. Budi Santoso** | `budi.santoso@prin.go.id` | None (User) | Pusat Riset Inovasi Nusantara (PRIN) | **Proposal B Submitter** (Under Review, COI blocker) |
| **5** | **Dr. Maya Putri** | `maya.putri@und.ac.id` | None (User) | Univ. Nusantara Digital (UND) | **Co-Investigator** (Nanotech, UND institutional proximity) |
| **6** | **Dr. Eko Wijaya** | `eko.wijaya@prin.go.id` | None (User) | Pusat Riset Inovasi Nusantara (PRIN) | **Peer Reviewer 1** (Score 84.6%, Minor Revision) |
| **7** | **Dr. Rina Dewi** | `rina.dewi@itc.ac.id` | None (User) | Inst. Teknologi Cakrawala (ITC) | **Peer Reviewer 2** (Score 88.8%, Accept) |
| **8** | **Dr. Hendra Gunawan** | `hendra.gunawan@und.ac.id` | None (User) | Univ. Nusantara Digital (UND) | **Excluded Reviewer** (Blocked by Budi for COI) |
| **9** | **Diana Kusuma, M.B.A.** | `diana.kusuma@energi-hijau.co.id` | None (User) | PT Energi Hijau Nusantara | **Challenge Sponsor & Decision Maker** (VP of R&D) |
| **10** | **Ir. Reza Gunadi** | `reza.gunadi@inovasi-tech.co.id` | None (User) | PT Inovasi Teknologi Indonesia | **Industry Sponsor** (CTO, Autonomous Systems) |
| **11** | **Bambang Sutrisno** | `bambang@agro-digital.co.id` | None (User) | PT Agro Digital Solusi | **Industry Sponsor** (Head of Innovation) |

---

## 2. Detailed Persona Profiles

### 1. Platform Governance & Operations

#### Superadmin (`superadmin@pavena.local`)
- **Username:** `superadmin`
- **Initial Password:** `PavenaAdmin#2026`
- **Role:** `PlatformRole.Superadmin`
- **Capabilities:** Database migrations, role elevation, emergency lockout override, tenant governance.

#### Platform Administrator (`admin@pavena.local`)
- **Username:** `platform_admin`
- **Role:** `PlatformRole.PlatformAdmin`
- **Capabilities:** Content moderation, account suspension, audit log inspection.
- **Security Boundary:** **Has NO automatic read access** to private research workspaces, confidential commercial proposals, or NDA-gated attachments. Respects multi-party research confidentiality.

---

### 2. Academic Researchers (HULU & HILIR)

#### Dr. Andi Pratama, Ph.D. (`andi.pratama@und.ac.id`)
- **Affiliation:** Associate Professor, Universitas Nusantara Digital (UND)
- **Specialization:** Autonomous Multi-Agent Robotics, Drone Swarms, Edge AI, Mesh Telemetry
- **Role in Platform:**
  - Owner of HULU project `eco-drone-swarm` ("Autonomous Eco-Drone Swarm for Tropical Peatland Reforestation").
  - Author and collaborator on LaTeX manuscript `Peatland-Swarm-Monitoring-Framework.tex`.
  - Author of trending public feed posts.
  - Has **NOT** signed the Industry Challenge NDA (demonstrates gating!).

#### Dr. Siti Rahmawati, S.T., M.Eng. (`siti.rahmawati@itc.ac.id`)
- **Affiliation:** Lead Researcher, Institut Teknologi Cakrawala (ITC)
- **Specialization:** Perovskite Photovoltaics, Materials Science, ALD Barrier Encapsulation
- **Role in Platform:**
  - Co-researcher on Dr. Andi's drone swarm project.
  - **Digitally executed bilateral NDA** for PT Energi Hijau Nusantara's Industry Challenge.
  - Submitter of Proposal A (`fluoropolymer-tandem-pv-encapsulation`), which passed review, won the industry award, and was converted into ResearchProject `converted-fluoropolymer-tandem-pv`.

#### Dr. Budi Santoso (`budi.santoso@prin.go.id`)
- **Affiliation:** Senior Scientist, Pusat Riset Inovasi Nusantara (PRIN)
- **Specialization:** Precision Agriculture, IoT Agronomy, Peatland Hydrology, Nanocomposites
- **Role in Platform:**
  - Submitter of Proposal B (`graphene-nanocomposite-pv-barrier`), currently under active peer review.
  - Has an active interpersonal block against Dr. Hendra Gunawan (demonstrating COI blocker enforcement).
  - Author of private `OnlyMe` draft posts.

#### Dr. Maya Putri (`maya.putri@und.ac.id`)
- **Affiliation:** Assistant Professor, Universitas Nusantara Digital (UND)
- **Specialization:** Nanomaterials, Biosensors, Microfluidic Interfaces
- **Role in Platform:**
  - Co-investigator on Proposal A with Dr. Siti.
  - Shares institutional affiliation (UND) with Dr. Andi Pratama, demonstrating proximity detection.

---

### 3. Independent Peer Reviewers

#### Dr. Eko Wijaya (`eko.wijaya@prin.go.id`)
- **Affiliation:** Senior Research Specialist, Pusat Riset Inovasi Nusantara (PRIN)
- **Specialization:** Clean Energy Technologies, Photovoltaic Standards, Grant Assessment
- **Role in Platform:**
  - Assigned as Reviewer 1 for Proposal B.
  - Declared absence of Conflict of Interest (`CoiDeclarationType.None`).
  - Submitted multi-criteria rubric evaluation: Overall score **84.6%**, Recommendation: `RecommendRevision`.

#### Dr. Rina Dewi (`rina.dewi@itc.ac.id`)
- **Affiliation:** Associate Professor, Institut Teknologi Cakrawala (ITC)
- **Specialization:** Renewable Power Systems, Polymer Composites, Accelerated Weathering
- **Role in Platform:**
  - Assigned as Reviewer 2 for Proposal B.
  - Declared absence of Conflict of Interest (`CoiDeclarationType.None`).
  - Submitted multi-criteria rubric evaluation: Overall score **88.8%**, Recommendation: `RecommendAccept`.

#### Dr. Hendra Gunawan (`hendra.gunawan@und.ac.id`)
- **Affiliation:** Professor of Informatics, Universitas Nusantara Digital (UND)
- **Role in Platform:**
  - Serves as the negative-test persona for Conflict of Interest. Because Dr. Budi Santoso has an active `UserBlock` against him, the platform prohibits assigning Dr. Hendra to review Dr. Budi's proposals.

---

### 4. Industry Sponsors & Commercial R&D Leaders

#### Diana Kusuma, M.B.A. (`diana.kusuma@energi-hijau.co.id`)
- **Organization:** VP of R&D, PT Energi Hijau Nusantara (`energi-hijau`)
- **Role in Platform:**
  - Creator and publisher of Industry Challenge `tropical-tandem-pv-encapsulation` (Funding: Rp 1,000,000,000).
  - Evaluated eligibility, assigned peer reviewers, and made final funding award.
  - Designated as `ProjectMemberRole.IndustryPartner` on the converted research project.

#### Ir. Reza Gunadi (`reza.gunadi@inovasi-tech.co.id`)
- **Organization:** CTO, PT Inovasi Teknologi Indonesia (`inovasi-tech`)
- **Role in Platform:** Commercial partner and prospective deep-tech challenge sponsor. Follows Dr. Andi Pratama on the academic network.

#### Bambang Sutrisno (`bambang@agro-digital.co.id`)
- **Organization:** Head of Product Innovation, PT Agro Digital Solusi (`agro-digital`)
- **Role in Platform:** Agricultural technology sponsor and commercialization director.
