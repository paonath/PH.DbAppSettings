---
trigger: model_decision
description: Git commit conventions and branch naming rules for repository changes
globs: '**/*'
---

## Commit Message Format

Follow the Conventional Commits specification for all commit messages:

```
<type>(<scope>): <short summary>

[optional body describing motivation and context]
```

### Commit Types

- `feat`: new user-facing functionality or feature
- `fix`: bug fix or defect resolution
- `docs`: documentation updates (README, docs/, CHANGELOG)
- `style`: code style, formatting, or whitespace changes
- `refactor`: structural code changes without functional alterations
- `test`: adding or updating unit/integration tests
- `chore`: maintenance tasks, build configuration, dependency updates

## Branch Naming Conventions

- `feature/<short-description>`: for new feature development
- `bugfix/<issue-description>`: for non-urgent bug fixes
- `hotfix/<critical-issue>`: for production-blocking fixes

## Integration Rules

- Keep commit messages concise, starting with a lowercase verb.
- Update `CHANGELOG.md` whenever adding new features or fixing bugs, matching conventional commit types.
