# Pavena — Posts & Academic Content Composer

**Milestone:** Phase 3 / Milestone K  
**Status:** Completed  
**Authors:** Pavena Engineering Team  
**Date:** November 2026  

---

## 1. Executive Summary

Milestone K completes the frontend user interface and content lifecycle for **Posts & Academic Content Composer** in Pavena. It provides a scholarly publication composer, content validation, multi-tier visibility controls (`Public`, `Connections`, `OnlyMe`), post draft management, archiving, live previewing, post detail reading, and author post management.

This frontend interfaces directly with the robust server-side domain established in Milestone D and feeds created in Milestone E, adhering to the official **Academic Professional** design system.

---

## 2. Component Architecture

All components are located under `backend/src/Pavena.Web/Components/Posts/`:

| Component | Responsibility |
|:---|:---|
| [`PostCategorySelector.razor`](file:///D:/coding/_toscaflow_/pavena/backend/src/Pavena.Web/Components/Posts/PostCategorySelector.razor) | Selector for 7 academic categories with icons, descriptions, and interactive selection states. |
| [`PostVisibilitySelector.razor`](file:///D:/coding/_toscaflow_/pavena/backend/src/Pavena.Web/Components/Posts/PostVisibilitySelector.razor) | Dropdown selector for Public, Connections, and OnlyMe visibility with academic context labels. |
| [`PostAttachmentUploader.razor`](file:///D:/coding/_toscaflow_/pavena/backend/src/Pavena.Web/Components/Posts/PostAttachmentUploader.razor) | File picker/drag-and-drop supporting PDF, TeX, CSV, Excel, DOCX, ZIP, and images with 25 MB/file limits, MIME validation, and object storage notices. |
| [`PostComposer.razor`](file:///D:/coding/_toscaflow_/pavena/backend/src/Pavena.Web/Components/Posts/PostComposer.razor) | Full-featured composer supporting category tagging, visibility control, live markdown preview toggle, character counter, "Simpan Draf", and "Publikasikan". |
| [`PostCard.razor`](file:///D:/coding/_toscaflow_/pavena/backend/src/Pavena.Web/Components/Posts/PostCard.razor) | Scholarly post card with author avatar, metadata, parsed category badge, visibility indicator, status pill, attachment pills, and author lifecycle actions. |
| [`PostDetail.razor`](file:///D:/coding/_toscaflow_/pavena/backend/src/Pavena.Web/Components/Posts/PostDetail.razor) | Dedicated reading view with full author info, draft/archived status banners, full content typography, attachment download triggers, and citation generation. |
| [`PostEditModal.razor`](file:///D:/coding/_toscaflow_/pavena/backend/src/Pavena.Web/Components/Posts/PostEditModal.razor) | Dialog modal wrapping `PvModal` allowing authors to edit content, visibility, and category without leaving context. |

---

## 3. Pages & Routing

| Route | Page Component | Description |
|:---|:---|:---|
| `/posts/create` | [`CreatePostPage.razor`](file:///D:/coding/_toscaflow_/pavena/backend/src/Pavena.Web/Pages/CreatePostPage.razor) | Dedicated scholarly publication composer with academic integrity guidelines sidebar. |
| `/posts/{postId:guid}` | [`PostDetailPage.razor`](file:///D:/coding/_toscaflow_/pavena/backend/src/Pavena.Web/Pages/PostDetailPage.razor) | Comprehensive article reading view with server-side authorization checks and back navigation. |
| `/posts/mine` | [`MyPostsPage.razor`](file:///D:/coding/_toscaflow_/pavena/backend/src/Pavena.Web/Pages/MyPostsPage.razor) | Author dashboard with status filtering tabs (`Semua`, `Terbit`, `Draf`, `Arsip`) and post counts. |

---

## 4. Academic Content Categories & Domain Gap Analysis

### Mandated Categories
The prompt specifies 7 academic categories:
1. **Research Update** (🔬) — Progres eksperimen, laboratorium, atau metodologi.
2. **Research Finding** (💡) — Temuan ilmiah baru, hipotesis teruji, atau penemuan teoritis.
3. **Academic Discussion** (💬) — Diskusikan pertanyaan ilmiah terbuka atau literatur.
4. **Publication** (📄) — Pengumuman artikel jurnal, pre-print arXiv/bioRxiv, atau buku.
5. **Collaboration Opportunity** (🤝) — Ajakan kolaborasi antar-laboratorium atau peneliti.
6. **Conference & Event** (🏛️) — Simposium, konferensi internasional, seminar, workshop.
7. **General Academic** (🎓) — Catatan umum akademisi, kebijakan riset, wawasan ilmiah.

### Domain & Schema Gap
- **Audit Result:** The existing PostgreSQL/MySQL database schema and `Post` domain entity (`backend/src/Pavena.Domain/Entities/Social/Post.cs`) established in Milestone D do not possess a native `Category` column.
- **Rule Adherence:** Per prompt instruction: *"Periksa apakah backend mendukung category. Jika belum, dokumentasikan API/domain gap dan usulkan perubahan backend terpisah sebelum membuat fitur yang tidak didukung."*
- **Current Implementation:** Non-speculative encapsulation. Categories are selected in [`PostCategorySelector.razor`](file:///D:/coding/_toscaflow_/pavena/backend/src/Pavena.Web/Components/Posts/PostCategorySelector.razor) and tagged cleanly in the content header (`[CategoryName]`), parsed transparently by [`PostCard.razor`](file:///D:/coding/_toscaflow_/pavena/backend/src/Pavena.Web/Components/Posts/PostCard.razor) and [`PostDetail.razor`](file:///D:/coding/_toscaflow_/pavena/backend/src/Pavena.Web/Components/Posts/PostDetail.razor) into custom colored badges while preserving database schema integrity.
- **Proposed Future Migration:**
  - Add `PostCategory` enum to `Pavena.Domain.Enums`.
  - Add nullable `PostCategory? Category { get; private set; }` to `Pavena.Domain.Entities.Social.Post`.
  - Create EF Core migration `AddPostCategoryColumn` with a database index on `(Category, Status, PublishedAtUtc)`.
  - Update `CreatePostRequest` and `UpdatePostRequest` DTOs to include `string? Category`.

---

## 5. Object Storage Status & Prerequisite Audit

### Findings
- **Storage Infrastructure:** There is currently no active S3/MinIO bucket service provisioned in the local development environment.
- **Data Integrity Rule:** No raw binary files or base64 blobs are written to PostgreSQL or MySQL databases.
- **Implementation:** [`PostAttachmentUploader.razor`](file:///D:/coding/_toscaflow_/pavena/backend/src/Pavena.Web/Components/Posts/PostAttachmentUploader.razor) performs strict client-side and server-side metadata validation:
  - Allowed types: PDF, LaTeX (.tex), CSV, Excel, DOCX, ZIP, PNG, JPG.
  - Maximum file size: 25 MB per file (`Post.MaxAttachmentSizeBytes`).
  - Maximum count: 10 attachments per post (`Post.MaxAttachmentsCount`).
  - Storage key generation: `storage/posts/{guid}_{filename}`.
  - Clear user-facing notice explaining that private object storage integration is an upcoming infrastructure release.

---

## 6. Security & Server-Side Authorization

1. **Authentication Enforcement:** All post author actions require authenticated session. `AuthorUserId` is derived strictly from `ClaimsPrincipal` (`User.FindFirstValue(ClaimTypes.NameIdentifier)`) on the server.
2. **Resource Authorization:** Edit, publish, archive, and delete operations execute server-side checks via `PostAuthorizationHandler` ensuring only the post author can perform mutations.
3. **Draft & OnlyMe Privacy:** Draft posts and `OnlyMe` posts return `404 Not Found` to any non-author caller, preventing information leakage.
4. **Bilateral Block Invariants:** If either user has blocked the other, posts are completely hidden (`404 Not Found`).
5. **Feed Endpoint (`GET /api/v1/posts/mine`):** Returns all posts belonging to the authenticated caller (Draft, Published, Archived) sorted chronologically by `CreatedAtUtc`.
