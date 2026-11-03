# Pavena — Academic Profile UI & Researcher Identity Specification
## Phase 3 / Milestone I

### 1. Overview & Architectural Alignment

Milestone I establishes the official frontend user experience for **Academic Profiles and Researcher Identity** on the Pavena platform. It conforms strictly to the approved **Academic Professional** design system, supporting both Light and Dark modes with balanced layout density.

The frontend consumes the existing .NET 8 ASP.NET Core backend APIs developed in Milestones B, C, D, and E without duplicating domain logic or database entities.

---

### 2. Information Architecture & Route Mapping

| Route | Component Page | Target User / Role | Description |
|---|---|---|---|
| `/profile`, `/profile/me` | `MyProfilePage.razor` | Authenticated Scholar (Owner) | Complete academic profile dashboard, completeness progress bar, edit modal trigger, publication posts, and education/experience/skills CRUD. |
| `/scholars/{identifier}`, `/profile/{identifier}` | `ResearcherProfilePage.razor` | Any User / Colleague | Public scholar viewer with connection state machine controls, follow/unfollow toggle, verified badge distinction, and server-side privacy masking. |

---

### 3. Component Hierarchy & Reusability

All components are located under `backend/src/Pavena.Web/Components/Profile/`:

1. **`ProfileHeader.razor`**:
   - Academic banner gradient with geometric micro-pattern.
   - Overlapping avatar with live status indicator.
   - Scholar full display name, verified researcher badge, and username handle.
   - Academic headline and primary institutional affiliation with department/title.
   - Geographical location, research laboratory website link, and ORCID badge.
   - Scholarly metrics: Publications count, Network connections count, Followers count, Following count.
   - Dynamic contextual action buttons:
     - For owner: `Edit Profil Peneliti` trigger.
     - For peers: Full connection state machine (`Koneksikan Kolega` -> `Batalkan Permintaan` -> `Terima Koneksi` -> `Terhubung`) and `Ikuti / Berhenti Mengikuti` toggle.

2. **`CompletenessCard.razor`**:
   - Real-time completeness calculation indicator (0–100%).
   - Gradient progress bar (`var(--pv-color-secondary)` to `var(--pv-color-primary)`).
   - Task breakdown badges with completion status:
     - Headline Akademik (+15%)
     - Biografi Riset (+15%)
     - Riwayat Pendidikan (+15%)
     - Pengalaman Riset (+15%)
     - Keahlian Spesifik (+10%)
     - ORCID iD (+10%)
     - Afiliasi Lembaga (+5%)
     - Akun Dasar (+15%)

3. **`EducationList.razor`**:
   - Timeline cards showcasing university/institution name, academic degree, field of study, study period, and graduation honors.
   - Interactive delete action (owner only) and "Tambah Pendidikan" trigger.

4. **`ExperienceList.razor`**:
   - Research appointments, laboratory roles, faculty positions, and postdoc history.
   - Start/end dates, "Posisi Aktif" badge for ongoing roles, and research responsibility descriptions.
   - Interactive delete action (owner only) and "Tambah Pengalaman" trigger.

5. **`SkillsList.razor`**:
   - Interactive pill chips (`.pv-skill-chip`) for research expertise, technical methods, and domains.
   - Inline tag removal for owner.
   - Quick input for typing new skills with keyboard `Enter` support.

6. **`AffiliationsList.razor`**:
   - Institutional cards clearly distinguishing **Verified Institutional Affiliation** (`.pv-pill-verified`) from **Self-Claimed Affiliation** (`.pv-pill-claimed`).
   - Displays organization name, department, role, and appointment title.

7. **`ProfileEditModal.razor`**:
   - Tabbed dialog modal (`Informasi Dasar`, `Tambah Pendidikan`, `Tambah Pengalaman`, `Pratinjau`).
   - Live Preview tab allowing researchers to review how their identity appears to colleagues before saving.
   - Real-time feedback and validation alerts via `IToastService`.

---

### 4. Verified vs. Claimed Academic Information Taxonomy

A foundational requirement of the Pavena platform is academic integrity. Information claims are categorized and presented as follows:

1. **Institutional Affiliation Verification**:
   - An affiliation is marked with the green badge **Peneliti Terverifikasi / Afiliasi Terverifikasi** only when the backend `Organization.VerificationStatus == Verified` and membership is confirmed.
   - Unverified memberships are marked with **Klaim Mandiri** to communicate to readers that the claim has not yet undergone domain or platform audit.

2. **ORCID Metadata**:
   - Displayed with the distinctive ORCID badge (`iD 0000-000X-XXXX-XXXX`).
   - Clarified in both the UI hint and data dictionary as researcher-claimed academic metadata rather than institutional employment proof.

---

### 5. Server-Side Privacy Authorization & Zero Frontend Leakage

Privacy enforcement is executed authoritatively on the ASP.NET Core backend:
- `PrivacyLevel = "Private"`: Restricts biography, education, experience, and skills from any non-owner caller. The backend returns `null` for sensitive properties and empty lists for private collections.
- `PrivacyLevel = "NetworkOnly"`: Only permits full inspection if a mutual `Accepted` connection relationship exists.
- The frontend renders an informative privacy notice (`Visibilitas Profil Terbatas`) when accessing restricted profiles without leaking any hidden data.

---

### 6. Identified Technical Debt & Backend API Gaps

In accordance with Pavena architectural guidelines, the following items are acknowledged as future roadmap items rather than fabricated client mocks:

1. **Third-Party ORCID OAuth 2.0 Ingestion**:
   - Currently, researchers enter their 16-digit ORCID identifier manually. Future phases will introduce live OAuth handshake with `orcid.org` to import verified publication DOIs and employment records automatically.
2. **DOI Journal Publication Synchronization**:
   - Publications currently display native scholarly posts published within Pavena. Automatic CrossRef / OpenAlex bibliographic DOI scraping is scheduled for Phase 4 (Research Workspace & Document Collaboration).
3. **Avatar and Document File Upload Service**:
   - Avatars and banners currently accept URLs. Direct multipart image upload to S3/MinIO storage will be plugged in via the upcoming blob storage service.
