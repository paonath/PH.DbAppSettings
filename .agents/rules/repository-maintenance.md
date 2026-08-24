---
trigger: model_decision
description: Repository maintenance rules for TokenSave indexing, lesson updates, and temporary file cleanup
globs: '**/*'
---

## TokenSave Maintenance

- Run `./tokensave-update/tokensave-update.sh` periodically to check for binary upgrades, execute `tokensave sync`, and verify knowledge graph health via `tokensave doctor`.
- Execute `tokensave sync` after major refactoring sessions to ensure semantic indexes remain up to date.

## Temporary Directory Cleanup

- Clean `./.tmp/` subdirectories after completing tests or build diagnostics.
- Never commit `.tmp/` contents to git history.

## Lesson Harvesting

- Record non-trivial architectural decisions or bug fix resolutions in `.agents/Lessons/` using the `agent-self-learning` skill.
- Review existing lessons before commencing complex refactoring tasks.
