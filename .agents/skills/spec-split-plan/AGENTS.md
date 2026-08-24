# AGENTS.md — spec-split-plan SKILL

## Project Info

- **Location**: `.agents/skills/spec-split-plan/`
- **Type**: AI Assistant SKILL (Pattern 1 — Guidance only)
- **Purpose**: Splits a large Markdown document into atomic stub spec plan files ready for `spec-generator`

## Skill Structure

```
.agents/skills/spec-split-plan/
├── SKILL.md                          # Main workflow instructions
├── examples/
│   ├── source-tour-booking.md        # Sample input document
│   ├── 1-STUB-SPEC-PLAN-database.md
│   ├── 2-STUB-SPEC-PLAN-backend.md
│   ├── 3-STUB-SPEC-PLAN-frontend.md
│   └── 4-STUB-SPEC-PLAN-testing.md
└── references/
    ├── stub-file-format.md           # Template + field definitions
    └── area-taxonomy.md              # Recognised area names
```

## Output Convention

Stub files are saved in `/specs/` as `{N}-STUB-SPEC-PLAN-{ambito}.md`.

## Implementation Notes

- Spec executed: `specs/spec-tool-spec-split-plan-skill.md` (v1.0.0)
- Executed on: 2026-03-25
- Skill uses Pattern 1 (guidance-only, no scripts)
- Registered in the workspace configurations (.agents/AGENTS.md)

