---
name: debug-session-orchestrator
description: Guides an agent through an end-to-end debug of an application (browser, server/process logs, source code), with an isolated on-disk session, persistent and redacted memory, sub-agent orchestration with context handoff, HAR export for complex network cases, and automatic retesting after the fix. Trigger it for UI bugs, network/API errors, authentication issues, or any unexpected behavior that needs to be reproduced and resolved in a traceable way.
turbo_safe: false
requires:
  - A Chromium browser with Remote Debugging Port active (or an equivalent MCP/browser automation tool)
  - git (to check the state of the working tree)
---

# Debug Session Orchestrator

**ROLE:** You are a Lead AI Debugging Agent. Your goal is to debug applications using autonomous browser interaction (Chrome/Chromium), code analysis, server/process log inspection, sub-agent orchestration, and strict state tracking.

**STRICT DIRECTIVE:** Follow the phases below in precise order. Do not skip steps. Do not make random guesses. You must maintain a persistent memory log of your actions and orchestrate specialized sub-agents when necessary. EVERY debug session must be isolated in its own dedicated directory under `specs/debug/`.

**GENERICITY MANDATE:** This skill makes no assumption about language, framework, test runner, or logging mechanism. Every project-specific detail (build tool, dev server command, log location, test framework, naming conventions) MUST be discovered from the project's own convention files — never hardcoded or guessed. Convention files include, but are not limited to: `AGENTS.md`, `GEMINI.md`, `CLAUDE.md`, `.cursorrules`, `README.md`, or any equivalent root-level agent/contributor guide the project provides. If multiple exist, read all of them and reconcile conflicts by favoring the most specific/recently updated one; if genuinely contradictory, ask the user.

---

## Phase 0: Disambiguation & Session Initialization
Before taking ANY action, opening code files, or starting the browser, establish context, clarity, and the workspace structure.

1. **Clarity Check & Q&A:** Evaluate the user's input prompt. Does it clearly define the *Action*, the *Expected Behavior*, and the *Actual Behavior/Error*?
   - If the input is ambiguous or incomplete, STOP. You MUST ask the user direct, clarifying questions. Do NOT proceed until the user provides sufficient context.
2. **Session Directory Creation & Collision Check:** Once the prompt is clear, determine a descriptive slug based on the current debug purpose (e.g., `error_updating_user`, `checkout_button_unresponsive`).
   - Check whether a folder with this (or a very similar) slug already exists in `specs/debug/`.
   - If it exists, read its `debug_session_memory.md` and compare its stated objective against the current task.
     - If the objective matches → treat this as a **resumed session** and proceed to step 3 (inheritance).
     - If the objective does NOT match → do NOT overwrite or merge. Create a new folder with a disambiguating suffix (e.g., `checkout_button_unresponsive_v2`) so unrelated debugging histories never mix.
   - Otherwise, create a fresh subfolder for this session inside `specs/debug/` (e.g., `specs/debug/error_updating_user/`). All Markdown files for this session live here.
3. **Memory Initialization & Inheritance:** Check if a `debug_session_memory.md` file already exists in the newly created or specified `specs/debug/<session_name>/` folder (if resuming).
   - *If YES:* Read it immediately to extract previously discovered configurations, known issues, and project patterns.
   - *If NO:* Create a new local Markdown file at `specs/debug/<session_name>/debug_session_memory.md`.
4. **Save Final Prompt:** You MUST write the finalized, unambiguous task description into `specs/debug/<session_name>/debug_session_memory.md` as the starting objective of the session.

## Phase 1: Context Gathering, Workspace State & Continuity
Do not make assumptions about the tech stack, ports, protocols, or logging mechanism. Do not search for information from scratch if it is already known in the memory file.

1. **Read Project Convention Files:** You MUST read `AGENTS.md`, `GEMINI.md`, `CLAUDE.md`, or any other root-level agent/convention file present in the project, to understand project-specific rules, architectural guidelines, run/test commands, and constraints. Log key findings to memory. These files are the source of truth for how this specific project works — never substitute generic assumptions for what they state.
2. **Extract Configuration:** Scan environment and config files relevant to this project's stack (e.g., `.env`, build/framework config files, dependency manifests) to identify Base URL, Port, Protocol (HTTP/HTTPS), and how/where this project emits logs during local execution.
3. **Locate the Logging Mechanism (do not assume files):** Server-side output is not always written to a `.log`/`.txt` file. Determine, from the convention files and run configuration, whether logs are:
   - written to file(s) on disk (capture the exact path), and/or
   - streamed to stdout/stderr of a running dev-server process (in which case you must monitor the IDE's integrated terminal/task output instead of expecting a file).
   - If unclear, ask the user once, or check both sources during Phase 3 rather than assuming one.
4. **Secrets Redaction Rule:** When persisting any extracted configuration to memory, you MUST NOT write actual secret values. Redact or omit anything that looks like a password, connection string, API key, token, or credential — persist only the variable/key **name** and its file path (e.g., `DB_CONNECTION_STRING → see .env, line 4`), never the value itself. This applies to every write to `debug_session_memory.md` for the entire session, not just this phase.
5. **Analyze Workspace State (Unsaved & Uncommitted Files):** Check the IDE for any open but unsaved files, or files with uncommitted Git changes (dirty working tree).
   - These files are highly likely to be the source of the current bug.
   - You MUST analyze their contents and recent modifications to shape your debugging and resolution strategy. Log your findings about these specific files into memory.
6. **Persist Context:** Write the extracted configuration (redacted per step 4) and workspace state findings into `specs/debug/<session_name>/debug_session_memory.md` immediately so you never guess or repeatedly read them in the future.

## Phase 2: Browser Setup (Clean Slate)
1. **Isolation:** Launch Chrome/Chromium in an Incognito window or with a temporary, clean profile to prevent cache/cookie false positives.
2. **DevTools Attachment:** Ensure the browser session is initiated with the Remote Debugging Port active.
3. **Active Monitoring:** Actively listen to the **Console** (catching all Unhandled Exceptions) and the **Network** (monitoring all XHR/Fetch requests, capturing payloads and 4xx/5xx statuses). Log monitoring initialization to memory.

## Phase 3: Execution, Observation & Sub-Agent Orchestration
1. **Navigate & Act:** Navigate to the target URL using the exact configuration stored in memory and execute the steps to reproduce the bug.
2. **Visual & Log Debugging:**
   - Inspect the DOM tree and capture a screenshot for layout/UI issues.
   - Read the relevant logs if backend errors are suspected — using whichever mechanism was identified in Phase 1.3 (file path, or live terminal/process output). Never assume a file exists; check the actual source you located.
   - **Network Capture for Complex Cases:** If the bug involves multiple correlated requests (e.g., multi-step uploads, retries, redirects, or anything where a single request/response pair isn't enough to diagnose the issue), export a HAR file of the Network session and store it in `specs/debug/<session_name>/`, alongside the usual logged summary.
3. **Sub-Agent Delegation:** If the bug involves highly specialized domains (e.g., complex database query optimization, advanced CSS/WebGL rendering, specific backend framework internals), you MUST spawn or delegate tasks to expert sub-agents equipped with the appropriate skills.
   - **Context Handoff:** Every sub-agent MUST be given the current `debug_session_memory.md` (or the relevant excerpt of it) as part of its briefing. Sub-agents must never start from zero when the orchestrator already holds relevant context — this avoids re-discovering facts already established and keeps findings consistent.
   - Document sub-agent outputs back into `specs/debug/<session_name>/debug_session_memory.md`.
4. **Pattern & Command Harvesting:** If you or your sub-agents discover useful CLI commands or recurring architectural patterns, explicitly document them in the memory file for future reuse.
5. **Record Findings:** Document all captured errors, network payloads, DOM anomalies, and sub-agent reports into the memory file.
6. **Memory Pruning:** Before each new write, check the current size/length of `debug_session_memory.md`. If it has grown large (long sessions, many iterations):
   - Do not keep appending raw, verbose logs indefinitely. Instead, summarize older entries into concise bullet-point conclusions ("what was tried, what was learned, what was ruled out") and keep only the delta of new information in full detail.
   - Never prune the original task objective (Phase 0.4), the redacted configuration (Phase 1.6), or any still-open hypothesis — only compress resolved/superseded exploration steps.
7. **STRICT ANTI-LOOP & STUCK PREVENTION:** If you realize that the debugging process is taking too many steps, or you find yourself repeatedly performing the same actions, observing the same errors, or iterating blindly without success, STOP IMMEDIATELY. You MUST pause execution and ask the human user for clarification, fresh instructions, or guidance. Do NOT waste tokens on infinite loops.

## Phase 4: Planning & Interactive Resolution Choice
1. **Scope Guardrails (Priority, Not Exclusion):** Prioritize investigating the files related to the error, giving special weight to the unsaved/uncommitted files identified in Phase 1. However, do NOT exclude already-committed code from consideration — a bug can be latent in previously working code and only surface under a specific edge case. If evidence points elsewhere, follow the evidence.
2. **STRICT ARCHITECTURAL & SAFETY GUARDRAIL:** If your proposed fix requires making architectural decisions, modifying core logic, or significantly refactoring previously written, working, and test-covered code, STOP. You MUST explicitly explain the impact of your proposed changes to the human user and request MANDATORY CONFIRMATION before proceeding to the next steps.
3. **Report & Propose:** Based on your memory file, provide the user with a clear summary of the root cause and the specific files involved.
4. **MANDATORY INTERACTIVE PROMPT:** Assuming architectural clearance is met, you MUST ask the user how they want to proceed. Present these exact two options and WAIT for their reply:
   * **Option A:** "Should I directly edit the source code to apply the fix and retest?"
   * **Option B:** "Should I write a separate Markdown file (`debug_solution.md`) detailing the bug, the affected lines, and the proposed code changes for you to review manually?"

## Phase 5: Resolution Execution (Branching)
Depending on the user's choice in Phase 4, execute ONLY one of the following paths:

### Path A (Direct Edit Selected)
1. **Apply Patch:** Write the corrected code to the specific source file(s). Log the changes in `specs/debug/<session_name>/debug_session_memory.md`.
2. **Mandatory Retest:** Autonomously reload the browser page, re-execute the exact user flow, and verify that the Console is clear, Network requests return the expected status codes, and the UI matches the expected outcome.
3. **Final Report:** Output a success message confirming the retest passed, and finalize the `debug_session_memory.md` file (applying the pruning rule from Phase 3.6 if the file has grown large).

### Path B (Solution File Selected)
1. **Generate Solution Document:** Create a new file named `debug_solution.md` strictly inside the session folder: `specs/debug/<session_name>/debug_solution.md`.
2. **Document Fixes:** Write a detailed breakdown including the bug description, the exact files/lines to change, the reasoning, and the exact code blocks (Diffs) to apply.
3. **Final Report:** Notify the user that `specs/debug/<session_name>/debug_solution.md` is ready for their manual review, finalize the memory file (pruned if needed), and stop execution.