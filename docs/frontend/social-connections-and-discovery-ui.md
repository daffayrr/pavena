# Pavena — Social Connections UI & Researcher Discovery Specification
## Phase 3 / Milestone J

### 1. Overview & Architectural Alignment

Milestone J delivers the user interface and frontend experience for **Social Connections & Researcher Discovery** on the Pavena platform. It adheres strictly to the approved **Academic Professional** design system (Light + Dark mode, Balanced density, Deep Navy `#17365D` and Teal `#187C78` palette).

The implementation strictly avoids replicating domain state machines or database entities on the client. The ASP.NET Core backend is the authoritative source of truth for:
- Mutual connection states (`None`, `PendingIncoming`, `PendingOutgoing`, `Accepted`).
- One-way follow relationships.
- Bidirectional user blocking and automatic relational decoupling.
- Discovery search and privacy visibility filters.

---

### 2. Route Architecture & Page Specifications

All network pages feature the shared [`NetworkNavTabs.razor`](file:///D:/coding/_toscaflow_/pavena/backend/src/Pavena.Web/Components/Network/NetworkNavTabs.razor) header bar for seamless switching between sections:

| Route | Component Page | Target Functionality |
|---|---|---|
| `/network/discover`, `/discover` | `NetworkDiscoverPage.razor` | Real-time researcher search and discovery with debounced query, institution, and expertise filters, researcher cards, connection triggers, and follow toggles. |
| `/network/connections`, `/network` | `MyConnectionsPage.razor` | Directory of accepted mutual connections with real-time text search, profile links, and connection termination modal. |
| `/network/requests` | `ConnectionRequestsPage.razor` | Segmented management of Incoming requests (`Accept` / `Reject`) and Outgoing requests (`Cancel`), backed by backend state machine transitions. |
| `/network/following` | `FollowingPage.razor` | Directory of researchers followed by the user with one-click `Unfollow` toggle. |
| `/network/followers` | `FollowersPage.razor` | Directory of peers following the user, with follow-back status indicators. |
| `/network/blocked` | `BlockedScholarsPage.razor` | Privacy isolation management displaying blocked accounts, reasons, and unblocking confirmation modals. |

---

### 3. Reusable Network Component Library

Located under `backend/src/Pavena.Web/Components/Network/`:

1. **`NetworkNavTabs.razor`**:
   - Tab strip providing direct navigation across `discover`, `connections`, `requests`, `following`, `followers`, and `blocked`.
   - Real-time notification badge for pending incoming connection requests.
   - Active state indicators compatible with CSS tokens across both Light and Dark modes.

2. **`ResearcherCard.razor`**:
   - Academic banner gradient, scholar avatar, full display name, and handle (`@username`).
   - Institutional verification pill (`Peneliti Terverifikasi`) vs claimed metadata.
   - Primary institutional affiliation and academic headline.
   - Interactive research expertise tags (`.pv-skill-chip`).
   - Publication and connection counts.
   - State-aware action buttons:
     - `Koneksikan` (Status: `None`)
     - `Batalkan` (Status: `PendingOutgoing`)
     - `Terima` (Status: `PendingIncoming`)
     - `Terhubung` (Status: `Accepted`)
     - `Ikuti / Mengikuti` toggle.

3. **`ConnectionCard.razor`**:
   - Connected scholar identity card with mutual connection timestamp.
   - Direct link to scholar profile (`/scholars/{identifier}`).
   - Accessible "Putuskan Koneksi" trigger with confirmation dialog (`PvModal`).

4. **`ConnectionRequestCard.razor`**:
   - Incoming request: Displays requester identity, invitation timestamp, and `Terima` (primary) / `Tolak` (outline) buttons.
   - Outgoing request: Displays addressee identity, invitation timestamp, and `Batalkan Permintaan` (outline) button.

5. **`FollowCard.razor`**:
   - Displays followed/follower scholars with follow date.
   - Follow/Unfollow toggle with asynchronous loading indicators.

6. **`BlockedUserCard.razor`**:
   - Displays blocked user identity, blocking timestamp, and optional reason.
   - Unblocking confirmation dialog (`PvModal`).

---

### 4. Backend Discovery API Implementation & Repository Audit

In accordance with Section B of the specification, the initial audit identified that while `FeedController` had a discover feed for posts, `UsersController` lacked a dedicated researcher discovery endpoint. 

To satisfy Definition of Done #1 ("Researcher discovery berfungsi dengan API aktual") without creating mock persistence:
1. **Added `GET /api/v1/users/discover`** on `UsersController`:
   - Supports query parameters `q` (name/headline/username), `institution`, `expertise`, `limit`, and `offset`.
   - Filters out `ProfileVisibility.Private` accounts.
   - Filters out bidirectional user blocks.
   - Evaluates caller-specific connection status (`ConnectionStatus`) and follow status (`IsFollowing`).
2. **Enriched Connection DTOs**:
   - `ConnectionDto` was augmented with optional profile properties (`RequesterDisplayName`, `RequesterAvatarUrl`, `RequesterHeadline`, `AddresseeDisplayName`, etc.) allowing incoming/outgoing requests to render identity information directly from the database.
   - `ConnectionUserDto`, `FollowUserDto`, and `BlockedUserDto` were enriched with `Username` for direct `@username` linking.

---

### 5. Privacy, Authorization & Security Isolation

- **Bidirectional Block Isolation:** When User A blocks User B:
  - Both users are immediately invisible to each other in `GET /api/v1/users/discover`.
  - Mutual connections and follows are atomically severed in the database transaction.
  - User B cannot view User A's profile or send connection requests.
- **Client Zero-Leakage:** Blocked or restricted user metadata is never delivered to the client DOM.

---

### 6. Identified Technical Debt & Roadmap Gaps

1. **Full-Text Inverted Indexing for Academic Discovery:**
   - The current discovery endpoint utilizes EF Core SQL `LIKE / ILIKE` against indexed name, handle, organization, and skill tables. For large-scale production, an inverted search engine (Elasticsearch, PostgreSQL Full-Text Search with `tsvector`, or Meilisearch) will be integrated.
2. **Mutual Connection Recommendations (Graph Traversal):**
   - The discovery view currently lists verified scholars alphabetically. Second-degree graph recommendations ("Researchers you may know / Shared connections") will be introduced in Phase 4.
3. **Institutional Department Hierarchy:**
   - Organizations are currently filtered by organization name. Sub-unit filtering (faculty, laboratory, division) is slated for the Organization Workspaces milestone.
