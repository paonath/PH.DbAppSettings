---
trigger: model_decision
description: Documentation generation rules with two-chapter structure
globs: '**/docs/**/*.md'
---

## Document Structure

Each documentation file MUST be self-contained and AI-parseable:

- One markdown file per topic/feature/module.
- Two mandatory chapters: **Technical** + **Functional**.
- Complete YAML metadata header.
- No external dependencies between files.

### Metadata Header

```yaml
---
title: [Feature/Module Name]
summary: [1-4 sentence description]
commit: [git hash]
date: [YYYY-MM-DD]
branch: [branch name]
review_date: [YYYY-MM-DD]
---
```

### Chapter 1: Technical Documentation

- Target: developers, AI code analysis, architects.
- All H2 headings prefixed with `Technical:`.
- Include: architecture overview (mermaid diagrams), code structure (DAL/BLL/UI layers), API endpoints, design patterns, code snippets (<20 lines), dependencies table.

### Chapter 2: Functional Documentation

- Target: project managers, end-users, QA.
- All H2 headings prefixed with `Functional:`.
- Include: feature descriptions, user workflows, configuration options, usage scenarios, troubleshooting.

## Writing Style

- Define technical terms on first use.
- One main idea per paragraph.
- Consistent terminology across both chapters.
- Heavy use of bullet points, tables, code blocks.

## Code References

- Link to files with line numbers: `src/Auth/JwtService.cs#L45-60`.
- Reference specific entities: `IAuthService`, `LoginController.Authenticate()`.
- Use inline code for class/method names.
- Snippets under 20 lines; no auto-generated code.

## File Naming

- Use kebab-case: `user-authentication.md`.
- Match main code entity: `OrderService.cs` -> `order-service.md`.
- Language suffix if needed: `order-service.it.md`.
- Target 200-500 lines per file; split if >500 lines.

## Versioning

- **MUST NOT** increment project version for documentation-only changes.
- Log changes in `CHANGELOG.md` under `## [Documentation]`.
- Use `docs:` prefix for git commit messages.

## Language

- Auto-detect language from source code comments.
- Fallback: English.
- Follow the project's established language convention for comments and documentation.