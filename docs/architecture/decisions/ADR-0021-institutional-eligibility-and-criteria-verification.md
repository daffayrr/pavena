# ADR-0021: Institutional Eligibility & Structured Criteria Verification

## Status
Proposed (Milestone O Architecture Baseline)

## Date
2026-11-04

## Context
Commercial sponsors frequently specify targeted constraints regarding who can submit proposals for a given challenge. For example:
- Some challenges require accredited higher-education institutions (Universities) with established laboratory facilities.
- Some funding frameworks mandate public-private consortia (e.g. University + SME).
- Certain challenges are restricted to specific geographical jurisdictions or certified research organizations.

## Decision

1. **Eligibility Taxonomy (`InstitutionalEligibility`):**
   - `OpenToAll`: Any verified researcher (independent or affiliated).
   - `UniversitiesOnly`: Lead principal investigator must have a confirmed active affiliation with a verified `Organization` of type `University`.
   - `AccreditedResearchInstitutesOnly`: Lead researcher must be affiliated with an accredited `ResearchInstitution` or `University`.
   - `ConsortiumRequired`: Proposal team must consist of researchers from at least two distinct organizations.

2. **Automated Server-Side Pre-Flight Check:**
   - The application service evaluates researcher eligibility prior to issuing proposal drafting tokens or granting confidential attachment access.
   - If a researcher fails criteria, the API returns a structured eligibility report explaining missing criteria (e.g., "Requires verified university affiliation", "Requires minimum h-index or expertise match") without leaking proprietary challenge information.

3. **Structured Research Domain & Expertise Matching:**
   - Challenges categorize requirements into normalized research domains (`ChallengeResearchDomain`) and skill requirements (`ChallengeExpertiseRequirement`).
   - Enables algorithmic matching against researcher profiles (`UserProfile.ResearchInterests`, `UserSkill`), populating personalized discovery feeds.

## Consequences

### Positive
- Minimizes irrelevant or unqualified proposal submissions for corporate sponsors.
- Guides academic researchers to collaboration opportunities strictly matching their faculty credentials and laboratory capabilities.

### Trade-offs
- Researchers without institutional affiliations cannot lead proposals on university-restricted challenges.
