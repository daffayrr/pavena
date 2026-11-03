# Phase 2 REST API Endpoints Specification

All endpoints are hosted under version prefix `/api/v1/`.

---

## 1. Profiles

| Method | Route | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `GET` | `/users/{id}/profile` | View public profile by user ID | Optional / Bearer |
| `GET` | `/users/me/profile` | View authenticated user's own profile | Bearer |
| `PUT` | `/users/me/profile` | Update authenticated user's profile | Bearer |
| `PUT` | `/users/me/profile/privacy` | Update profile visibility setting | Bearer |
| `GET` | `/users/me/education` | List user's education entries | Bearer |
| `POST` | `/users/me/education` | Add education entry | Bearer |
| `PUT` | `/users/me/education/{id}` | Update education entry | Bearer |
| `DELETE` | `/users/me/education/{id}` | Delete education entry | Bearer |
| `GET` | `/users/me/experience` | List user's experience entries | Bearer |
| `POST` | `/users/me/experience` | Add experience entry | Bearer |
| `PUT` | `/users/me/experience/{id}` | Update experience entry | Bearer |
| `DELETE` | `/users/me/experience/{id}` | Delete experience entry | Bearer |
| `GET` | `/users/me/skills` | List user's skills | Bearer |
| `POST` | `/users/me/skills` | Add user skill | Bearer |
| `DELETE` | `/users/me/skills/{id}` | Remove user skill | Bearer |
| `GET` | `/users/me/links` | List external scholarly links | Bearer |
| `POST` | `/users/me/links` | Add external link | Bearer |
| `DELETE` | `/users/me/links/{id}` | Remove external link | Bearer |

---

## 2. Connections

| Method | Route | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `POST` | `/connections/requests` | Send a connection request | Bearer |
| `POST` | `/connections/requests/{id}/accept` | Accept an incoming connection request | Bearer |
| `POST` | `/connections/requests/{id}/reject` | Reject an incoming connection request | Bearer |
| `DELETE` | `/connections/requests/{id}` | Cancel an outgoing connection request | Bearer |
| `DELETE` | `/connections/{userId}` | Remove an accepted mutual connection | Bearer |
| `GET` | `/connections` | List accepted connections | Bearer |
| `GET` | `/connections/requests/incoming` | List pending requests received | Bearer |
| `GET` | `/connections/requests/outgoing` | List pending requests sent | Bearer |
| `GET` | `/connections/status/{userId}` | Get connection status with user | Bearer |

---

## 3. Following

| Method | Route | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `POST` | `/users/{userId}/follow` | Follow a user | Bearer |
| `DELETE` | `/users/{userId}/follow` | Unfollow a user | Bearer |
| `GET` | `/users/{userId}/followers` | List followers of a user | Bearer |
| `GET` | `/users/{userId}/following` | List users followed by user | Bearer |

---

## 4. User Blocking

| Method | Route | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `POST` | `/users/{userId}/block` | Block a user (severs connections & follows) | Bearer |
| `DELETE` | `/users/{userId}/block` | Unblock a user | Bearer |
| `GET` | `/users/me/blocks` | List users blocked by current user | Bearer |

---

## 5. Posts

| Method | Route | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `POST` | `/posts` | Publish a new post | Bearer |
| `GET` | `/posts/{id}` | Retrieve post by ID (enforces visibility) | Bearer |
| `PUT` | `/posts/{id}` | Edit owned post | Bearer |
| `DELETE` | `/posts/{id}` | Soft-delete post | Bearer |
| `GET` | `/users/{userId}/posts` | List posts by user (enforces visibility) | Bearer |

---

## 6. Feed

| Method | Route | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `GET` | `/feed` | Retrieve My Network feed (own posts, accepted connections, follows; excludes blocked; respects visibility) | Bearer |
| `GET` | `/feed/discover` | Retrieve Discover feed (public published posts, bilateral block excluded) | Bearer |

### Query Parameters
- `limit` (optional, integer): Number of posts per page. Default: `20`, Min: `1`, Max: `50`.
- `cursor` (optional, string): Opaque URL-safe base64 keyset cursor returned by `nextCursor` from the previous page.

### Response Payload (`FeedResponseDto`)
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "authorUserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "authorDisplayName": "Dr. Marie Curie",
      "authorAvatarUrl": null,
      "content": "Excited to share our latest findings in crystallography!",
      "visibility": "Public",
      "status": "Published",
      "createdAtUtc": "2026-11-03T08:00:00Z",
      "publishedAtUtc": "2026-11-03T08:00:00Z",
      "editedAtUtc": null,
      "attachments": []
    }
  ],
  "nextCursor": "MXw2Mzg5Nzc4MjQwMDAwMDAwMDB8M2ZhODVmNjQtNTcxNy00NTYyLWIzZmMtMmM5NjNmNjZhZmE2fDNhNmY0ZTI4OGQ2NGYwY2E4OWQyMzE0ZmVmOGExYTFhYzg2Y2UzZTlmMGY4Nzg3YTcyMjE3MWNkMDdmOTM4NmU",
  "hasMore": true
}
```
