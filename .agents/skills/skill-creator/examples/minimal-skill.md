---
name: code-review-guide
description: |
  Guide code reviews in AI Agent with language-specific best practices.
  Use when: (1) reviewing code quality, (2) checking for maintainability issues,
  (3) verifying language-specific patterns, (4) ensuring project consistency
---

# Code Review Guide

Help AI Agent perform thorough code reviews by checking clarity, testing, and consistency.

## Universal Code Review Principles

When AI Agent reviews code, apply these principles:

1. **Clarity & Maintainability**
   - Is the code readable and self-documenting?
   - Are variable/function names descriptive?
   - Are complex operations explained with comments?

2. **Testing & Edge Cases**
   - Are edge cases tested?
   - Is error handling present?
   - Is test coverage adequate for critical paths?

3. **Project Consistency**
   - Does the code follow project conventions?
   - Are dependencies and imports organized correctly?
   - Is the code style consistent with existing code?

## Language-Specific Patterns

### Python Reviews
- PEP 8 compliance: indentation (4 spaces), naming conventions
- Type hints on public functions
- Docstrings following project standards
- Exception handling specificity (avoid bare `except`)

### JavaScript/TypeScript Reviews
- ES6+ syntax usage (arrow functions, const/let, destructuring)
- Type safety in TypeScript (avoid `any`, use strict mode)
- Async/await patterns (no callback pyramid)
- Import organization (grouped by: node modules, local)

### Go Reviews
- Error handling (check and return early)
- Interface design (small, focused interfaces)
- Naming conventions (receiver names, package names)
- Defer usage for cleanup operations

## Example Review Checklist

When AI Agent reviews code, verify:
- [ ] All variables/functions have clear names
- [ ] Complex logic has explanatory comments
- [ ] Error cases are handled
- [ ] Tests exist for main logic
- [ ] No hardcoded values (use constants)
- [ ] Dependencies are documented
- [ ] Code follows project style guide
- [ ] Performance issues addressed for critical paths

## Quick Decision Tree

```
Is the code readable?
├─ YES: Check testing
└─ NO: Request clarification or renaming

Are edge cases handled?
├─ YES: Check consistency
└─ NO: Request error handling

Does it follow project style?
├─ YES: Approve or request minor changes
└─ NO: Request updates to match conventions
```

Provide specific, actionable feedback for AI Agent to relay to developers.
