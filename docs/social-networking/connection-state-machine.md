# Connection State Machine & Lifecycle

Pavena's connection system manages mutual user-to-user relationships.

---

## 1. Domain Invariants

1. **Anti-Self Connection:** A user cannot connect with themselves (`RequesterUserId <> AddresseeUserId`).
2. **Canonical Pair Uniqueness:** The unordered pair is canonicalized as `CanonicalUserA = min(User1, User2)` and `CanonicalUserB = max(User1, User2)`. A database unique constraint on `(CanonicalUserA, CanonicalUserB)` guarantees that only one relationship record can exist for any two users.
3. **Actor Authorization:**
   - Only the **Addressee** may accept or reject an incoming request.
   - Only the **Requester** may cancel an outgoing request.
   - Either party may terminate/remove an active connection.
4. **Safety & Blocking:** If either user has blocked the other, connection requests are blocked at the application level.

---

## 2. State Machine Diagram

```
                 [Initial: None]
                        │
                        │ User A requests User B
                        ▼
               ┌─────────────────┐
               │     Pending     │
               └─────────────────┘
                  │      │      │
      B accepts   │      │      │ B rejects
   ┌──────────────┘      │      └──────────────┐
   │                     │ A cancels           │
   ▼                     ▼                     ▼
┌──────────────┐  ┌──────────────┐      ┌──────────────┐
│   Accepted   │  │  Cancelled   │      │   Rejected   │
└──────────────┘  └──────────────┘      └──────────────┘
   │                     │                     │
   │ Either removes      │ User A or B         │ User A or B
   │ or blocks           │ re-requests         │ re-requests
   ▼                     ▼                     ▼
[Deleted / None] ◄─────────────────────────────┘
```

---

## 3. Transition Rules & API Actions

| Current State | Trigger Action | Authorized Actor | Resulting State | Notes |
| :--- | :--- | :--- | :--- | :--- |
| **None** | `POST /connections/requests` | Requester | `Pending` | Creates new row with canonical pair |
| **Pending** | `POST /connections/requests/{id}/accept` | Addressee | `Accepted` | `RespondedAtUtc` recorded |
| **Pending** | `POST /connections/requests/{id}/reject` | Addressee | `Rejected` | `RespondedAtUtc` recorded |
| **Pending** | `DELETE /connections/requests/{id}` | Requester | `Cancelled` | `RespondedAtUtc` recorded |
| **Accepted** | `DELETE /connections/{userId}` | Either user | `Deleted (None)` | Row deleted; users become unconnected |
| **Rejected** | `POST /connections/requests` | Either user | `Pending` | Re-request reactivates row |
| **Cancelled**| `POST /connections/requests` | Either user | `Pending` | Re-request reactivates row |
| **Any** | Block action | Either user | `Deleted (None)` | Immediate mutual severing |
