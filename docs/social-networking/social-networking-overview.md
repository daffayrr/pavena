# Social Networking Overview

Pavena combines academic networking, professional profiles, research collaboration, and scientific document editing into an integrated platform.
The social graph establishes the foundation for academic discovery, peer communication, and scholarly collaboration.

---

## 1. Core Principles

1. **Individual-First Identity:** Every user registers and maintains an independent profile. Institutional affiliation (universities, research labs, companies) is optional and non-exclusive.
2. **Mutual Connections vs. Asymmetric Following:**
   - **Connections:** Require mutual consent (Request -> Accept). Establishes a reciprocal peer relationship. Unordered pair uniqueness guarantees no duplicates.
   - **Following:** Directional and unreciprocated (A follows B). Allows users to track publications, announcements, and thoughts of prominent researchers without mutual agreement.
3. **Safety & Blocking:** Immediate mutual severing upon block. Blocks erase all existing connections and follows between the parties, and hide all content in both directions.
4. **Academic Post Publishing:** Scholars publish research updates, discussion queries, and findings with three strictly enforced visibility levels: `Public`, `Connections`, `OnlyMe`.
5. **Deterministic Social Feed:** Chronological, keyset-paginated timeline preventing pagination drift and data leakage.

---

## 2. Module Composition

```
┌──────────────────────────────────────────────────────────┐
│                   Pavena Social System                   │
└──────────────────────────────────────────────────────────┘
      │                    │                    │
┌─────▼──────────┐   ┌─────▼──────────┐   ┌─────▼──────────┐
│ Academic       │   │ Social Graph   │   │ Content & Feed │
│ Profiles       │   │ & Safety       │   │ Publishing     │
├────────────────┤   ├────────────────┤   ├────────────────┤
│ • Display Name │   │ • Connections  │   │ • Posts        │
│ • Bio/Headline │   │ • Follows      │   │ • Visibility   │
│ • Affiliations │   │ • Blocks       │   │ • Keyset Feed  │
│ • Education    │   │ • State Machine│   │ • Moderation   │
│ • Experience   │   │ • Invariants   │   │ • Deletions    │
│ • Links/Skills │   │                │   │                │
└────────────────┘   └────────────────┘   └────────────────┘
```
