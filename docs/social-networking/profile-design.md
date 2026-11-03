# Academic Profile Design & Privacy Controls

Pavena's academic profiles showcase researchers' credentials, publications, affiliations, and scholarly background.

---

## 1. Domain Entities & Normalization

Profile information is structured into normalized entities rather than a monolithic JSON blob:

- **`UserProfile`:** Display name, username/slug, headline, biography, location, preferred language, avatar/banner URLs, profile visibility.
- **`UserEducation`:** Academic institutions, degrees, fields of study, start/end years, honors/grades.
- **`UserExperience`:** Research/professional positions, institutions/companies, role title, employment type, start/end dates, current status, description.
- **`UserSkill`:** Technical and academic skills, methodologies.
- **`UserInterest`:** Research topics, academic domains, keywords.
- **`UserLink`:** External scholarly URLs (ORCID, Google Scholar, ResearchGate, personal lab page).
- **`OrganizationMembership`:** Verified or unverified affiliations with universities or research organizations.

---

## 2. Privacy & Information Hiding

- **Public Profiles (`GET /api/v1/users/{id}/profile`):**
  - Exposes: Display Name, Headline, Bio, Country, City, Avatar, Banner, Educations, Experiences, Skills, Interests, Links, Public Affiliations.
  - Strictly Excludes: Email address, normalized username/email, password hashes, security stamps, lockout times, refresh tokens, audit logs, unapproved organization memberships.
  - If `Visibility == Private`: Non-superadmin viewers receive minimal card (Display Name, Headline, Avatar) with details masked.
  - If `Visibility == NetworkOnly`: Detailed sections are visible only to accepted connections.

---

## 3. Profile Completeness Calculation

Profile completeness is computed on the fly (0% to 100%) based on:
1. Basic info (Display Name, Headline, Bio, Country, City, Avatar) -> 30%
2. At least 1 Education entry -> 20%
3. At least 1 Experience entry -> 20%
4. At least 1 Skill or Interest -> 15%
5. At least 1 External Link -> 15%
