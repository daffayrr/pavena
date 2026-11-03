# ADR-0008: Native PostCategory Domain Entity & PostDto Enrichment

## Status
Accepted

## Date
2026-11-03

## Context
During Milestone D and K, scholarly publications were modeled using `Post`, `PostAttachment`, `PostVisibility`, and `PostStatus`. While Milestone K provided frontend category selection (Research Update, Research Finding, Academic Discussion, Publication, Collaboration Opportunity, Conference & Event, General Academic) using client-side tag encapsulation, a domain gap was documented:
1. The database schema lacked a first-class `Category` column on `Posts`, preventing server-side filtering, indexing, and category analytics.
2. `PostDto` lacked the author's academic headline and institutional affiliation, preventing the feed UI from displaying verified scholar credentials without separate secondary queries (avoiding N+1 round-trips).

Before starting Phase 4 (Research Collaboration), this domain foundation must be stabilized with native database persistence and DTO enrichment.

## Decision
1. **Domain Enum:** Introduce `PostCategory` in `Pavena.Domain.Enums` with values:
   - `ResearchUpdate = 1`
   - `ResearchFinding = 2`
   - `AcademicDiscussion = 3`
   - `Publication = 4`
   - `CollaborationOpportunity = 5`
   - `ConferenceAndEvent = 6`
   - `GeneralAcademic = 7`
2. **Entity Modification:** Add `public PostCategory Category { get; private set; } = PostCategory.GeneralAcademic;` to `Pavena.Domain.Entities.Social.Post`.
3. **Database Indexing & Migrations:**
   - Configure EF Core property as string conversion (`varchar(50)` / `character varying(50)`).
   - Add composite database index `IX_Posts_Category_Status_PublishedAtUtc` on `(Category, Status, PublishedAtUtc)` for high-performance category filtering.
   - Generate dual EF Core migrations for both PostgreSQL and MySQL providers.
4. **PostDto & Request Enrichment:**
   - Add `string Category`, `string? AuthorHeadline`, and `string? AuthorInstitution` to `PostDto`.
   - Update `CreatePostRequest` and `UpdatePostRequest` to accept `string? Category = null`.
   - Update `PostService` and `FeedService` mapping to automatically populate author headline and primary institution from `AuthorUser.Profile` and `AuthorUser.Memberships`.
5. **Backward Compatibility:**
   - In `PostService`, if an incoming post body starts with a legacy tag `[CategoryName]`, parse it smoothly and persist the native enum property, ensuring existing data integrity.

## Consequences
- **Positive:**
  - High-performance, indexed category filtering at the database engine level.
  - Complete elimination of N+1 queries for author headline/affiliation on feed and post displays.
  - Strongly typed domain rules for publication categorization.
- **Trade-off:**
  - Requires executing EF Core database migration across staging and production instances.
