# Pavena — Feed Experience & Social Networking Integration

**Milestone:** Phase 3 / Milestone L  
**Status:** Completed  
**Authors:** Pavena Engineering Team  
**Date:** November 2026  

---

## 1. Executive Summary

Milestone L delivers the core **Home Feed Experience** for Pavena, integrating the social networking graph, academic researcher profiles, scholarly publications, bilateral block rules, and deterministic keyset cursor pagination into a unified academic feed.

The feed is structured into two dedicated operational modes:
1. **My Network Feed (`/feed/network` or `/feed`)**: Displays reverse-chronological updates authored by the authenticated scholar, accepted mutual connections (Public & Connections visibility), and followed scholars (Public visibility only). Follow relationships strictly do not grant access to connection-restricted content.
2. **Discover Feed (`/feed/discover`)**: Open-access scholarly stream displaying published posts with `Public` visibility from across the platform, filtered strictly to eliminate any bilateral blocks.

---

## 2. Page & Component Architecture

### Routes & Navigation
- `/feed`: Default authenticated landing stream displaying **My Network Feed**.
- `/feed/network`: Explicit network feed route with accepted connection posts and followed updates.
- `/feed/discover`: Platform-wide open exploration stream.
- `/`: Automatically redirects to `/feed` without route flash.

### Components
- [`FeedPage.razor`](file:///D:/coding/_toscaflow_/pavena/backend/src/Pavena.Web/Pages/FeedPage.razor): The central home feed page managing mode transitions, keyset cursor pagination state, quick post creation, live stream prepending, refresh operations, and sidebar rails.
- [`FeedSkeleton.razor`](file:///D:/coding/_toscaflow_/pavena/backend/src/Pavena.Web/Components/Posts/FeedSkeleton.razor): Shimmer loading skeleton displayed during initial load and manual refresh operations.
- [`PostCard.razor`](file:///D:/coding/_toscaflow_/pavena/backend/src/Pavena.Web/Components/Posts/PostCard.razor): Feed post card displaying author avatar, name, headline/institution, category badge, timestamp, clean content body, attachment pills, and owner actions (Edit, Publish, Archive, Delete).
- [`PostComposer.razor`](file:///D:/coding/_toscaflow_/pavena/backend/src/Pavena.Web/Components/Posts/PostComposer.razor): Embedded quick-post launcher and inline composer directly atop the feed.

---

## 3. Keyset Cursor Pagination & Deduplication

In accordance with non-negotiable architectural requirements, Pavena utilizes **pure deterministic keyset cursor pagination** instead of offset-based pagination:

```text
Cursor Schema: Version | PublishedAtUtcTicks | PostId | Signature (HMAC-SHA256)
```

### Client-Side Rules
1. **Opaque Tokens:** The client treats cursors as immutable, opaque strings and never parses, edits, or synthesizes cursor values.
2. **Duplicate Prevention:** When appending newly fetched pages (`LoadMoreAsync`), the client computes existing IDs and filters out any post IDs already present in the active feed stream:
   ```csharp
   var existingIds = new HashSet<Guid>(_posts.Select(p => p.Id));
   var newItems = res.Items.Where(p => !existingIds.Contains(p.Id)).ToList();
   _posts.AddRange(newItems);
   ```
3. **State Transitions:**
   - Deletion of an active post immediately removes the card from the stream.
   - Archiving or changing post visibility to `OnlyMe` immediately evicts the card from public/network view.
   - Creating a new post prepends the card to the top of the feed without needing a full reload.

---

## 4. Social Engagement: Apresiasi & Diskusi Audit

### Audit Finding
- The backend domain currently does not possess database entities, migrations, or controllers for post reactions (`Appreciation` / `Like`) or comments (`Comment` / `Discussion`).
- **Prompt Part F Mandate:** *"Audit apakah backend sudah menyediakan API yang benar. Jika belum, tampilkan fitur sebagai Coming Soon atau sembunyikan. Jangan membuat tombol interaksi palsu yang hanya mengubah UI tanpa persistence."*

### UI Implementation
- Buttons for **Apresiasi** and **Diskusi** are styled with an explicit **Segera** (`.pv-coming-soon-pill`) badge.
- Clicking either button triggers an informational toast explaining that persistent reactions and threaded discussions are scheduled for the upcoming engagement release, preventing any unpersisted counter increments or deceptive client-only mutations.

---

## 5. Researcher Suggestions & Discovery Rail

### Audit Finding
- The backend provides keyword, institution, and expertise discovery queries via `GET /api/v1/users/discover`, but does not yet feature a machine-learning recommendation engine (`/api/v1/recommendations/researchers`).
- **Prompt Part G Mandate:** *"Jika belum tersedia, jangan membuat algoritma rekomendasi semu. Dokumentasikan kebutuhan API untuk tahap berikutnya."*

### UI Implementation
- The Feed Right Rail leverages the genuine `GET /api/v1/users/discover` endpoint to fetch real, active scholars with public profiles, displaying their verified display name, institution, and avatar.
- Clicking the quick connect (`+`) button invokes the real `POST /api/v1/connections/requests` API from Milestone C/J.

---

## 6. Security, Isolation, & Authorization

1. **Server-Side Enforcement:** Every feed request is authorized server-side via `ClaimsPrincipal`.
2. **Bilateral Block Isolation:** Blocked accounts cannot see each other's posts in either Network or Discover feeds.
3. **Connection Privacy:** Posts marked `Connections` are strictly filtered out of the Discover feed and are hidden from followers who are not accepted mutual connections.
4. **Draft and OnlyMe Isolation:** Draft and private posts are strictly excluded from feed queries on the server.
