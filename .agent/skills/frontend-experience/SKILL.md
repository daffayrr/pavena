---
name: frontend-experience
description: Design Pavena's responsive, accessible academic social network and research workspace UI with a polished, non-generic visual system.
---

# Frontend Experience Skill

## Product experience

Pavena should feel like a modern professional network combined with a focused research workspace. It should not look like a generic admin template across every page.

Primary navigation concepts:
- Home / Feed
- My Network
- Discover
- Organizations
- Research
- Documents
- Notifications
- Profile

Platform administration must have a separate admin interface and navigation.

## Visual principles

- Professional, calm, research-oriented, and contemporary.
- Clear typographic hierarchy.
- Consistent spacing and alignment.
- Restrained color palette with meaningful semantic colors.
- Strong content readability for long-form academic text.
- Avoid excessive gradients, glassmorphism, oversized rounded cards, decorative blobs, and generic AI-generated dashboard patterns.
- Do not copy LinkedIn's logo, exact branding, or proprietary visual identity.
- Establish design tokens for color, typography, spacing, radii, shadows, and breakpoints.
- Prefer reusable components over duplicated markup.

## Core screens

1. Registration/login and onboarding.
2. Home feed.
3. User profile.
4. Search/discovery.
5. Connection requests and network.
6. Organization profile.
7. Research project overview.
8. Project member/task workspace.
9. Document list.
10. WYSIWYG editor.
11. LaTeX editor with source/preview layout.
12. Notifications.
13. Platform admin dashboard.

## Editor UX

- Keep editing canvas visually focused.
- Make collaborator presence clear but unobtrusive.
- Provide clear save/sync status.
- Distinguish offline, syncing, saved, and error states.
- Avoid claiming "saved" before durable acknowledgement.
- Keep comments and version history accessible without obscuring the document.
- Warn before destructive actions.
- Preserve keyboard shortcuts and accessible controls.

## Accessibility

- Use semantic HTML.
- Ensure keyboard navigation and visible focus.
- Label form controls.
- Provide accessible validation/error messages.
- Maintain sufficient contrast.
- Do not communicate status by color alone.
- Respect reduced-motion preferences.
- Test common viewport sizes and zoom.

## Internationalization

- Use resource-based localization; do not hardcode user-facing strings in components.
- Support Indonesian and English initially.
- Avoid assuming names are first-name/last-name pairs.
- Format dates, numbers, and time zones according to user locale.
- Ensure layouts accommodate longer translated strings.

## Frontend security

- Treat all user content as untrusted.
- Avoid unsafe HTML rendering.
- Do not place secrets or privileged decisions in frontend code.
- Do not use client-side role checks as security.
- Handle expired sessions and forbidden resources gracefully.
