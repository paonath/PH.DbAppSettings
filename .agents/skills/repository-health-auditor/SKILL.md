---
name: repository-health-auditor
description: Automates code health audits, dependency integrity checks, linting verification, and TokenSave doctor diagnostics.
trigger: repository-health-auditor, repo health, audit repo, tokensave doctor, check dependencies
---

# Repository Health Auditor

## Purpose

Provides a structured automated routine to assess codebase health, verify dependency versions, check semantic knowledge graph integrity, and ensure compliance with `.agents/rules/`.

## Procedure

1. **TokenSave Health Check**:
   - Run `tokensave doctor` to verify semantic index integrity.
   - Run `tokensave status` to confirm indexed file count.
2. **Dependency Audit**:
   - For .NET projects: execute `dotnet list package --outdated` to detect outdated packages.
   - For Angular/Node projects: execute `npm outdated` to identify vulnerable dependencies.
3. **Build and Test Verification**:
   - Execute solution build (`dotnet build` or `npm run build`).
   - Run unit test suite (`dotnet test` or `npm test`).
4. **Rule Compliance Check**:
   - Verify presence of `.agents/rules/`, `.agents/skills/`, `AGENTS.md`, and `CHANGELOG.md`.
   - Ensure rule files adhere to `markdown-style-ai.md` formatting.
