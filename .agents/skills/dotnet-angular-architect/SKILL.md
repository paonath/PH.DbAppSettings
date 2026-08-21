---
name: dotnet-angular-architect
description: |
  Plans, scaffolds, and maintains C# Minimal API (.NET) + Angular solutions following the project's layered architecture template. Produces a solution-specific blueprint document with all resolved project names, technology decisions, AGENTS.md content for every project layer, and a per-entity implementation checklist.
  Use when: (1) starting a new C#/.NET Minimal API + Angular solution from scratch, (2) adding new entities or features to an existing .NET + Angular solution, (3) reviewing or auditing architecture consistency, (4) invoked by spec-generator to provide architecture context before writing a spec, (5) invoked by spec-executor to retrieve layer conventions during implementation of a spec.
---

# .NET + Angular Solution Architect Skill

## Overview

Instantiates the MinimalAPI + Angular architecture template into a concrete, solution-specific blueprint.

**Architecture template (canonical):** `./references/solution-architecture-template.md`

**Inputs required:**
- `{SolutionName}` — root name, PascalCase, no spaces
- Entity list — PascalCase names of all DB-mapped entities (comma-separated)
- Technology decisions — .NET version, Angular version, database engine, ORM strategy, auth provider

**Output:** `specs/{solution-name}-blueprint.md` — the solution blueprint document.

---

## Step 1: Collect Inputs

Before running prompts or scanning manually, use TokenSave MCP tools (specifically `tokensave_search`, `tokensave_files`, or `tokensave_context`) to inspect the project layout, check existing dependencies, and list database entities. This makes verification fast and accurate.

Extract the following from existing context (spec file, codebase, blueprint). For any item not already present, use the `qa` skill (`.agents/skills/qa/SKILL.md`) to collect it interactively — one question at a time.


1. **SolutionName** — PascalCase (e.g. `Portal`, `MyApp`)
2. **Entity list** — PascalCase DB entity names (e.g. `Project`, `User`, `Invoice`)
3. **Technology decisions:**
   - .NET target version
   - Angular target version
   - Database engine + version (e.g. MySQL 8, PostgreSQL 16)
   - ORM strategy: EF Core only / EF Core + Dapper
   - Auth provider: Microsoft Entra ID / none / other

When invoked by `spec-generator` or `spec-executor`, extract all inputs from the spec file or existing blueprint rather than prompting the user.

---

## Step 2: Resolve Template Tokens

Load `.agents/templates/solution-architecture-template.md` and apply token resolution:

| Template token | Resolution rule |
|---------------|-----------------|
| `{SolutionName}` | Input SolutionName verbatim |
| `{Entity}` | Expand once per entity in the entity list |
| `{Dto}` | `{Entity}Dto` (read model) · `{Entity}CreateDto` (write model) |
| `{Service}` | `I{Entity}Service` — interface, lives in `.Services` |
| `{ServiceImpl}` | `{Entity}Service` — class, lives in `.Services.Components` |
| `{entity}` | lowercase-hyphenated form of Entity name (for Angular paths) |

Validate the dependency rules from Section 0.1 of the template before proceeding. Flag any violation.

---

## Step 3: Generate AGENTS.md Content

Use `./references/agents-md-guide.md` to produce AGENTS.md content for each project layer.

Rules:
- Substitute all template tokens with resolved values
- List concrete entity names explicitly in Dal, Models, Services, and Services.Components sections
- Keep each AGENTS.md ≤ 60 lines

AGENTS.md files are placed at the root of each project folder (e.g. `{SolutionName}.Dal/AGENTS.md`). Also create a root-level `AGENTS.md` for the repository summarising the solution layout.

---

## Step 4: Produce the Blueprint Document

If the generated blueprint or references are very large (>150 lines), use `headroom_compress` to compress the content and manage context window space.

Generate `specs/{solution-name}-blueprint.md` using `./references/solution-output-template.md`.


Required sections — do not omit any:

1. **Solution summary** — SolutionName, tech stack table, entity list
2. **Resolved project map** — all project names with namespaces
3. **Dependency diagram** — Section 0.1 with actual project names substituted
4. **AGENTS.md content** — verbatim populated content for all 6 server projects (and root)
5. **Per-entity implementation checklist** — 13-step table from the template, expanded per entity
6. **Build & test commands** — with actual project paths resolved.
   - For all `dotnet` CLI usage (project creation, build, publish, EF Core migrations, NuGet packages, User Secrets), follow the project's .NET CLI conventions (see `dotnet-cli-usage.md` rule or `AGENTS.md`).
   - For all Angular CLI usage (`ng new`, `ng generate component`, `ng serve`, `ng build`), follow the project's Angular CLI conventions (see `angular-cli-usage.md` rule or `AGENTS.md`). When generating components, always apply `--inline-template --inline-style --standalone`; determine `--prefix` from `angular.json`.

If `specs/{solution-name}-blueprint.md` already exists, update only the sections affected by new entities or changed technology decisions. Never overwrite AGENTS.md sections that were manually customised.

---

## Integration with spec-generator

When invoked by `spec-generator`:

1. Run Steps 1–4 to create or load the blueprint
2. Include the blueprint path in the spec's **Context** section
3. Add the AGENTS.md content for the target layer(s) as **Constraints** in the spec

## Integration with spec-executor

When invoked by `spec-executor`:

1. Load `specs/{solution-name}-blueprint.md`
2. Identify the project layer(s) the spec touches
3. Extract the AGENTS.md for those layers — treat "Does NOT belong here" as hard constraints
4. Use the per-entity checklist to verify no artifact is missing after implementation

---

## References

- AGENTS.md templates per layer: `./references/agents-md-guide.md`
- Blueprint output template: `./references/solution-output-template.md`
- Instantiated example: `./examples/portal-blueprint.md`
- Architecture template (canonical): `./references/solution-architecture-template.md`
- **dotnet CLI conventions**: see `dotnet-cli-usage.md` rule or `AGENTS.md` — authoritative rules for all `dotnet` CLI commands: project creation (including `-f <tfm>` per project), build, publish, EF Core migrations, NuGet packages, User Secrets, and tool management.
- **Angular CLI conventions**: see `angular-cli-usage.md` rule or `AGENTS.md` — authoritative rules for all `ng` CLI commands: workspace setup, `ng generate component` (with mandatory `--inline-template`, `--inline-style`, `--standalone`), other schematics (service, directive, pipe, guard), `ng serve`, `ng build`, `ng test`.
