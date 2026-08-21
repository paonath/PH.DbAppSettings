---
title: "Specification: AGENTS.md Repository Governance and Architectural Boundaries"
version: "1.0.0"
date_created: "2026-08-21 10:44:00"
last_updated: "2026-08-21 10:44:00"
tags: [governance, agentsmd, ai-guidelines]
git_commit: ""
git_branch: "main"
status: ready
related_specs: ["specs/001_spec-tdd-key-normalization-and-options-binding.md", "specs/002_spec-tdd-storage-abstraction-and-multi-dialect-dapper.md", "specs/003_spec-tdd-efcore-modernization-and-reload-optimization.md", "specs/004_spec-tdd-cli-appsettings-tool.md"]
supersedes: []
source_purpose: "Establish comprehensive AGENTS.md instructions for root repository, core library, CLI tool, and test projects to guide AI agents and developers."
---

# Specification: AGENTS.md Repository Governance and Architectural Boundaries

## 1. Purpose & Scope

### 1.1 Problem Statement

The repository currently lacks `AGENTS.md` files, leaving AI coding agents without explicit component boundary definitions, technology standards, build/test commands, and architectural rules.

### 1.2 In-Scope

- Creation of `AGENTS.md` at repository root.
- Creation of `src/PH.DbAppSettings/AGENTS.md`.
- Creation of `src/PH.DbAppSettings.Cli/AGENTS.md`.
- Creation of `tests/PH.DbAppSettings.Tests/AGENTS.md`.

### 1.3 Out-of-Scope

- Non-agent documentation (handled in `README.md`).

---

## 2. Definitions & Terminology

| Term | Definition |
|---|---|
| `AGENTS.md` | Machine-readable instructions file defining repository rules, architectural layers, and development protocols for AI assistants. |

---

## 3. Requirements & Constraints

- **REQ-001**: Root `AGENTS.md` MUST specify target framework (`net10.0`), C# 14 standards, build/test commands, and security policies.
- **REQ-002**: `src/PH.DbAppSettings/AGENTS.md` MUST define component responsibilities for `Configuration/`, `Storage/`, `Data/`, `Encryption/`, and `Services/`.
- **REQ-003**: `src/PH.DbAppSettings.Cli/AGENTS.md` MUST document CLI command structure and parameters.
- **REQ-004**: `tests/PH.DbAppSettings.Tests/AGENTS.md` MUST specify TDD discipline (Red -> Green -> Refactor) and test commands.

---

## 4. Acceptance Criteria

- **AC-001**:
  - **Given**: The workspace root and sub-project folders.
  - **When**: Inspecting the repository structure.
  - **Then**: `AGENTS.md` files exist in root, `src/PH.DbAppSettings/`, `src/PH.DbAppSettings.Cli/`, and `tests/PH.DbAppSettings.Tests/`.

---

## 5. Task Breakdown

```yaml
tasks:
  - id: TASK-009
    title: "Create Root AGENTS.md"
    type: documentation
    priority: medium
    objective: "Create repository root AGENTS.md with build commands and architecture standards."
    files_to_create:
      - path: "AGENTS.md"
        reason: "Root repository instructions."

  - id: TASK-010
    title: "Create Project-Level AGENTS.md Files"
    type: documentation
    priority: medium
    objective: "Create AGENTS.md in src/PH.DbAppSettings/, src/PH.DbAppSettings.Cli/, and tests/PH.DbAppSettings.Tests/."
    files_to_create:
      - path: "src/PH.DbAppSettings/AGENTS.md"
        reason: "Library instructions."
      - path: "src/PH.DbAppSettings.Cli/AGENTS.md"
        reason: "CLI tool instructions."
      - path: "tests/PH.DbAppSettings.Tests/AGENTS.md"
        reason: "Test suite instructions."
```

---

## 6. References & Instructions

- `.agents/rules/markdown-style-ai.md`
- `.agents/skills/create-agentsmd/SKILL.md`
