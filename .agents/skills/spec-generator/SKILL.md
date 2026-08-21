---
name: spec-generator
description: 'Generate AI-ready specification files following project template and naming conventions'
---

# Specification Generator Skill

## Overview

- Validate input to ensure `SpecPurpose` is clear and complete.
- Assess scope to detect broad or ambiguous specs for splitting.
- Plan spec type as architecture, design, process, infrastructure, data, schema, or tool.
- Check conflicts to verify no duplicates exist.
- Generate file with correct naming and frontmatter.
- Structure content by filling template sections systematically.
- Validate output for AI-readiness before finalization.

## Core Principles

### Principle 1: AI-Ready First

- Use unambiguous language without idioms or metaphors.
- Ensure self-contained context without external dependencies.
- Define testable acceptance criteria.
- Structure machine-readable output with headings, lists, tables, code blocks.

### Principle 2: Never Implement Code

**CRITICAL RULE**: NO FILE MUST BE WRITTEN/MODIFIED except the specification being created and temporary files necessary for its creation.
- [X] Create/edit ONLY the markdown specification file and required temporary files.
- [X] Read actual source code to understand current implementation.
- [X] Run tests to validate understanding.
- [ ] Avoid modifying existing source code.
- [ ] Avoid executing commands that change existing code.
- [ ] Avoid implementing features directly.

### Principle 3: Structured Templates

- Follow standard template structure with 13 required sections.

### Principle 4: Convention Over Configuration

- Use established naming conventions, terminology, and patterns from existing specs.

### Principle 5: Always Read Attached Context

**MANDATORY**: Read ALL attached documents before starting the Clarification Loop.
- Extract requirements, constraints, and domain knowledge from attached content.
- Avoid asking clarification questions already answered in attached content.
- Treat attached source code as ground truth for system behaviour.
- Document all attached files in Section 13.

### Principle 6: Markdown Style Rules

**MANDATORY**: Comply with `.agents/rules/markdown-style-ai.md`.
- Use `[X]` / `[ ]` / `[~]` for checkboxes.
- Use backtick-wrapped paths instead of markdown links.
- Avoid HTML tags.
- Prefer bullet lists over prose.
- Limit nesting depth to 2.
- Limit to one sentence per bullet.
- Include language specifiers in code blocks.
- Use tables only when 3+ items share 2+ attributes.
- Cut any word that does not add new information.

### Principle 7: Visual Diagrams with Mermaid (Optional)

- Include Mermaid diagrams for complex structural flows (multi-step workflows, branching logic, state transitions, system interactions) ONLY when they enhance visual clarity.
- Diagrams supplement prose and must never replace text descriptions.
- **MANDATORY**: Whenever Mermaid diagrams are needed, invoke `.agents/skills/mermaid-flow-diagrams/SKILL.md`.


## Clarification Loop

- Execute clarification loop as defined in `.agents/skills/prompt-clarifier/SKILL.md`.
- Use the `qa` skill for interaction mechanism.
- Ask one question at a time with suggested answers.
- Run before classifying the spec type.
- Limit to maximum 3 iterations.
- Record all questions, answers, and assumptions in the spec Clarification Log section.

### Loop Decision Flow

- Receive SpecPurpose.
- Check if SpecPurpose is fully unambiguous.
  - If YES: Document "No clarification needed" in Clarification Log.
  - If NO: Proceed to Decision Tree.
    - Ask Question 1.
    - Record in Clarification Log.
    - Re-evaluate clarity.
      - If CLEAR: Proceed to Decision Tree.
      - If STILL UNCLEAR: Ask Question 2.
        - Record in Clarification Log.
        - Re-evaluate clarity.
          - If CLEAR: Proceed to Decision Tree.
          - If STILL UNCLEAR: Ask Question 3.
            - Record in Clarification Log and document Assumptions.
            - Proceed to Decision Tree.

## Scope & Complexity Assessment

- Execute assessment immediately after the Clarification Loop.
- Detect specs too ambiguous or broad to produce a single document.

### Ambiguity Signals

| Signal | Example |
|--------|---------|
| Undefined domain model | "gestione dei processi aziendali" |
| Implicit business rules | "standard approval workflow" |
| Unnamed external system | "integration with external system" |

### Scope Breadth Signals

| Signal | Example |
|--------|---------|
| Multiple tech stacks | Backend C# + Frontend Angular in same spec |
| Multiple skills required | Needs `expert-dotnet` and `expert-angular` |
| Independent delivery units | Parts built and deployed independently |
| Multiple spec types | Architecture + design + data in single request |

### Failure Criteria

| ID | Criterion |
|----|----------|
| FA-001 | Contains undefined domain concepts driving structural decisions |
| FA-002 | Spans 2+ independent technology domains |
| FA-003 | Requires 2+ agent skills to implement |
| FA-004 | Final Interpretation contains conjunctions connecting separate systems |

### When Assessment Fails

- Notify user with structured message containing failed criteria.
- Propose split into multiple specs.
- Wait for user confirmation before proceeding.
- If user says NO: Stop and ask user to refine SpecPurpose.
- If user says YES: Generate split plan document and stop.

### Proposed Split Example

| Order | Spec | Rationale |
|-------|------|-----------|
| 1 | `spec-process-business-definitions.md` | Domain must be defined first |
| 2 | `spec-architecture-backend-api.md` | Backend depends on domain model |
| 3 | `spec-design-frontend.md` | Frontend depends on API contracts |

### Assessment Decision Flow

- Evaluate FA-001 through FA-004.
- If ALL PASS: Proceed to Decision Tree.
- If ANY FAIL: Notify user with structured warning and proposed split.
  - Wait for user confirmation.
  - If NO: Stop and ask user to narrow SpecPurpose.
  - If YES: Create `spec-split-plan-{topic}.md` in `/specs/` and stop.

## Determine Spec Type

- Technical architecture, technology choices -> ARCHITECTURE.
- User interface, visual design -> DESIGN.
- Workflows, development processes -> PROCESS.
- DevOps, deployment pipelines -> INFRASTRUCTURE.
- Database schemas, data models -> DATA.
- API contracts, interfaces -> SCHEMA.
- Developer tools, build systems -> TOOL.

## Naming Convention

Format: `[prefix]_spec-[type]-[description].md`

- Generate progressive numeric prefix by evaluating last spec in `/specs/implemented/`.
- Use lowercase type from decision tree.
- Use lowercase slug with hyphens for description.
- Limit maximum length to 80 characters.
- Avoid special characters except hyphens and underscores.

### Examples

- Valid: `000_spec-architecture-jwt-auth-api.md`
- Valid: `001_spec-infrastructure-azure-cicd-pipeline.md`
- Invalid: `spec-architecture-jwt-auth-api.md`
- Invalid: `004_spec-JwtAuthApi.md`

## Conflict Detection Workflow

- Search existing specs in `/specs/` for similar titles using `tokensave_search`.
- Check for duplicate specs in `/specs/implemented/`.
- Identify duplicate requirements or overlapping scope.
- Identify contradicting technology choices.
- Document relationship in `related_specs` frontmatter for partial overlap.
- Update `supersedes` field and document rationale for conflict.
- Document conflicts in new spec under Conflict Analysis section.
- Run `scripts/check-conflicts.py` to validate.

## Source Code Review

**MANDATORY**: Read source code before writing Requirements or Architecture sections.

| Area | Where to look | What to extract |
|---|---|---|
| Entities & data model | DAL project | Property names, types, constraints, relationships |
| Service interfaces | Services project | Method signatures, parameter names, return types |
| Service implementations | Services.Components project | Business rules, query patterns, validation logic |
| API endpoints | API project Endpoints folder | Route patterns, HTTP methods, request shapes |
| DTOs / Records | Models project | DTO structure, nullable fields, naming |
| Tests | `*.Tests/` | Observed behaviour, edge cases |

- Use TokenSave MCP tools as primary way to review source code.
- Use `headroom_compress` for files larger than 150 lines.
- Align all entity names in spec with names found in source code.
- Flag any discrepancy between source code and spec intent in Section 12.
- Record source files read in Section 13.

## Template Sections

Include these sections in order:

| # | Section | Purpose | Key Content |
|---|---------|---------|-------------|
| 1 | Purpose & Scope | Define what spec covers | In/out scope, audience, assumptions |
| 2 | Definitions & Terminology | Define all terms | Acronyms, domain terms |
| 3 | Requirements & Constraints | Specify required behaviour | Functional, non-functional, security |
| 4 | Architecture & Interfaces | Describe system design | Architecture diagrams (Mermaid), API contracts |
| 5 | Dependencies & Integrations | List all dependencies | Tech stack, external services |
| 6 | Acceptance Criteria | Define success | Testable, measurable criteria |
| 7 | Test Automation Strategy | Plan testing | Test levels, frameworks |
| 8 | Examples & Edge Cases | Provide concrete examples | Code samples, error scenarios |
| 9 | Spec Validation & AI-Readiness | Self-check completeness | Checklist |
| 10 | References & Instructions | Link project rules | Related specs, `.agents/rules/*.md` |
| 11 | Task Breakdown | Atomic implementation tasks | YAML tasks with objectives |
| 12 | Conflict Detection | Document conflicts found | Conflict IDs, resolutions |
| 13 | Files Added to Context | Log all files referenced | Specs, code samples |

## Frontmatter Requirements

- Include YAML frontmatter.
- Keep values machine-readable.

```yaml
---
title: 'Concise descriptive title'
version: '1.0.0'
date_created: 'YYYY-MM-DD HH:mm:ss'
last_updated: 'YYYY-MM-DD HH:mm:ss'
tags: [type, domain]
git_commit: ''
git_branch: ''
status: draft
related_specs: []
supersedes: []
source_purpose: 'Original SpecPurpose input'
---
```

## Requirement Language

Use explicit keywords in all requirements sections:

| Keyword | Meaning | Usage |
|---------|---------|-------|
| MUST / SHALL | Mandatory requirement | REQ-001: System MUST authenticate users |
| MUST NOT | Explicit prohibition | SEC-001: System MUST NOT log passwords |
| SHOULD | Strongly recommended | PERF-001: System SHOULD maintain 99.9% uptime |
| SHOULD NOT | Avoid this approach | SEC-002: System SHOULD NOT use hardcoded secrets |
| MAY | Optional capability | API-001: System MAY support multiple auth |

## Task Breakdown Format

Format tasks in section 12 as atomic YAML:

```yaml
tasks:
  - id: TASK-001
    title: "Create User entity and database migration"
    type: code
    priority: critical
    estimated_effort: small
    dependencies: []
    objective: |
      Clear statement of what this task accomplishes
    preconditions:
      - Database accessible
    acceptance_criteria:
      - AC: Specific, measurable criterion
    implementation_hints:
      - Suggested approach
    files_to_create:
      - path: /src/file.cs
        reason: Why this file is needed
    validation:
      - Run: command to verify
    estimated_completion: 2 hours
```

## Spec Validation Checklist

Verify spec meets ALL criteria before finalizing:

- [ ] Use unambiguous language without idioms.
- [ ] Define all acronyms and terms in section 2.
- [ ] Use MUST/SHALL/SHOULD/MAY keywords for requirements.
- [ ] Define measurable acceptance criteria.
- [ ] Ensure self-contained context without unstated assumptions.
- [ ] Use structured headings, lists, tables, code blocks.
- [ ] Ensure independent and atomic task granularity.
- [ ] Map all dependencies with integration details.
- [ ] Document edge cases and error handling.
- [ ] Provide concrete code examples.
- [ ] Comply with `.agents/rules/markdown-style-ai.md`.
- [ ] If Mermaid diagrams are included for complex flows, confirm `.agents/skills/mermaid-flow-diagrams/SKILL.md` was invoked.

Run `scripts/validate-spec.py` for automated checks.

## File Storage

- Store all new specs in `/specs/`.
- Move specs to `/specs/implemented/` only when 100% complete.
- Remove `spec-` prefix when moving to implemented folder.

## Content Generation Tips

- Start with Purpose & Scope to clarify problem and boundaries.
- Define all domain terms before requirements using a table.
- Group requirements by type using tables or numbered lists.
- Provide request/response JSON examples for all API endpoints.
- Break implementation into atomic tasks.
- Use concrete examples for complex behavior.

## Common Issues & Solutions

- **Vague SpecPurpose**: Request clarification to define precise scope.
- **Section Count**: Include all 13 sections and mark inapplicable sections as N/A.
- **Lengthy Spec**: Split specs exceeding 10,000 words into parent and child specs.
- **Code Implementation**: Avoid implementing code while writing specs.
- **Conflicts**: Document conflicts in section 12 and update supersedes field.
- **Multiple Technologies**: Follow Scope & Complexity Assessment to split spec.
- **Undefined Domain**: Create process or architecture spec to define domain first.
- **Project Instructions**: Reference `.agents/rules/*.md` files in section 10.
- **Source Code Review**: Read source code to ensure spec aligns with implementation.

## Workflow

- Prepare input by running Clarification Loop.
- Assess scope to evaluate failure criteria.
- Determine spec type using decision tree.
- Verify naming convention.
- Check conflicts in existing specs.
- Review source code relevant to domain.
- Initialize spec skeleton using `scripts/init-spec.py`.
- Fill content systematically following template.
- Validate compliance using `scripts/validate-spec.py`.
- Save finalized spec to `/specs/` directory.

## Reference Files

- `.agents/rules/`: Project rules and full spec template.
- `/specs/`: Active specifications.
- `/specs/implemented/`: Completed implementations.
- `.agents/skills/spec-generator/SKILL.md`: Wrapper prompt for spec creation.
- `.agents/skills/mermaid-flow-diagrams/SKILL.md`: Guidelines and syntax for Mermaid flow, sequence, state, and ER diagrams.
