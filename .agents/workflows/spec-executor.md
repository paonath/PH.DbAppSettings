---
description: Executes a specification file step by step with baseline health checks, native TDD Red-Green-Refactor enforcement, adaptive sub-agent delegation, and memory synchronization.
---

## Hard Stops
- **No spec attached** → stop. Ask: *"No spec attached. Please attach the spec file you want me to execute."*
## Phase 1: Pre-Flight & Baseline Health Gate
1. **Verify Spec Attachment**:
   - Check if a specification file under `/specs/` is attached or referenced.
   - If missing, stop immediately and prompt: *"No spec attached. Please attach or specify the path to the spec file under `/specs/` to execute."*
2. **Verify Baseline Health**:
   - Run the project baseline build and test suite (e.g. `dotnet test`, `npm test`) *before* editing any files.
   - If baseline tests fail, stop and invoke the `qa` skill asking the user whether to resolve pre-existing failures or proceed.
## Phase 2: Context & Scope Loading
1. **Load Context & Tools**:
   - Read the entire attached specification document.
   - Use TokenSave MCP tools (`tokensave_search`, `tokensave_context`, `tokensave_files`) to locate target code areas.
   - Read local `AGENTS.md` in all affected project folders.
   - Use `headroom_compress` if context or source files exceed 150 lines.
   - Identifies useful skills and rules
2. **Strict Scope Enforcement**:
   - Implement ONLY what is explicitly specified. Any out-of-scope requirement requires human approval via `qa`.
   - **CRITICAL NOTE**: Git commits and `CHANGELOG.md` edits are strictly excluded from this workflow.
## Phase 3: Task Execution Loop (with TDD Enforcement)
Execute each task in Section 11 (Task Breakdown) strictly in order:
1. **For TDD Test Tasks (`[PHASE: RED]` / `type: test`)**:
   - Write the failing unit/integration test file only.
   - Run the test validation command.
   - Verify the test **FAILS** for the expected reason (e.g., `NotImplementedException`, validation error). If it passes immediately or fails with a syntax error, fix the test before proceeding.
2. **For TDD Code Tasks (`[PHASE: GREEN]` / `type: code`)**:
   - Write the minimum production code necessary to pass the preceding RED test.
   - Run the test validation command.
   - Verify the test **PASSES** (Green).
3. **For TDD Refactor Tasks (`[PHASE: REFACTOR]` / `type: refactor`)**:
   - Refactor code and test structure (naming, duplication, clarity) without altering external behavior.
   - Re-run the entire relevant test suite and ensure all tests remain **GREEN**.
4. **For Standard Tasks (non-TDD `type: code` / infrastructure / schema)**:
   - Implement the task directly and execute its specific validation command.
5. **Adaptive Execution Model**:
   - Direct Execution: Execute atomic single-file tasks directly in the main orchestrator thread.
   - Sub-Agent Delegation: For complex multi-stack tasks (e.g. backend + frontend), spawn a specialized domain sub-agent (e.g. `dotnet-expert`, `angular-expert`).
   - Every sub-agent brief must require reporting lessons/memories and reasoning summary:
     ```text
     LessonsSuggested: <title>: <reason> | none
     MemoriesSuggested: <title>: <reason> | none
     ReasoningSummary: <rationale>
     ```
## Phase 4: Global Verification & Security Gate
1. Run the full solution test suite to verify zero regressions.
2. Run `security-secret-scanner` across modified files to ensure no hardcoded secrets or credentials were introduced.
3. Review `git diff` against the spec scope.
## Phase 5: Execution Summary, Memory & Sync
1. **Execution Summary**:
   - Print a comprehensive summary of all implemented files, newly added tests, and suite pass metrics.
   - Provide an informational reminder to the user that moving the specification to `/specs/implemented/` (and removing the `spec-` prefix) is an optional manual human decision after review.
   - **STRICT PROHIBITION**: The workflow must NEVER move, rename, or delete the spec file itself.
2. **Harvest Lessons & Memories**:
   - Invoke `agent-self-learning` skill to record corrected mistakes to `.agents/Lessons/` and durable insights to `.agents/Memories/`.
3. **Sync Project Memory**:
   - Update relevant `AGENTS.md` files if new architectural decisions or CLI commands were established.