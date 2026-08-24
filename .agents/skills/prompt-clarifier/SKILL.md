---
name: prompt-clarifier
description: |
  Pre-analyzes every user prompt before execution to ensure it is actionable. Use when: (1) starting to process any user request in chat, (2) a prompt is ambiguous, incomplete, or has multiple valid interpretations, (3) clarification is needed before taking an irreversible or complex action, (4) re-engineering an unclear prompt iteratively with user feedback.
---

# Prompt Clarifier

Pre-analyze every prompt before execution. Decide immediately whether to proceed or enter a clarification loop.

---

## Phase 1: Quick Analysis (always first)

Evaluate the prompt against these three criteria:

| Criterion | Questions to ask |
|---|---|
| **Goal** | Is there a single, unambiguous desired outcome? |
| **Scope** | Are the affected files, projects, or systems clear? |
| **Constraints** | Are there conflicting requirements or missing context that would block execution? |

**Decision**:
- All three criteria pass → **Phase 2A: Proceed**
- Any criterion fails → **Phase 2B: Clarification loop**

---

## Phase 2A: Clear Prompt — Proceed

1. Perform deep analysis of the prompt (intent, risks, affected components).
2. Identify and load any other relevant skills.
3. Execute the request.

---

## Phase 2B: Unclear Prompt — Clarification Loop

### Step 1: Create clarification file

Create a markdown file named `clarification-{topic}.md` in the workspace root (or in a `_clarifications/` subfolder if one exists). Use a short topic slug derived from the prompt (e.g. `clarification-add-user-endpoint.md`).

Use this template:

```markdown
# Clarification: {short topic}

## Original Prompt

> {paste the original prompt verbatim}

## Ambiguities Identified

- {ambiguity 1}
- {ambiguity 2}

---

## Iteration 1

### Question

{specific clarification question for the user}

### User Response

{filled in after user replies}

### Re-engineered Prompt

{updated prompt after incorporating the response}

### Status

[ ] Still unclear — see Iteration 2
[x] Clear — ready to proceed
```

### Step 2: Ask the user

Ask **one focused question** that resolves the most critical ambiguity. Do not ask multiple questions at once.

### Step 3: Incorporate the response

Edit the clarification file (do not create a new one):
- Fill in `### User Response` under the current iteration.
- Write the `### Re-engineered Prompt` incorporating the clarification.
- Update `### Status`.

### Step 4: Re-evaluate

Apply Phase 1 criteria to the re-engineered prompt:
- **Clear** → mark `[x] Clear` in the file and move to **Phase 2A**.
- **Still unclear** → add a new `## Iteration N` block to the same file and repeat from Step 2.

---

## Rules

- **Never create a new clarification file per iteration** — always edit the existing one.
- **One question per iteration** — do not bundle multiple questions.
- **Preserve the original prompt verbatim** in the file; only the re-engineered prompt is modified.
- **Stop iterating after 3 rounds** — if still unclear after 3 iterations, proceed with the best available interpretation and document the assumption in the clarification file under a `## Assumptions` section.
- **Do not create a clarification file for trivial prompts** — simple, single-action requests (e.g. "what is X?", "list files in Y") require no clarification file.

---

## Clarification File Naming

| Prompt topic | File name |
|---|---|
| "add a login endpoint" | `clarification-add-login-endpoint.md` |
| "refactor the service layer" | `clarification-refactor-service-layer.md` |
| "generate DTO for User" | `clarification-generate-user-dto.md` |

---

## Decision Summary

```
Receive prompt
    │
    ▼
Phase 1: Quick Analysis
    │
    ├─ CLEAR ──────────────────────────► Phase 2A: Proceed
    │
    └─ UNCLEAR
           │
           ▼
     Create clarification-{topic}.md
     Ask one focused question
           │
           ▼
     User responds
     Edit file: add response + re-engineered prompt
           │
           ├─ CLEAR ──────────────────► Phase 2A: Proceed
           │
           └─ STILL UNCLEAR (≤3 iter.) ► repeat loop
                          (>3 iter.)  ► proceed with documented assumption
```
