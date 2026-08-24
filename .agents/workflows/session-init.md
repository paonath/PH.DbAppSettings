---
description: Bootstraps a work session by calling TokenSave session start, recalling active project decisions, checking git status, and loading recent lessons.
---

1. Activate the `tokensave-memory-bridge` skill and execute `tokensave_session_start`.
2. Recall active project decisions and recent code area modifications using `tokensave_session_recall`.
3. Check repository status with `git status` and `git branch --show-current`.
4. Scan `.agents/Lessons/` for relevant project gotchas and constraints.
5. Summarize session starting context for the user.
