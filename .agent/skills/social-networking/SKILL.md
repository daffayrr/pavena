---
name: social-networking
description: Build Pavena's LinkedIn-like academic social graph, profiles, connections, follows, feed, posts, comments, and discovery.
---

# Social Networking Skill

## Product intent

The social graph is central to Pavena. Users connect directly with other users regardless of whether they belong to an institution. Organizations have their own profiles and followers, but do not replace individual identities.

## Profile capabilities

MVP profile fields:
- Display/full name.
- Username/handle.
- Headline.
- Biography.
- Avatar and optional cover image.
- Country and city/region.
- Preferred language.
- Education history.
- Work/research experience.
- Skills and research interests.
- Optional organization affiliations.
- Website and public scholarly identifiers where appropriate.
- Profile visibility.

Do not require a university email or institutional affiliation for registration.

## Connection model

Connection is mutual:
- User A sends request to User B.
- B accepts or rejects.
- A may cancel a pending request.
- Either side may remove an accepted connection.
- Prevent self-connections.
- Prevent duplicate or reverse duplicate pairs.
- Define behavior for repeated requests and blocked users.

Store the canonical pair or enforce a unique normalized pair. Keep requester/addressee information for pending requests.

## Follow model

Follow is one-way and independent of connection:
- A follows B without approval unless the product later adds private accounts.
- A can follow an organization.
- Unfollow is idempotent.
- Following does not grant access to private content.

## Feed

For MVP, implement a straightforward chronological feed before ranking algorithms:
- Posts by the user and followed/connected accounts according to visibility policy.
- Organization posts if followed or publicly visible.
- Pagination using a stable cursor (timestamp + ID).
- Filters may include posts, research updates, and publications.
- Respect block, privacy, moderation, and deleted content rules.

Do not expose private project names or document metadata through feed cards.

## Posts and comments

Support:
- Text posts.
- Optional attachments.
- Comments.
- Reactions.
- Edit/delete rules.
- Report content.
- Visibility: public, connections, or private where appropriate.

Sanitize rich text and validate attachment metadata. Never render untrusted HTML directly.

## Search and discovery

MVP search:
- Users by name/username.
- Organizations by name/type/country.
- Skills/research interests where feasible.

Use database indexes and normalized search fields initially. Do not add a dedicated search engine until scale or relevance requirements justify it.

## Messaging

If direct messaging is included in the MVP:
- Define conversations and participants.
- Enforce participant-only reads.
- Handle block and report behavior.
- Rate-limit message creation.
- Avoid leaking read receipts or online status against privacy settings.

If time is constrained, defer full messaging and ship invitations/comments/notifications first.

## UI guidance

Use a familiar professional-network layout:
- Main feed.
- Search/discovery.
- Profile pages.
- Connections.
- Organization pages.
- Notifications.

Avoid cloning LinkedIn branding or exact UI. Pavena needs its own design language and accessible responsive behavior.
