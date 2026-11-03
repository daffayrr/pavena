# ADR-0013: Integrated Research Document Workspace

## Status
Accepted

## Date
2026-11-03

## Context
Pavena connects researchers and industry partners in collaborative research workspaces (`ResearchProject`). Scientific manuscripts, grant proposals, literature reviews, and research reports must belong to a project workspace while supporting a structured multi-file directory hierarchy (e.g., sections, figures, tables, bibliographies).

## Decision
1. **Document Ownership & Hierarchical Structure:**
   - Every `ResearchDocument` belongs to a parent `ResearchProject` and has an individual creator (`CreatedByUserId`).
   - Each document contains multiple `DocumentFile` records structured with relative paths (e.g., `main.tex`, `sections/introduction.tex`, `references.bib`).
   - One file is explicitly designated as `IsMainFile` for compilation entry point.
2. **Path Sanitization & Isolation:**
   - All relative paths are sanitized against directory traversal (`../`, absolute paths, symlink escapes).
   - Document files reside under a unique document root in private object storage.

## Consequences
- **Positive:**
  - True multi-file academic authoring resembling professional LaTeX/Overleaf projects.
  - Strong encapsulation within the research project workspace.
- **Trade-off:**
  - Requires relational mapping for file hierarchies and atomic operations for file renaming/moving.
