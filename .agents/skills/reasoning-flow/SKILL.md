---
name: 'reasoning-flow'
description: 'Workspace-level workflow for conducting structured AI reasoning and planning'
trigger: 'reasoning, plan, workflow, reasoning-flow'
---

## Scope

Execute this workflow when initiating a new reasoning process, planning task, or complex problem-solving session.
Produce a comprehensive markdown document capturing the complete reasoning lifecycle.

## Language and Translation

- Use English by default for all generated reasoning files.
- Override the default language and write all reasoning files in a target language only if explicitly requested by the user.
- Record the input language and target output language in `000-plan.md` if the prompt and output languages differ.
- Record the exact, unmodified original prompt in `000-plan.md` if the input is not in English.
- Translate non-English prompts to English and record the translation in `000-plan.md`.
- **Must** ask the human user for confirmation of the English translation before proceeding with any other operations.

## Structure

- Create all files in the `reasoning` directory at the project root.
- Create the `reasoning` directory if it does not exist.
- Create a specific sub-folder for the current reasoning session.
- Name the session sub-folder descriptively based on the topic (e.g., `image-import-tool-csharp`).
- Store step files in a `steps` sub-folder within the session directory.
- Store newly generated attached files in an `attachments` sub-folder within the session directory.
- Use backtick-wrapped paths to reference attached files already present in the workspace, instead of copying them to `attachments`.

## Pre-Reasoning Phase (Orchestrator Only)

- **FIRST IMMEDIATE CHECK**: If no prompt is provided, terminate the procedure immediately. If the prompt is ambiguous, conduct a Q&A session with the human user to disambiguate the prompt **BEFORE** generating the plan.
- Define the necessary set of `domain-expert` sub-agents required by the prompt's complexity.
- Create at least one `domain-expert` specialized in the specific domain of the prompt (e.g., a chef for cooking, a C# expert for C# code).
- Conduct an initial brainstorming session with the created `domain-expert`s to define the best strategy and the list of steps to execute.
- Perform this analysis and brainstorming without user interaction.
- Synthesize the results of this brainstorming, including the analysis phase, directly inside `000-plan.md` as the official plan and list of tasks to be executed sequentially.
- **Must** ask the human user for approval of the plan in `000-plan.md` (and allow for modifications) before executing any steps, using tools, or starting Q&A sessions.
- While drafting the plan, mark in `000-plan.md` where the mandatory mid-reasoning human checkpoint (see "Mandatory Human Checkpoint (Mid-Reasoning)" below) is expected to occur, so the human approving the plan knows in advance when they will be asked to weigh in again.

## Sub-Agent Roles and Creation

- **ALWAYS** create a `qa-agent` sub-agent. This sub-agent incorporates the `qa` skill and is responsible for asking interactive questions to the human user. Questions MUST be asked one at a time. The `qa-agent` MUST strictly follow the `qa` skill.
- Limit mutating tool usage (writing files, running terminal commands) exclusively to the orchestrator agent.
- Permit `domain-expert` agents to use read-only and search tools to investigate the repository.
- Restrict `domain-expert` agents from communicating directly with the human user (they must answer only to the orchestrator).
- Evaluate requests from `domain-expert`s to create additional specialized agents (e.g., functional analysts).
- Approve and create the requested agent if the justification is valid, or reject it if invalid, always documenting the reasoning behind the decision.
- Create a dedicated `search_web` domain-expert **ONLY** if a web search is strictly necessary.
- Mandate that if a `domain-expert` operates in an area covered by existing skills or rules (e.g., C#, `markdown-style-ai.md`, `.agents/skills/mermaid-flow-diagrams/SKILL.md`), they **MUST** explicitly invoke and adhere to them.

### Handoff Protocol Between Agents

Sub-agents can lose track of prior context between invocations, and free-form summaries make it easy for findings, assumptions, and open questions to get silently dropped when work passes from one expert to the next. To prevent this, treat every transition between two sequential steps whose output feeds directly into the next step as a formal handoff, not a casual note.

- Whenever a step's output is meant to feed the next step, close that step's file with a `## Handoff` block using this exact structure:
  - `Findings`: what was established, in plain prose.
  - `Confidence`: `high`, `medium`, or `low`.
  - `Assumptions`: anything taken for granted that the next agent should be able to challenge.
  - `Open questions`: unresolved points the next agent (or the orchestrator) still needs to address.
- A `domain-expert` never briefs another `domain-expert` directly. The orchestrator always reads the `Handoff` block first, confirms or corrects it, and only then briefs the next agent with the confirmed version. This keeps the orchestrator as the single source of truth for what each agent actually knows.
- If the orchestrator has to correct a `Handoff` block (wrong assumption, missing finding), record the correction inline in the same block rather than silently rewriting it, so the discrepancy stays visible in the reasoning trail.

## Research and Analysis

- **MUST** use `tokensave` and `headroom` MCP tools across **ANY AND ALL** agents (including the orchestrator and all sub-agents) to manage context and minimize interaction costs.
- Report the results of any repository or web searches directly within the current step document.
- **DO NOT** create dedicated markdown files for search steps.
- Keep internal search results concise. Provide a summary with backtick-wrapped file references if the search output is extensive.
- Maintain a clear structural division between discovered facts (search results) and the deductions derived from those facts.
- Identify explicitly WHICH expert found the information and WHAT they deduced from it.
- Ensure the `search_web` expert reports the source URL and **ALWAYS** verifies the reliability of the source.
- Allow the `search_web` expert to ask the orchestrator to conduct a QA session with the user to identify reliable sources.

## Mandatory Human Checkpoint (Mid-Reasoning)

The pre-reasoning plan approval and the pre-termination question both bookend the process, but neither one gives the human a chance to redirect the reasoning while it is actually happening. To close that gap, the workflow requires exactly one additional, non-optional human checkpoint in the middle of execution.

- Once the research/analysis phase is complete and before synthesis or drafting begins, the orchestrator **MUST** pause execution.
- Write a `qa-midpoint.md` file in the `steps` sub-folder (following the same `qa-[topic].md` naming convention as other Q&A files) summarizing the key findings gathered so far and any tradeoffs or decisions that remain open.
- Ask the human user **at least one substantive question** at this point — e.g., confirming a direction, choosing between tradeoffs, or validating an assumption a `domain-expert` relied on. A purely rhetorical or yes/no "does this look fine?" question does not satisfy this requirement; the question must surface a real decision point from the research so far.
- This checkpoint is mandatory **even if the original prompt was unambiguous** and no other Q&A session has been triggered. It is independent from the optional Q&A sessions described elsewhere, which remain triggered only "when needed."
- `AUTO-STOP` does not skip this checkpoint: it only governs whether the workflow may terminate itself without a final question. If the user needs the entire workflow to run fully unattended, they must say so explicitly (e.g., `AUTO-STOP-FULL`); absent that, treat the mid-reasoning checkpoint as always active.
- Reference `qa-midpoint.md` in `TABLE-OF-CONTENTS.md` like any other generated file.

## Cross-Reasoning References

- If the current reasoning relies on a previous reasoning session (Reasoning B), read the `README.md` of Reasoning B in its entirety.
- Combine the `README.md` of the referenced reasoning with the current user prompt.
- **ONLY** use the `README.md` file from the previous reasoning; **DO NOT** read its individual step files.

## File Access and Permissions

- **NO file writing is permitted** except within the folder generated for the current reasoning session.
- **Modifying existing reasoning sessions is STRICTLY DENIED**: write permission is granted **ONLY** in the current reasoning folder.
- **NO agent can modify existing files**.
- **NO write, modify, or delete permissions** are allowed outside the current reasoning folder.
- **IF** the reasoning process concludes that an existing external file needs to be modified, **AGENTS CANNOT MODIFY IT THEMSELVES** under any circumstances.
- Instead, agents **MUST write explicit instructions for the human user** detailing exactly what modifications are needed.
- These instructions must be written in an appropriate step file and then explicitly **summarized in a dedicated paragraph** in the final `README.md` (e.g., "Update chapter 2 of document 'prova.md' with the title 'post-flow changes'").

## Core Rules

- Initialize the reasoning session by creating `000-plan.md` in the `steps` sub-folder.
- Create `TABLE-OF-CONTENTS.md` in the `steps` sub-folder immediately after the plan.
- Create a new sequentially numbered markdown file (e.g., `001-research.md`) in the `steps` sub-folder for each executed reasoning step.
- Maintain a minimum of two files: `000-plan.md` and `TABLE-OF-CONTENTS.md`.
- Ensure `000-plan.md` contains enough information to completely restart the reasoning process and recreate the required agents collection.
- Enforce strict adherence to the original request and reasoning focus during all brainstorming and reasoning sessions.
- Conduct brainstorming at any step by integrating the reasoning and conclusions directly into the current step file.
- Document clearly within the step file which experts participated and summarize their contributions concisely.
- **DO NOT** create dedicated, standalone brainstorming markdown files.
- Close every step file whose output feeds a subsequent step with the `## Handoff` block described in "Handoff Protocol Between Agents".
- Initiate Q&A sessions with the human user using the `qa` skill when needed after plan approval, in addition to the mandatory `qa-midpoint.md` checkpoint.
- Assign descriptive filenames to Q&A session files (e.g., `qa-[topic].md`) to clearly identify the subject.
- Write sequentially into new step files for each phase of reasoning.
- Update `TABLE-OF-CONTENTS.md` with backtick-wrapped paths to new step files.

### 000-plan.md

- Must always be the first step file.
- Must contain EXCLUSIVELY the following information:
  - Exact original user prompt within YAML frontmatter.
  - English translation of the prompt if applicable.
  - Input and output language specifications if applicable.
  - Original file system path of the reasoning directory.
  - List of callable agents and tools.
  - The analysis phase and the synthesized list of tasks/steps defined during pre-reasoning.
  - The expected point in the task list where the mandatory mid-reasoning human checkpoint will occur.
- Allow the human user to modify the tool list to restrict or define available tools.
- Use default tools in combination with user-defined tools if the user specifies a subset.
- Use only user-defined tools if the user explicitly overrides defaults entirely.
- Update the tool list dynamically during reasoning if new requirements emerge.

### TABLE-OF-CONTENTS.md

- Must always be present and unnumbered.
- List backtick-wrapped paths to all generated step files.
- Update continuously as new step files are created.
- Include an `attachments` section.
- List paths to newly generated attachment files stored in the `attachments` sub-folder.
- List paths to existing workspace files referenced by the prompt without duplicating them.

### Formatting Rules

- Format all generated files in Markdown.
- Include YAML frontmatter in all files.
- Ensure correct YAML syntax.
- Adhere strictly to the `markdown-style-ai.md` rules for all files.
- Ensure all files are convertible to PDF via pandoc.
- **DO NOT** use footnotes, section references, callouts, or HTML tags.
- **DO NOT** use markdown links.
- Use backtick-wrapped paths for all file references.
- Include Mermaid diagrams for complex structural flows (multi-step workflows, branching logic, state transitions, system interactions) ONLY when they enhance visual clarity alongside prose.
- **MANDATORY**: Whenever Mermaid diagrams are needed, invoke `.agents/skills/mermaid-flow-diagrams/SKILL.md`.

### Termination and Output

- **UNBREAKABLE RULE**: The workflow can **NEVER** terminate on its own. It **MUST** ask the user at least 1 question before terminating, unless the user explicitly requested otherwise (`AUTO-STOP`).
- If the `AUTO-STOP` directive is present in the prompt, the workflow can terminate on its own and write the `README.md` file as described. `AUTO-STOP` affects only this final termination question; it does not exempt the workflow from the mandatory mid-reasoning human checkpoint.
- Stop execution when the human user requests `STOP`.
- Propose a `STOP` to the user when the reasoning is fully elaborated and effective (if `AUTO-STOP` is not active).
- **DO NOT** create a dedicated step document for the STOP phase.
- Record the `STOP` phase simply as a note inside `TABLE-OF-CONTENTS.md` including the current DATE and TIME.
- Terminate all active sub-agents upon `STOP`.
- **DO NOT** remove sub-agent entries from `000-plan.md`.
- Generate a final `README.md` in the session directory.
- Include an EXTREMELY SYNTHETIC summary in the `README.md` YAML frontmatter.
- **UNBREAKABLE RULE**: The `README.md` file **MUST** be a complete reasoning and can reference existing documents and references, but it **MUST NOT** reference step files:
  - Each step file is an incremental block used to generate the `README.md` document.
  - Each step file is small to occupy fewer tokens and be more manageable by the LLM.
  - Each step file **DOES NOT** contain repetitions or references to previous steps.
  - Each step file contains ONLY the information necessary to generate the `README.md` document at that precise point.
- **UNBREAKABLE RULE**: The `README.md` file is the expansion of the ENTIRE reasoning:
  - It **MUST** be readable by the user as a final document.
  - It **MUST** be self-contained and **MUST** contain ALL information necessary to be understood by the final user.
  - It **MUST NOT** reference the step files (this is fundamental).
  - It can contain references to external documents or files, but not to step files.
  - It **MUST** explain EVERYTHING necessary to be understood by the final user.
  - It **MUST** explain the reasoning that led to the drafting of the document.
  - It **MUST** include the creation date and time in the YAML frontmatter.
  - It **MUST** have YAML format in the frontmatter and Markdown in the body.
  - It **MUST** be a broader reasoning than the individual parts of the steps, or be equal to it, but it **MUST NOT** be a mere summary of the various steps.
  - It **MUST** be a document that can "live on its own" without depending on the reasoning steps that generated it.
- **DO NOT** delete step files autonomously; only the human user is allowed to delete them.
- Introduce `README.md` with a need/requirement paragraph that rewrites the initial prompt in a discursive format.
- Keep the exact initial prompt isolated exclusively within `000-plan.md`.
- Include backtick-wrapped paths to initial prompt attachments in `README.md`.

## Validation

- [ ] `reasoning` directory exists
- [ ] Session sub-folder exists with descriptive name
- [ ] `steps` sub-folder contains `000-plan.md`
- [ ] `000-plan.md` contains exact prompt, translation, tools, and synthesized task list
- [ ] Pre-reasoning completed and user approved `000-plan.md` before execution
- [ ] Orchestrator is the only agent using mutating tools
- [ ] `domain-expert` agents strictly confined to domain reasoning and safe searches
- [ ] `tokensave` and `headroom` tools utilized by all agents to reduce costs
- [ ] Search results and brainstorming integrated concisely into current step files
- [ ] Clear distinction between facts and deductions, including which expert made them
- [ ] `search_web` expert reports verified URLs and coordinates with orchestrator for user guidance
- [ ] Previous reasoning context restricted to `README.md` file only
- [ ] Every step file whose output feeds a subsequent step ends with a `## Handoff` block (Findings, Confidence, Assumptions, Open questions)
- [ ] Orchestrator confirmed or corrected each `Handoff` block before briefing the next agent
- [ ] `qa-midpoint.md` created and at least one substantive question asked before final synthesis began
- [ ] Q&A sessions use descriptive `qa-[topic].md` filenames
- [ ] Human user confirmed English prompt translation if applicable
- [ ] `steps` sub-folder contains `TABLE-OF-CONTENTS.md`
- [ ] `TABLE-OF-CONTENTS.md` is up-to-date and contains STOP note with date/time
- [ ] All step files follow formatting rules
- [ ] If Mermaid diagrams are included for complex flows, confirm `.agents/skills/mermaid-flow-diagrams/SKILL.md` was invoked
- [ ] Final `README.md` generated upon termination
- [ ] No existing files modified or deleted outside the current reasoning folder
- [ ] Required file modifications are explicitly marked in steps and highlighted in the final `README.md`
