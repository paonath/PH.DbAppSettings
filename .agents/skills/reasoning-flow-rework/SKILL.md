---
name: 'reasoning-flow-rework'
description: 'Workflow for reworking, expanding, and deepening an already completed AI reasoning session.'
trigger: 'rework reasoning, reasoning-flow-rework, reasoning rework, expand reasoning'
---

## Scope

Execute this workflow when the user requests to rework or expand an **already completed** reasoning session.
This workflow re-executes the **COMPLETE** `reasoning-flow` process (all steps, from the first) inside the **existing** reasoning directory, without creating a new one.
The old `README.md` and the new prompt are **combined as a single unified input** throughout all reasoning steps.

## Pre-Requisites and Setup

- **FIRST IMMEDIATE CHECK**: If no prompt is provided, terminate immediately. If no pre-existing `README.md` is found in the current reasoning directory, terminate immediately. If the prompt is ambiguous, conduct a Q&A session using the `qa` skill **BEFORE** generating any plan.
- **UNBREAKABLE RULE**: The workflow **MUST NOT** create a new reasoning directory. All operations are confined to the **existing** session directory.

## Backup Phase (MANDATORY — EXECUTE BEFORE ANYTHING ELSE)

**CRITICAL**: This phase is not optional. No reasoning step may start until the backup is fully complete and verified.

- Create the sub-folder `attachments/old-readme` inside the session directory if it does not exist.
- Determine the current backup index using progressive numbering (`000` for the first rework, `001` for the second, etc.) by counting existing backup files in `attachments/old-readme`.
- Copy and rename the following files using the determined index:
  - `README.md` → `attachments/old-readme/<index>-old-README.md`
  - `steps/000-plan.md` → `attachments/old-readme/<index>-old-plan.md`
  - `steps/TABLE-OF-CONTENTS.md` → `attachments/old-readme/<index>-old-toc.md`
- **DO NOT** modify or delete the originals during this phase; only copy them.
- **Verify** all three backup files exist before proceeding. If any copy fails, halt and notify the user.
- **DURING the rework, ALL historicized READMEs in `attachments/old-readme` MUST be used incrementally** (from `000` onward) to build a cumulative knowledge base.
- **UNBREAKABLE RULE**: If multiple historicized READMEs exist, **ALL of them MUST be analyzed** and integrated into the final elaboration — not just the most recent one. Each previous README represents a distinct reasoning layer that must be explicitly accounted for.

## Initialization Phase

- Generate a **new** `steps/000-plan.md` replacing the previous one, containing:
  - The new user prompt.
  - A backtick-wrapped reference to the most recent historicized README (`<index>-old-README.md`).
  - The list of agents and tools.
  - The synthesized task list from the pre-reasoning brainstorming.
- Generate a **new** `steps/TABLE-OF-CONTENTS.md` replacing the previous one.
- **Step Numbering**: New step files **MUST** continue the incremental numbering from where the old steps left off. Never reset numbering; never reuse existing numbers.
- Old step files in the `steps` folder **MUST NOT** be modified. They are read-only historical knowledge.

## Combined Input Rule

**UNBREAKABLE RULE**: Every reasoning step treats the **old `README.md`** (the most recent historicized one) and the **new user prompt** as a single combined input.

- The first reasoning step **MUST** explicitly analyze the old README in relation to the new prompt, identifying:
  - What is confirmed and carried forward unchanged.
  - What must be revised, extended, or contradicted.
  - What is entirely new and introduced only by the new prompt.
- Subsequent steps build progressively on this delta analysis.

## Full Re-Execution of reasoning-flow Steps

**UNBREAKABLE RULE**: The rework **MUST** re-execute ALL steps of the `reasoning-flow` process from the beginning, including steps previously completed in the original session, including `orchestrator` and `sub-agents`.

- Apply **all** `reasoning-flow` rules.
- Do not skip any step category because it was addressed in the previous session.
- Each re-executed step produces a new step file with incremental numbering continuing from the previous session's last step.
- All brainstorming and domain-expert reasoning within each step must account for both the old README and the new prompt.

## Sub-Agent Roles and Knowledge

- Create a `domain-expert` agent that **MUST know ALL historicized READMEs** in `attachments/old-readme`, reading them from `000` onward.
- Follow all standard `reasoning-flow` rules for creating additional domain experts.
- The `domain-expert` must explicitly state, for each finding, whether it confirms, revises, or contradicts the previous README.
- Old step files in the `steps` folder **MUST NOT** be modified under any circumstances. They are read-only historical knowledge.

## User Comments Handling (`[[...]]`)

- Scan the most recent historicized README for phrases delimited by `[[...]]`.
- These are explicit user comments inserted to expand or correct the reasoning.
- **Rules for Comments**:
  - They **MUST** be correlated with the surrounding document context.
  - They **MUST** be analyzed exhaustively in relation to the new prompt.
  - **ALL** comments **MUST** be explicitly registered, addressed, and discussed within the new step files.
  - The comments **MUST NOT** be removed or altered in the historicized README files.

## File Access and Permissions

- **NO file writing is permitted** outside the existing session directory.
- **NO agent may modify existing files** (step files, backup files, or any external file).
- **IF** the reasoning concludes an external file must be modified, agents **MUST** write explicit instructions for the human user and summarize them in the final `README.md`.

## Termination and Output

- **UNBREAKABLE RULE**: The workflow can **NEVER** terminate on its own. It **MUST** ask the user at least one question before terminating, unless `AUTO-STOP` is present in the prompt.
- If `AUTO-STOP` is present, the workflow terminates autonomously and writes the new `README.md`.
- The new `README.md` **MUST** be the elaboration of ALL steps starting from the very first, synthesizing both the old reasoning and the new prompt into a single, deeper, unified document.
- **UNBREAKABLE RULE**: The new `README.md` MUST be **standalone** and **MUST NOT** depend on old READMEs or step files. It must read as a first and complete elaboration, naturally richer and deeper than the previous one.
- **UNBREAKABLE RULE**: The `README.md` **MUST NOT** reference step files. Each step file is an incremental building block; the README is the synthesis.
- The `README.md` **MUST**:
  - Be readable as a final, self-contained document.
  - Explain the reasoning that led to its content.
  - Include creation date and time in the YAML frontmatter.
  - Contain ALL information necessary for the final user without requiring access to step files or old READMEs.

## Validation

- [ ] Backup executed BEFORE any reasoning step: `<index>-old-README.md`, `<index>-old-plan.md`, `<index>-old-toc.md` in `attachments/old-readme`.
- [ ] All three backup files verified to exist before proceeding.
- [ ] No new reasoning directory created; all files are inside the existing session directory.
- [ ] New `steps/000-plan.md` and `steps/TABLE-OF-CONTENTS.md` generated.
- [ ] Old step files untouched (read-only).
- [ ] New step files use incremental numbering continuing from the previous session.
- [ ] ALL `reasoning-flow` steps re-executed from the beginning.
- [ ] Every step uses the combined input: old README + new prompt.
- [ ] First step contains explicit delta analysis (confirmed / revised / new).
- [ ] Domain-expert knows all historicized READMEs and annotates findings accordingly.
- [ ] All `[[...]]` comments identified, logged, and deeply analyzed in step files.
- [ ] New `README.md` is fully self-contained, references no step files, and is deeper than the previous one.
- [ ] `AUTO-STOP` rules respected.
