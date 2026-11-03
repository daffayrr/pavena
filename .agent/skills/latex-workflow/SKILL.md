---
name: latex-workflow
description: Implement Pavena's collaborative LaTeX editor, source management, compilation pipeline, PDF preview, and secure isolation.
---

# LaTeX Workflow Skill

## Product requirements

LaTeX is a separate document mode, not a formatting toggle inside WYSIWYG. Store source files and project structure using a defined schema.

MVP capabilities:
- LaTeX source editor with syntax highlighting.
- Basic multi-file project support if scope permits.
- Collaborative source editing.
- Compile to PDF.
- Preview compilation output and errors.
- Download PDF and source archive.
- Persist compiler version and build metadata.

## Editor

A code editor such as CodeMirror can provide source editing. Yjs may synchronize the source text, but define how multiple files are represented and synchronized before implementation.

Use explicit file records:
- Document/project ID.
- Relative path.
- File role (main, bibliography, image, style, other).
- Content or object-storage key.
- Version metadata.

Prevent path traversal and reject absolute paths.

## Compilation security

LaTeX compilation executes complex tooling and must be isolated:
- Run in a disposable sandbox/container or dedicated restricted worker.
- Disable outbound network access.
- Use non-root user.
- Use read-only base filesystem where possible.
- Provide a dedicated temporary working directory.
- Apply CPU, memory, process, file-size, and wall-clock limits.
- Restrict shell escape and dangerous compiler options.
- Do not mount application secrets or database credentials.
- Restrict allowed input file types and paths.
- Clean up temporary files after completion.
- Treat compiler logs and generated files as untrusted.

Never compile user source inside the ASP.NET Core web process or on the host with unrestricted permissions.

## Build workflow

1. User requests compile.
2. Backend verifies document permission and rate limit.
3. Backend creates a build job with immutable source revision.
4. Worker retrieves only authorized files.
5. Sandboxed compiler runs with resource limits.
6. Worker stores PDF/log artifacts in object storage.
7. Backend records status, compiler version, revision, checksum, and artifact key.
8. User receives notification and can preview/download if authorized.

## Reproducibility

Record:
- Source revision ID.
- Compiler engine/version.
- Template/package profile.
- Build timestamp.
- Exit status.
- Artifact checksum.

Do not promise identical output across arbitrary TeX package versions unless the build environment is pinned.

## MVP scope control

Start with a constrained, known TeX distribution and supported templates. Do not offer arbitrary package installation or unrestricted shell escape in MVP.
