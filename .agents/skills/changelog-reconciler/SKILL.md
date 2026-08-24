---
name: changelog-reconciler
description: Automates drafting and updating CHANGELOG.md entries from git commit history following Keep a Changelog conventions.
trigger: changelog-reconciler, update changelog, reconcile changelog, generate changelog, changelog
---

# Changelog Reconciler

## Purpose

Parses recent git commits and repository diffs to produce structured, compliant entries in `CHANGELOG.md` following `changelog-format.md` conventions.

## Procedure

1. **Git Commit History Extraction**:
   - Inspect recent commits using `git log --oneline -10`.
   - Inspect current branch name using `git branch --show-current`.
   - Obtain short commit hash using `git rev-parse --short HEAD`.
2. **Category Classification**:
   - Group commit messages into appropriate categories:
     - `Added`: new features
     - `Changed`: functionality modifications
     - `Fixed`: bug fixes
     - `Removed`: removed features
     - `Security`: security fixes
3. **Format and Write**:
   - Insert new `## [Unreleased] - YYYY-MM-DD UTC` header at top of `CHANGELOG.md`.
   - Include branch name and commit hash details.
   - Append bullet points under relevant categories using clear, past-tense descriptions.
