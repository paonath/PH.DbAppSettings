---
name: tokensave-memory-bridge
description: Cross-session persistent memory bridge using TokenSave tools (record_decision, record_code_area, session_recall, session_start, session_end).
trigger: tokensave-memory-bridge, session memory, recall decision, record decision, project memory
tools: mcp_tokensave_*
---

# Tokensave Memory Bridge

## Why This Skill

The `tokensave` MCP server provides code analysis tools (`tokensave_search`, `tokensave_context`, `tokensave_callers`, etc.) and cross-session memory tools persisted in `.tokensave/tokensave.db` within the project root:

| Tool | Purpose |
|------|---------|
| `tokensave_record_decision` | Persists design/architecture decision (rationale, affected files, tags) |
| `tokensave_record_code_area` | Marks touched code area (counter + timestamp of last modification) |
| `tokensave_session_recall` | Queries (FTS5) saved decisions; exponential decay ranking if query is omitted |
| `tokensave_session_start` | Saves code health baseline + returns recent `memory_delta` |
| `tokensave_session_end` | Compares current metrics against baseline to display improvements |

Antigravity requires this skill as an explicit instruction layer for TokenSave cross-session memory persistence. Without this skill, the agent will only use code reading tools (`tokensave_search`, `tokensave_context`) and ignore persistent memory capabilities.

**Scope and Boundaries**: This memory is project-specific (bound to `.tokensave/tokensave.db`). It records technical code decisions explicitly. Do not store user personal preferences, chat conversation history, secrets, API credentials, or non-project data in this memory layer.

## Trigger Scenarios

1. **Session Start**: Initiating work on a project containing a `.tokensave/` directory in the root (verify via `tokensave_status`).
2. **Post Architectural Decision**: After agreeing on a library choice, design pattern, data schema, performance trade-off, or module structure.
3. **Pre Code Modification**: Before modifying a code area not touched recently, check if existing decisions apply to that area.
4. **Explicit User Requests**: When the user asks "what did we decide about...", "why did we choose...", "resume where we left off", "save this decision", or "summarize project state".
5. **Session Completion**: Closing a long task where `tokensave_session_start` was invoked.

## Procedure

### A. Session Startup

1. Call `tokensave_session_start`. Review the returned `memory_delta` (up to 5 recent decisions and 5 recent code areas) to orient architectural context.
2. If the task targets a specific domain (e.g., chunked file upload), call `tokensave_session_recall` with a relevant query (e.g., `"upload" OR "chunked"`) to retrieve historic decisions.

### B. Execution Phase

- **Before Structural Edits**: Call `tokensave_session_recall` querying module/symbol names. If prior decisions conflict with proposed changes, notify the user before proceeding.
- **Recording Architecture Decisions**: Call `tokensave_record_decision` immediately after agreeing on a technical choice:
  - `decision`: Short declarative summary (e.g., "Use streaming SHA-256 via @noble/hashes instead of loading full file buffer for chunked upload")
  - `reason`: Rationale in 1-2 sentences (trade-off, constraint, discarded alternatives)
  - `files`: Array of affected file paths
  - `tags`: 1-4 short reusable tags (e.g., `["upload", "performance", "angular"]`)
- **Recording Code Area Updates**: Call `tokensave_record_code_area` after completing non-trivial work on a module or directory path.

### C. Session Wrap-Up

- Call `tokensave_session_end` if `tokensave_session_start` was called at session start. Provide a concise 1-2 line summary of metric improvements if relevant.

## Decision Recording Guidelines

- **Record**: Architecture decisions, explicit trade-offs, design patterns, project conventions, technical constraints discovered during debugging, and security policies.
- **Do Not Record**: Trivial implementation details, formatting refactorings, generic TODOs (use backlog issues instead), secrets/credentials, or personal user data.
- **One Decision Per Call**: Keep decisions granular. Do not aggregate unrelated decisions into a single `tokensave_record_decision` call to preserve FTS5 search accuracy.

## End-to-End Workflow Example

```
User: "Let's resume work on the project, today I want to review permission handling"

1. tokensave_session_start
   -> memory_delta: ["Decision: Permission inheritance logic must be handled strictly in Service layer, not in Controllers", "Code area: src/Permissions/* (touched 6 times, last 3 days ago)"]

2. tokensave_session_recall(query: "permission inheritance")
   -> Returns historic decision details

3. [Code modification...]

4. Agree on decision with user: "Add ownership check before breaking inheritance"
   -> tokensave_record_decision(
       decision: "Added ownership check prior to modifying permission inheritance",
       reason: "Prevent non-owner write users from altering ACL structure",
       files: ["src/Permissions/PermissionService.cs"],
       tags: ["permissions", "security", "ownership"]
     )

5. tokensave_record_code_area(path: "src/Permissions/")

6. Wrap up -> tokensave_session_end
```

## Tool Availability Fallback

If `tokensave_*` tools are not available in the MCP tool list, the server is not registered. Inform the user and suggest running:
```bash
tokensave install --agent antigravity
tokensave init
```
Do not create custom text files or alternative temporary mechanisms for memory persistence.
