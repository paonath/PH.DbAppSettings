---
name: agent-self-learning
description: Manages durable project knowledge via Lessons (.agents/Lessons) and Memories (.agents/Memories) using versioned, deduplicated, governable patterns.
trigger: agent-self-learning, lesson, learn, record lesson, memory, retro, mistake
---

# Agent Self-Learning System

Maintain project learning artifacts under `.agents/Lessons` and `.agents/Memories`.

## Artifact Types

| Type | Location | Trigger |
|---|---|---|
| Lesson | `.agents/Lessons/` | A mistake was made and corrected |
| Memory | `.agents/Memories/` | Durable architectural fact or constraint discovered |

## Governance Rules

Apply **before** creating, updating, or reusing any artifact:

1. **Versioned Patterns** — every artifact must include `PatternId`, `PatternVersion`, `Status`, `Supersedes`.
2. **Pre-Write Dedupe** — search existing artifacts (use TokenSave MCP tools like `tokensave_search` or `tokensave_context` for fast querying) for similar root cause, decision, and scope; update existing instead of creating a duplicate.
3. **Conflict Resolution** — if new evidence conflicts with an `active` pattern: mark the older as `deprecated` (or `blocked` if unsafe), create the replacement with `Supersedes` set, notify the user.
4. **Safety Gate** — never apply or recommend patterns with `Status: blocked`; reactivation requires validation evidence and explicit user confirmation.
5. **Reuse Priority** — prefer the newest validated `active` pattern; ask the user before applying if confidence is low.
6. **Context Optimization** — if reviewing a large volume of past logs, lessons, or code areas, use `headroom_compress` to compress the history and avoid cluttering the context window.


Allowed `Status` values: `active` | `deprecated` | `blocked`

## Lessons: When and How

### Triggers
- Incorrect output was generated and corrected
- A tool call failed in an unexpected or repeatable way
- A process step was skipped or mis-ordered

### Required Sections
- **Metadata**: PatternId, PatternVersion, Status, Supersedes, CreatedAt, LastValidatedAt, ValidationEvidence
- **Task Context**: triggering task, date/time, impacted area
- **Mistake**: what went wrong, expected vs actual behavior
- **Root Cause**: primary cause, contributing factors, detection gap
- **Resolution**: fix implemented, why it works, verification performed
- **Preventive Actions**: guardrails added, tests/checks, process updates
- **Reuse Guidance**: how to apply this lesson in future tasks

Template: `examples/lesson-template.md`

## Memories: When and How

### Triggers
- An architectural constraint is confirmed
- A key platform-specific behavior is validated
- A recurring pitfall pattern is identified
- A design decision should inform all future work in the area

### Required Sections
- **Metadata**: PatternId, PatternVersion, Status, Supersedes, CreatedAt, LastValidatedAt, ValidationEvidence
- **Source Context**: triggering task, scope/system, date/time
- **Memory**: key fact or decision, why it matters
- **Applicability**: when to reuse, preconditions/limitations
- **Actionable Guidance**: recommended future action, related files/services/components

Template: `examples/memory-template.md`

## Subagent Contract

Every subagent brief MUST include instruction to follow this contract.
Every subagent MUST append this block at the end of its final response:

```
LessonsSuggested:
- <title>: <reason>   (or "none")

MemoriesSuggested:
- <title>: <reason>   (or "none")

ReasoningSummary:
- <concise rationale for decisions, trade-offs, and confidence>
```

The **orchestrating agent** is responsible for:
- Consolidating and deduplicating subagent suggestions
- Finalizing artifacts in `.agents/Lessons/` and `.agents/Memories/` before task completion
- Informing the user of any deprecated or blocked pattern changes

## File Naming

- Lessons: `YYYY-MM-DD-<short-kebab-title>.md` in `.agents/Lessons/`
- Memories: `<short-kebab-title>.md` in `.agents/Memories/`
- PatternId: `LESSON-NNN` or `MEMORY-NNN` (sequential, zero-padded to 3 digits)

## Decision Tree

```
New information available
├── Was a mistake made?
│   ├── Yes → Create or update Lesson
│   └── No  → continue
└── Is this durable architecture / constraint knowledge?
    ├── Yes → Create or update Memory
    └── No  → No artifact needed
```

When creating or updating either artifact type:
1. Search existing artifacts (dedupe check).
2. Found a close match? → Update it, increment `PatternVersion`.
3. Found a conflict? → Deprecate old, create new with `Supersedes`.
4. Not found? → Create new file with `PatternVersion: 1`.
