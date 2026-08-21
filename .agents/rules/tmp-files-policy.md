---
trigger: model_decision
description: Temporary files policy: all temp files go to .tmp/ directory
globs: '**/*'
---

## Required Rules

- Write all temporary files only in `./.tmp/` or its subfolders.
- Ensure `./.tmp/` exists before any write: `mkdir -p .tmp`.
- Use typed subfolders: `.tmp/logs/`, `.tmp/output/`, `.tmp/test-results/`, `.tmp/agents/<agent-name>/`.
- Delete temporary files when no longer needed.

## Prohibited

- **MUST NOT** route application/runtime logs to `.tmp/` (NLog setup/output logs stay in configured targets).
- **MUST NOT** change logging configuration to write app logs into `.tmp/`.
- **MUST NOT** use `.tmp/` for outputs that must be preserved (e.g., final spec outputs).
- **MUST NOT** store long-term or sensitive data in `.tmp/`.
- **MUST NOT** commit `.tmp/` content (keep in `.gitignore`).

## Temporary Inspection

For temporary log inspection, use shell read/pipe redirection only:
```bash
cat logfile.log | grep "pattern"
```
- **Large Log Content**: If the log content is large (exceeding 100 lines), use `headroom_compress` to compress the content before reasoning over it.
- Never change logger targets for inspection purposes.


## CI Rules

- Create `./.tmp/` at job start for stages needing temp files.
- Do not publish `./.tmp/` as release artifacts.