# Stub File Format

## Template

```markdown
<!-- Source: {filename} | Lines: {start}–{end} -->
# Stub Spec Plan: {N} — {Ambito}

**Area**: {ambito}
**Source**: `{filename}` lines {start}–{end}

## Prompt for spec-generator

{prompt}
```

## Fields

| Field | Rule |
|-------|------|
| `{filename}` | Name of the attached source document |
| `{start}–{end}` | Approximate line range in source document |
| `{N}` | 1-based integer; sequential across all stubs |
| `{ambito}` | Lowercase area name (see `area-taxonomy.md`) |
| `{prompt}` | 5–10 sentences; self-contained; same language as source |

## Naming Convention

```
{N}-STUB-SPEC-PLAN-{ambito}.md
```

Examples: `1-STUB-SPEC-PLAN-database.md`, `3-STUB-SPEC-PLAN-frontend.md`

## Rules

- Save all stubs in `/specs/`.
- Prompt MUST be self-contained (no external context required by spec-generator).
- No implementation detail — describe *what*, not *how*.
- Match source document language exactly.
