# Reference — TokenSave Memory Tools

This reference document provides detailed argument structures for TokenSave memory tools. Consult this reference when exact parameter specifications are required.

## tokensave_record_decision

Persists a technical design or architecture decision.

Parameters:
- `decision` (string, required) — Short declarative summary (e.g., "Use internal JWT for LDAP authentication instead of Basic Auth on every request")
- `reason` (string, optional) — Rationale explanation in 1-2 sentences
- `files` (array of strings, optional) — Affected file paths
- `tags` (array of strings, optional) — Short, reusable, lowercase tags without spaces

Best Practices:
- Record one decision per tool invocation.
- Ensure `decision` text is understandable out of context without requiring full chat transcript history.
- Use consistent, standardized tags across the project (e.g., use `acl` consistently rather than alternating between `permissions`, `acl`, and `authz`) to optimize FTS5 recall queries.

## tokensave_record_code_area

Marks a file or directory path as touched. Increments the touch counter and updates `last_touched_at`.

Parameters:
- `path` (string, required) — File or directory path

Best Practices:
- Invoke after completing work on modules or feature areas rather than after every individual line edit.
- Prefer directory paths (e.g., `src/Acl/`) when modifications touch multiple related files in a feature module.

## tokensave_session_recall

Queries persisted architecture decisions.

Parameters:
- `query` (string, optional) — FTS5 search query string. If omitted, returns recent decisions ranked using exponential decay (14-day half-life: older decisions rank lower but remain permanently retrievable).

Best Practices:
- Use `OR` operators for synonyms when uncertain of exact historical terminology (e.g., `"acl" OR "permission" OR "authorization"`).
- If an initial query returns zero matches, retry with alternative terms before assuming no decision was recorded.

## tokensave_session_start / tokensave_session_end

`session_start`:
- Captures a baseline snapshot of codebase health metrics.
- Returns `memory_delta` containing up to 5 recent decisions and 5 recent code areas for quick context orientation.

`session_end`:
- Recalculates metrics and compares them against the baseline established by `session_start`.
- Displays delta metrics by dimension (e.g., complexity, dead code) showing improvements or regressions.
- If `session_start` was not called during the session, `session_end` lacks a baseline snapshot; avoid calling `session_end` without an active baseline.

## Distinction from Standard TokenSave Tools

Standard TokenSave tools (`tokensave_search`, `tokensave_context`, `tokensave_callers`, etc.) are read-only tools that inspect the current code graph extracted from source files. The five memory tools described in this reference write persistent state stored across sessions in `.tokensave/tokensave.db`.
