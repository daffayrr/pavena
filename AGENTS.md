# Pavena Agent Instructions

Before working on Pavena:
1. Read `.agent/skills/pavena-core/SKILL.md`.
2. Read the relevant module skills under `.agent/skills/`.
3. Inspect the existing repository and preserve established conventions.
4. Do not assume the recommended folder structure already exists.
5. Do not implement speculative features outside the requested scope.

Project baseline:
- Individual-first academic/professional social network.
- Open registration; organization affiliation optional.
- User-to-user connections and one-way follows.
- Platform Superadmin and Platform Admin are distinct from organization roles.
- Research workspaces with explicit membership.
- WYSIWYG and LaTeX are separate document modes.
- ASP.NET Core / C# + EF Core.
- MySQL initially; PostgreSQL is the planned target.
- Modular monolith; Docker Compose for initial deployment.
- Yjs-based collaboration is the intended direction; validate implementation choices.

Implementation expectations:
- Inspect before editing.
- Plan non-trivial changes.
- Keep domain/application/infrastructure boundaries clear.
- Enforce authorization server-side for every protected resource.
- Add tests and update docs with code changes.
- Avoid adding infrastructure without an ADR and a demonstrated need.
- Never expose or commit secrets.
- Report changed files, tests run, and limitations honestly.
