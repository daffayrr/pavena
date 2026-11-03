---
name: research-workspaces
description: Implement Pavena research projects, teams, invitations, tasks, milestones, files, and collaboration lifecycle.
---

# Research Workspaces Skill

## Purpose

A research workspace brings users together around a research objective. A workspace can be owned by an individual or an organization. Membership is explicit and separate from social connections.

## Project fields

Recommended fields:
- ID.
- Title.
- Abstract/description.
- Research field / subject tags.
- Owner user OR owner organization (exactly one).
- Visibility: private, invited, public-discovery.
- Lifecycle status: draft, active, completed, archived.
- Start/end dates.
- Created/updated timestamps.

Do not conflate visibility with lifecycle status.

## Membership

Suggested project roles:
- Owner.
- ProjectManager.
- Member.
- Supervisor.
- Reviewer.
- Observer.

Keep role definitions scoped to a project. A user's global profile classification must not automatically grant project permissions.

Membership lifecycle:
- Invited.
- Requested (if open applications are supported).
- Active.
- Declined.
- Removed.
- Left.

Define who can invite, approve, remove, and change roles.

## Invitations

- Invitation identifies inviter, invitee, project, role offered, expiry, and status.
- Invitations must be idempotent or prevent duplicates.
- Acceptance must verify the invitee identity.
- Do not grant access before acceptance.
- Notify invitee without exposing private project content beyond the invitation preview.

## Tasks and milestones

MVP task fields:
- Project.
- Title/description.
- Assignee (optional).
- Due date (optional).
- Status.
- Priority.
- Creator.
- Completion timestamp.

Milestones represent project-level checkpoints, not ordinary tasks. Keep them distinct if progress reporting depends on them.

## Files

- Store binary content in object storage.
- Store metadata, object key, checksum, size, content type, uploader, and project relation in DB.
- Verify project access before issuing download URLs.
- Use short-lived signed URLs.
- Do not trust file extensions or browser MIME types.

## Document relationship

A project can contain many documents. A document can optionally belong to a project. Project membership must not silently bypass document-level permission unless an explicit inheritance policy is designed and tested.

## MVP lifecycle

1. Create project.
2. Invite collaborators.
3. Accept invitations.
4. Assign tasks.
5. Create documents.
6. Collaborate and review.
7. Mark project completed or archived.

## Acceptance criteria

- Project has exactly one owner.
- Only authorized members can view private project details.
- Only permitted roles can invite/manage members.
- Removed members lose access immediately, including collaboration WebSocket access.
- Task updates are audited where useful.
- Project visibility and membership are enforced server-side.
