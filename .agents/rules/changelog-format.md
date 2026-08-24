---
trigger: model_decision
description: CHANGELOG.md format and maintenance rules
globs: '**/CHANGELOG.md'
---

## Format

Follow [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) conventions.

### Version Header

```markdown
## [Unreleased] - YYYY-MM-DD UTC
Branch: <branch> | Commit: <short-hash>
```

- Use `[Unreleased]` for ongoing changes.
- **MUST** include git branch name in every section.
- **MUST** include short commit hash when available.
- **MUST NOT** increase version number without explicit user instruction.

### Categories

Categorize changes under these headings (in this order):

- `### Added` — new features
- `### Changed` — changes in existing functionality
- `### Fixed` — bug fixes
- `### Removed` — removed features
- `### Security` — security fixes
- `### Deprecated` — features to be removed

### Ordering

- Versions in descending order (newest at top).
- Dates in descending order.
- Each entry as a bullet point with concise description.

## Workflow

1. Check recent commits: `git log --oneline --decorate -10`.
2. Check current branch: `git branch --show-current`.
3. Review changed files: `git diff --name-only HEAD~1`.
4. Read existing `CHANGELOG.md` to understand current format.
5. Reconcile any missing entries from previous commits.
6. Write entry following the format above.

## Commit Message for Changelog Updates

```
docs(changelog): <short summary>

<optional body: what changed and why>
```

## Rules

- **MUST NOT** edit any file other than `CHANGELOG.md` during changelog operations.
- **MUST NOT** run `git push`, `git reset`, `git rebase`, or destructive git commands.
- **MUST NOT** increase version number without explicit instruction.
- If `CHANGELOG.md` does not exist, create it with analysis of git history.