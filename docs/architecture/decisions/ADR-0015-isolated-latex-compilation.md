# ADR-0015: Isolated and Sandboxed LaTeX Compilation Architecture

## Status
Accepted

## Date
2026-11-03

## Context
Compiling LaTeX involves executing Turing-complete macro engines (`pdflatex`, `xelatex`, `lualatex`, `bibtex`) that can be abused for arbitrary command execution (`\write18`), file system reading (`\openin`), infinite loops, and resource exhaustion attacks.

## Decision
1. **Out-of-Process Sandbox Execution:**
   - LaTeX compilation is strictly forbidden within the ASP.NET Core web process.
   - A dedicated compilation worker service executes compilation in disposable, isolated workspaces.
2. **Hardened Security Parameters:**
   - Always run with `-no-shell-escape` flag.
   - Run as an unprivileged non-root operating system account.
   - Outbound and inbound network access are completely disabled during compilation.
   - Working directories are created dynamically under an isolated scratch space (`/tmp/pavena-compiler/{jobId}/`) with zero access to application source code or credentials.
3. **Strict Resource Quotas & Timeouts:**
   - Maximum execution time: 30 seconds (hard process kill on timeout).
   - Maximum memory: 512 MB.
   - Maximum output PDF size: 50 MB; maximum compiler log: 5 MB.
4. **Artifact Protection:**
   - Output PDFs and diagnostic logs are immediately uploaded to private object storage and the local scratch workspace is securely wiped.

## Consequences
- **Positive:**
  - Robust protection against Remote Code Execution (RCE) and host compromise.
  - Reproducible compilation environment with recorded compiler versions and build flags.
- **Trade-off:**
  - Requires pre-installed TeX Live distribution and worker process orchestration.
