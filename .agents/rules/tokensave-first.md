---
trigger: model_decision
description: TokenSave-first policy: use MCP tools before file reads for code analysis
globs: '**/*.cs, **/*.ts, **/*.html, **/*.css, **/*.md'
---

## Core Policy

Before reading source files or scanning the codebase, use TokenSave MCP tools. They provide instant semantic results from a pre-built knowledge graph.

## Tool Ordering

| Need | Use First | Fallback |
|------|-----------|----------|
| Understand a symbol/module | `tokensave_context` | `view_file` |
| Find a symbol/pattern | `tokensave_search` | `grep_search` |
| Who calls a function | `tokensave_callers` | `grep_search` with function name |
| What a function calls | `tokensave_callees` | `view_file` on the function |
| Impact analysis | `tokensave_impact` | manual file-by-file review |
| Node/symbol details | `tokensave_node` | `view_file` on the file |
| List files in area | `tokensave_files` | `list_dir` |
| Affected by change | `tokensave_affected` | manual analysis |

## Rules

- **MUST** call `tokensave_context` as the first step for any code analysis task.
- **MUST** use `tokensave_search` before `grep_search` for symbol lookups.
- **MUST** use `tokensave_callers`/`tokensave_callees` for call graph analysis.
- Use fallback to file reads only when TokenSave output is insufficient.
- Do not duplicate analysis already obtained from TokenSave.

## Cross-Session Memory

- Use the `tokensave-memory-bridge` skill at session start and end.
- Call `tokensave_session_start` at the beginning of a work session.
- Call `tokensave_record_decision` after design/architecture decisions.
- Call `tokensave_record_code_area` after non-trivial modifications to a module.
- Call `tokensave_session_end` at the end of a work session.

## Source Code File Read Fallback

Direct SQL queries or command-line database tools targeting `.tokensave/tokensave.db` are **STRICTLY PROHIBITED**. When TokenSave MCP tools are unavailable or return insufficient output for code analysis, the agent **MUST** fall back directly to inspecting source code files using workspace inspection tools (`view_file`, `grep_search`).

## Headroom Policy (Context Compression)

Use Headroom MCP tools (`headroom_compress`, `headroom_retrieve`, `headroom_stats`) to optimize context window usage when dealing with large volumes of text.

### When to Compress (MANDATORY triggers)

- **Large File Reads**: Any file content exceeding 150 lines (or 5KB) should be compressed with `headroom_compress` before reasoning over it, unless exact character-by-character editing of the entire block is immediately required.
- **Verbose Tool Output**: Large outputs from commands like `dotnet test`, `dotnet build`, `npm test`, or `find` that exceed 100 lines should be compressed.
- **Search Results**: Multiple search matches or grep results that are large should be compressed.
- **DOM Snapshots / Traces**: Large HTML snapshots or test logs from Playwright/browser testing.

### Compression Rules

- **Compress first**: Pass the large text directly to `headroom_compress`. It returns a summary/compressed representation along with a hash.
- **Use the Hash**: Keep the hash in your context. When you need specific details, query that content using `headroom_retrieve` with a query or a target section.
- **Check Stats**: Use `headroom_stats` to verify tokens saved.

## When TokenSave Is Not Available

If `tokensave_*` tools are not available in the MCP tool list, the server is not registered. Inform the user and suggest:
```bash
tokensave install --agent antigravity
tokensave init
```
Do not improvise alternative memory mechanisms.

## Gap Reporting

If a TokenSave tool limitation is discovered, propose opening an issue at `https://github.com/aovestdipaperino/tokensave` after removing sensitive/proprietary code from the description.