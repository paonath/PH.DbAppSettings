# Specification Validation Rules

Specifications MUST pass all validation checks to be considered ready for implementation.

## Validation Categories

### 1. Frontmatter Validation

Every specification file MUST include valid YAML frontmatter with these required fields:

| Field | Type | Rules | Example |
|-------|------|-------|---------|
| `title` | string | 1-80 characters, descriptive | `JWT Authentication with Refresh Tokens` |
| `version` | string | Semantic versioning (X.Y.Z) | `1.0.0` |
| `date_created` | string | ISO 8601 format (YYYY-MM-DD HH:mm:ss) | `2025-01-16 14:30:00` |
| `last_updated` | string | ISO 8601 format, update when modified | `2025-01-16 15:45:00` |
| `owner` | string | Team or individual name | `Backend Team` |
| `tags` | array | [category, domain, focus] | `[feature, backend, api]` |
| `git_commit` | string | Git commit hash when created | `abc123def456` |
| `git_branch` | string | Branch name | `feature/auth-system` |
| `status` | string | One of: draft, review, approved, implemented, deprecated | `draft` |
| `related_specs` | array | Related specification filenames | `[spec-related-1.md, spec-related-2.md]` |
| `supersedes` | array | Specs this one replaces | `[]` or `[old-spec.md]` |
| `ai_agent_version` | string | AI agent that created this | `Claude Haiku 4.5` |
| `source_purpose` | string | Original SpecPurpose input | `Implement JWT authentication...` |

**Validation**:
```bash
python .agents/skills/spec-generator/scripts/validate-spec.py spec-file.md
```

### 2. Structure Validation

Specification MUST include all 15 required sections in order:

```
1. Purpose & Scope
2. Definitions & Terminology
3. Requirements & Constraints
4. Architecture & Interfaces
5. Dependencies & External Integrations
6. Acceptance Criteria
7. Test Automation Strategy
8. Examples & Edge Cases
9. Validation Criteria
10. AI-Readiness Checklist
11. Related Specifications & References
12. Task Breakdown for Implementation
13. Conflict Detection & Resolution
14. Files Added to Context
15. Always Follow Project Instructions
```

**Rules**:
- All 15 sections MUST be present (minimum)
- Sections MUST appear in this order
- Each section MUST have a heading: `## N. Section Name`
- No section can be skipped (mark as "N/A" with brief explanation if not applicable)

### 3. Naming Convention Validation

Filename MUST follow pattern: `spec-[type]-[description].md`

**Rules**:
- Starts with `spec-`
- One valid type (architecture, design, process, infrastructure, data, schema, tool, bugfix)
- Description: lowercase, hyphens only, 2-50 characters
- Ends with `.md`
- Total length ≤ 80 characters
- No duplicates in `/specs/` or `/specs/implemented/`

**Validation**:
```bash
python .agents/skills/spec-generator/scripts/validate-spec.py spec-file.md
# Check: filename validation in output
```

### 4. AI-Readiness Validation

Specification MUST be machine-readable and unambiguous:

| Check | Criteria | How to Validate |
|-------|----------|-----------------|
| **Unambiguous Language** | No idioms, metaphors, cultural references | Search for: "should", "might", "try", "possibly" |
| **Complete Definitions** | All acronyms and terms in section 2 | Compare acronyms in spec vs. section 2 |
| **Explicit Requirements** | MUST/SHALL/SHOULD/MAY keywords in section 3 | Count: MUST (≥10), SHALL (≥5), SHOULD (≥3) |
| **Testable Criteria** | Section 6 has measurable acceptance criteria | Each AC is specific and quantifiable |
| **Self-Contained** | No external context dependencies | Can read spec without external files |
| **Structured Format** | Headings, lists, tables, code blocks | Proper Markdown formatting |
| **Task Granularity** | Section 12 tasks are atomic (~2-4 hours) | Each task independent and completable |
| **Dependency Clarity** | Section 5 lists all dependencies | External services, libraries, platforms documented |
| **Error Scenarios** | Section 8 includes edge cases | Error handling, edge cases documented |
| **Examples Provided** | Code/config examples for complex sections | Section 4 and 8 have code examples |

**Checklist**:
```
- [ ] Unambiguous Language
- [ ] Complete Definitions
- [ ] Explicit Requirements
- [ ] Testable Criteria
- [ ] Self-Contained
- [ ] Structured Format
- [ ] Task Granularity
- [ ] Dependency Clarity
- [ ] Error Scenarios
- [ ] Examples Provided
```

**Target**: ≥8 of 10 checks passed to be AI-ready

### 5. Requirements Language Validation (RFC 2119)

Section 3 MUST use explicit keywords:

| Keyword | Count | Example |
|---------|-------|---------|
| MUST / SHALL | ≥10 | "System MUST authenticate users" |
| MUST NOT | ≥2 | "API MUST NOT expose passwords" |
| SHOULD / SHOULD NOT | ≥3 | "System SHOULD log all events" |
| MAY | ≥1 | "API MAY support multiple auth methods" |

**Rules**:
- Each requirement MUST have unique ID (REQ-001, SEC-001, etc.)
- Requirements MUST include reason/rationale
- Avoid "will", "is", "are" - use MUST/SHALL/SHOULD/MAY

**Validation**:
```
Search for keywords:
- MUST: minimum 10 occurrences
- SHALL: minimum 5 occurrences
- SHOULD: minimum 3 occurrences
- MAY: minimum 1 occurrence
```

### 6. Task Breakdown Validation

Section 12 MUST include atomic tasks in YAML format:

**Requirements per task**:
- `id`: Unique identifier (TASK-001, TASK-002, etc.)
- `title`: Clear, concise title (5-10 words)
- `type`: code | test | documentation | infrastructure | design
- `priority`: critical | high | medium | low
- `estimated_effort`: small (<2h) | medium (2-8h) | large (>8h)
- `dependencies`: Array of task IDs this depends on
- `objective`: Clear statement of what task accomplishes
- `preconditions`: What must be true before starting
- `acceptance_criteria`: Minimum 2 measurable criteria (AC: ...)
- `implementation_hints`: 2+ suggested approaches
- `files_to_create`: Array with path and reason for each
- `validation`: Commands or steps to verify completion
- `estimated_completion`: Time estimate (e.g., "2 hours")

**Rules**:
- Minimum 3 tasks required
- Each task atomic and independently executable
- No circular dependencies
- Estimated effort total should match project timeline
- All AC must be testable and measurable

### 7. Conflict Detection Validation

Section 13 MUST analyze conflicts with existing specs:

**Rules**:
- Identify ALL related existing specs
- Document actual conflicts (not just similarities)
- Provide resolution strategy for each conflict
- If superseding old spec, clearly document rationale
- If multiple related specs, link them in frontmatter

**Conflict table format**:
```
| Conflict ID | Conflicting Spec | Description | Resolution |
|-------------|------------------|-------------|------------|
| CNF-001 | old-spec.md | Different approach | This spec supersedes old spec |
```

### 8. Documentation Validation

**Rules**:
- All referenced files MUST exist or be created by tasks
- All links MUST be valid (relative paths)
- External links MUST use valid URLs
- Code examples MUST be syntactically correct
- No lorem ipsum or placeholder text (except marked as example)

**Check external links**:
- RFC links: https://tools.ietf.org/html/
- GitHub links: https://github.com/
- Documentation links: Version-appropriate URLs

### 9. Section Content Validation

| Section | Minimum Content | Required Elements |
|---------|-----------------|-------------------|
| 1. Purpose & Scope | 100 words | In/Out of scope, assumptions |
| 2. Definitions | 3+ terms | All acronyms defined |
| 3. Requirements | REQ, NFR, SEC | At least 10 requirements |
| 4. Architecture | 200 words or diagram | System design, API examples |
| 5. Dependencies | 3+ items | Tech stack, platforms, services |
| 6. Acceptance Criteria | 5+ criteria | All testable, Given-When-Then |
| 7. Test Strategy | All levels | Unit, integration, E2E |
| 8. Examples | 2+ examples | Success case, edge cases |
| 9. Validation | Checklist | 8+ items |
| 10. AI-Readiness | 10 checkboxes | ≥8 checked |
| 11. References | 2+ specs or docs | Related and external |
| 12. Tasks | 3+ tasks | Full YAML structure |
| 13. Conflicts | Analysis table | Related specs identified |
| 14. Files | 2+ files | Context recorded |
| 15. Instructions | References | Links to .agents/rules/ |

### 10. Language & Clarity Validation

**Check for**:
- Present tense: "The system provides..." (not "will provide")
- Passive voice avoided: "Users MUST authenticate" (not "authentication must be done")
- Abbreviations defined: First use should be "Term (ABBR)"
- Technical terms precise: "timeout" not "wait"
- Measurements specific: "< 200ms" not "fast"

**Tools**:
```bash
# Search for problematic words:
grep -i "should be" spec-file.md           # Passive voice
grep -i "will\|might\|could" spec-file.md  # Ambiguous
grep -E "TODO|FIXME|TBD" spec-file.md      # Incomplete
```

## Validation Checklist

Before finalizing specification:

- [ ] Frontmatter complete and valid YAML
- [ ] All 15 sections present and in order
- [ ] Filename follows naming convention
- [ ] Conflict check passed (no duplicates)
- [ ] AI-readiness: ≥8/10 checks passed
- [ ] Requirements use RFC 2119 keywords
- [ ] Tasks are atomic and structured
- [ ] No unambiguous language
- [ ] All links valid
- [ ] No placeholder or incomplete content

## Running Validation

```bash
# Full validation
python .agents/skills/spec-generator/scripts/validate-spec.py spec-architecture-jwt-auth.md

# Check for conflicts
python .agents/skills/spec-generator/scripts/check-conflicts.py "JWT authentication with refresh tokens" --type architecture

# Both in CI/CD:
make validate-spec FILE=specs/spec-architecture-jwt-auth.md
```

## Common Validation Errors

| Error | Fix |
|-------|-----|
| "Missing section: ## 2. Definitions" | Add missing section header |
| "Invalid status 'wip'" | Change to draft/review/approved/implemented/deprecated |
| "Only 4/10 AI-readiness checks passed" | Add missing definitions, clarify ambiguous language |
| "Only 2 tasks found, need ≥3" | Break down implementation into more atomic tasks |
| "Filename exceeds 80 characters" | Shorten description slug |
| "Conflicting spec found: spec-old-api.md" | Update `related_specs` or `supersedes` field |

---

**Reference**: Use `scripts/validate-spec.py` for automated validation before submission.
