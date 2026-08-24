---
name: spec-split-plan
description: |
  Splits a large Markdown document into atomic stub spec plan files ready for spec-generator.
  Use when: (1) a user attaches a large requirements doc and asks to split it into specs,
  (2) a user says "split this document into specs", (3) a complex analysis document needs
  decomposing into separate implementable areas before spec generation.
---

# spec-split-plan

Split a large Markdown document into numbered stub spec plan files (`{N}-STUB-SPEC-PLAN-{ambito}.md`), each ready for `spec-generator`.

## Prerequisites

- One Markdown document attached to context. If missing → stop and ask.
- Read `prompt-clarifier` SKILL before proceeding.
- **Large Document Context**: If the attached Markdown document is large (>150 lines), use `headroom_compress` to compress the content and manage context window space during analysis.

## Workflow


```
1. Run prompt-clarifier on user prompt
2. Verify attachment (missing → stop)
3. Analyse document → identify logical areas + line ranges
4. Present proposed split to user (table: N | area | lines | description)
5. Refine loop: one question per turn until user approves
6. Generate one stub file per approved area → /specs/
7. Summarise output
```

## Decision Tree — Identify Areas

Scan document for content matching these domains:

| Area (`ambito`) | Trigger keywords |
|----------------|-----------------|
| `database` | schema, table, entity, migration, model |
| `backend` | API, endpoint, service, controller, business logic |
| `frontend` | component, UI, page, form, Angular, React |
| `testing` | test, acceptance criteria, validation, QA |
| `documentation` | doc, README, guide, manual |

Use content semantics when keywords are absent. Present your reasoning.

## Stub File Format

```markdown
<!-- Source: {filename} | Lines: {start}–{end} -->
# Stub Spec Plan: {N} — {Ambito}

**Area**: {ambito}
**Source**: `{filename}` lines {start}–{end}

## Prompt for spec-generator

{5–10 sentences. Self-contained. Same language as source doc.}
```

Save to `/specs/{N}-STUB-SPEC-PLAN-{ambito}.md`.

## Iterative Session Rules

Use the `qa` skill (`.agents/skills/qa/SKILL.md`) for the refinement loop:

- Checklist item: *"Does the proposed split cover all areas correctly?"*
- Present split as a table (N, area, lines, 1-line description).
- Ask exactly **one** question per turn with at least one suggested answer and a free-answer option.
- Loop until explicit approval or user scopes down.
- On approval: generate files, then list them.

## Edge Cases

| Situation | Action |
|-----------|--------|
| No section breaks | Infer from content semantics; explain reasoning |
| User wants subset only | Generate only approved areas |
| Mixed languages | Use dominant language; note inconsistency in stub header |
| Doc < 20 lines | Warn: may not need splitting; offer single stub |
| Name collision in `/specs/` | Warn user; ask overwrite or rename |

## Integration

Stubs feed directly into `spec-generator`. Each stub is self-contained — no extra context needed. See `references/stub-file-format.md` for full field definitions and `references/area-taxonomy.md` for the complete area list.
