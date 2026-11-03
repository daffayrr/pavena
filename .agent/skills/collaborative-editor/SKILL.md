---
name: collaborative-editor
description: Design and implement Pavena's collaborative WYSIWYG editor using TipTap/ProseMirror and Yjs CRDT, including persistence and permissions.
---

# Collaborative Editor Skill

## Product requirements

Pavena supports two document modes:
1. WYSIWYG rich-text documents.
2. LaTeX source documents.

This skill covers WYSIWYG collaborative editing. Read `latex-workflow` for LaTeX.

## Candidate stack

- TipTap / ProseMirror for editor UI and document schema.
- Yjs for CRDT state and concurrent editing.
- Hocuspocus as a candidate Yjs WebSocket collaboration server.
- ASP.NET Core as the authority for users, documents, permissions, and application workflows.
- Object storage for exported files and attachments.
- Relational DB for metadata, update persistence, snapshots, and version records.

Validate package versions, licensing, deployment model, and integration contracts before implementation.

## Core design

- A document has stable ID, owner, editor type, visibility, schema version, and lifecycle status.
- WYSIWYG content uses a versioned ProseMirror/TipTap JSON schema.
- Yjs state is the collaborative source of truth during editing.
- Persist Yjs updates and periodically compact them into snapshots.
- Keep human-readable document versions separate from raw CRDT update logs.
- Store exports as immutable artifacts with metadata/checksum.
- Do not treat each keystroke as an ordinary REST PUT.

## Collaboration authorization

Before joining a room:
1. Client authenticates with Pavena.
2. Client requests collaboration access for a specific document.
3. ASP.NET Core verifies account status, ownership, visibility, collaborator role, and policy.
4. Server issues a short-lived, scoped collaboration token.
5. Collaboration server validates token and room/document binding.
6. Collaboration server checks revocation/expiry strategy.
7. User joins only the authorized document room.

Never trust a client-supplied room name or user ID. Re-check authorization when membership changes; disconnected/revoked users must lose access.

## Roles

- Owner: manage collaborators, edit, restore snapshots, archive/delete according to retention policy.
- Editor: edit.
- Reviewer: read/comment/suggest according to configured capability.
- Viewer: read only.

Do not infer document access solely from connection or project membership.

## Persistence and recovery

- Persist updates durably.
- Define update ordering and idempotency.
- Use snapshots/compaction to bound replay cost.
- Keep a snapshot version/sequence and integrity metadata.
- Test recovery after server restart and network interruption.
- Define backup/restore for both database updates and object storage artifacts.
- Avoid losing acknowledged edits during graceful shutdown.

## Versioning

Distinguish:
- CRDT update history: operational collaboration state.
- Snapshot: compact state for recovery.
- Named version: user-visible checkpoint such as "Supervisor review".
- Export artifact: PDF/DOCX output at a particular revision.

Provide restore as a controlled operation that creates a new state/version rather than destructively deleting all later history.

## Comments

Comments should reference stable document anchors where possible. Define behavior when anchored text is deleted or substantially changed. Do not store comments as editor body text.

## Performance

- Debounce non-critical metadata updates.
- Batch persistence where safe.
- Use backpressure and connection limits.
- Monitor room count, active sessions, update size, persistence latency, and reconnects.
- Do not introduce Redis until horizontal scaling/presence coordination requires it.

## Tests

Test simultaneous edits, same-range edits, offline/reconnect, server restart, snapshot recovery, permission revocation, expired tokens, unauthorized room access, and large documents.
