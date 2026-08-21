#!/usr/bin/env python3
"""Initialize a specification file from template.

Usage:
    python init-spec.py --purpose '<purpose>' --type TYPE --file OUTPUT_FILE

Output:
    Creates specification file with populated frontmatter and section placeholders
"""

import json
import sys
import re
from datetime import datetime
from pathlib import Path


def slugify(text):
    """Convert text to slug format (lowercase, hyphens)."""
    text = text.lower()
    text = re.sub(r'[^a-z0-9]+', '-', text)
    text = text.strip('-')
    return text


def create_spec_file(purpose, spec_type, output_file, git_commit=None, git_branch=None):
    """Create a new specification file."""
    
    # Validate inputs
    if not purpose or len(purpose.strip()) < 10:
        return {
            "status": "error",
            "error": "Purpose must be at least 10 characters",
            "purpose_provided": purpose
        }
    
    valid_types = ['architecture', 'design', 'process', 'infrastructure', 'data', 'schema', 'tool', 'bugfix']
    if spec_type not in valid_types:
        return {
            "status": "error",
            "error": f"Invalid spec type. Must be one of: {', '.join(valid_types)}",
            "type_provided": spec_type
        }
    
    # Generate filename if not provided
    if not output_file:
        description_slug = slugify(purpose)[:50]  # Max 50 chars for description
        output_file = f"spec-{spec_type}-{description_slug}.md"
    
    # Validate filename
    if not output_file.endswith('.md'):
        output_file += '.md'
    
    if not output_file.startswith('spec-'):
        output_file = 'spec-' + output_file
    
    # Create frontmatter
    now = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
    
    frontmatter = f"""---
title: {purpose[:80]}
version: 1.0.0
date_created: {now}
last_updated: {now}
owner: Team or individual responsible
tags: [feature|bugfix, backend|frontend|fullstack, api|ui|database|infrastructure]
git_commit: {git_commit or 'TBD'}
git_branch: {git_branch or 'TBD'}
status: draft
related_specs: []
supersedes: []
ai_agent_version: Claude Haiku 4.5
source_purpose: {purpose}
---

# {purpose[:80]}

[Brief 2-3 sentence introduction describing what this specification addresses and its intended outcome]

## 1. Purpose & Scope

**Purpose**: [Clear statement of what this specification aims to achieve]

**Scope**:
- **In Scope**: [What is covered by this specification]
- **Out of Scope**: [What is explicitly NOT covered]

**Intended Audience**: [Who should read/implement this]

**Assumptions**:
- [Assumption 1]
- [Assumption 2]

## 2. Definitions & Terminology

| Term | Definition |
|------|------------|
| [Term] | [Definition] |

## 3. Requirements & Constraints

### 3.1 Functional Requirements

- **REQ-001**: [Requirement using MUST/SHALL/SHOULD/MAY]

### 3.2 Non-Functional Requirements

- **NFR-001**: [Performance, availability, or scalability requirement]

### 3.3 Security Requirements

- **SEC-001**: [Security requirement]

### 3.4 Compliance Requirements

- **COM-001**: [Compliance requirement if applicable]

### 3.5 Constraints

- **CON-001**: [Explicit limitation or constraint]

### 3.6 Guidelines & Best Practices

- **GUD-001**: [Recommended approach]

## 4. Architecture & Interfaces

### 4.1 System Architecture

[Description or ASCII diagram of system architecture]

### 4.2 API Contracts

[API endpoint specifications with request/response examples]

### 4.3 Data Models

[Data structure definitions and database schemas]

## 5. Dependencies & External Integrations

### 5.1 Architectural Dependencies

- [Technology choice and rationale]

### 5.2 External System Integrations

- [External service integrations]

### 5.3 Platform & Runtime Requirements

- [Platform and runtime specifications]

### 5.4 Third-Party Services

- [Third-party service requirements]

### 5.5 Implementation Dependencies (Informational)

- [Recommended libraries and packages]

## 6. Acceptance Criteria

- **AC-001**: [Testable acceptance criterion in Given-When-Then format]

## 7. Test Automation Strategy

### 7.1 Test Levels

- **Unit Tests**: [Test coverage and framework]
- **Integration Tests**: [Test coverage and framework]
- **End-to-End Tests**: [Test coverage and framework]

### 7.2 Test Data Management

[Approach for test data management]

### 7.3 CI/CD Integration

[CI/CD pipeline requirements]

### 7.4 Performance Testing

[Performance testing requirements]

## 8. Examples & Edge Cases

### 8.1 Successful Flow

[Example code showing successful execution]

### 8.2 Edge Cases

[Edge cases with handling]

## 9. Validation Criteria

- [ ] All sections of this template are filled out
- [ ] All requirements have unique IDs and explicit MUST/SHALL/SHOULD/MAY language
- [ ] All acceptance criteria are testable and measurable
- [ ] All dependencies are documented with rationale
- [ ] All API contracts include request/response examples
- [ ] Security requirements include threat model
- [ ] Task breakdown section is complete with atomic tasks
- [ ] AI-Readiness Checklist passes all items
- [ ] No conflicts with existing specifications

## 10. AI-Readiness Checklist

- [ ] **Unambiguous Language**: No idioms, metaphors, or context-dependent terms used
- [ ] **Complete Definitions**: All acronyms and domain terms defined in section 2
- [ ] **Explicit Requirements**: All requirements use MUST/SHALL/SHOULD/MAY keywords
- [ ] **Testable Criteria**: All acceptance criteria are measurable and verifiable
- [ ] **Self-Contained**: Document does not rely on external context or unstated assumptions
- [ ] **Structured Format**: Proper use of headings, lists, tables, code blocks for parsing
- [ ] **Task Granularity**: Each task is atomic and independently executable
- [ ] **Dependency Clarity**: All dependencies clearly mapped with integration details
- [ ] **Error Scenarios**: Edge cases and error handling explicitly documented
- [ ] **Examples Provided**: Concrete code examples for critical paths

## 11. Related Specifications & References

### Related Specifications

[Links to related specification files]

### External Documentation

[Links to external documentation, RFCs, official docs]

## 12. Task Breakdown for Implementation

[Atomic tasks in YAML format - see template for structure]

```yaml
tasks:
  - id: TASK-001
    title: "[Task title]"
    type: code
    priority: critical
    estimated_effort: small
    dependencies: []
    
    objective: |
      [What this task accomplishes]
    
    preconditions:
      - [Precondition 1]
    
    acceptance_criteria:
      - AC: [Criterion 1]
    
    implementation_hints:
      - [Hint 1]
    
    files_to_create:
      - path: [file path]
        reason: [why this file]
    
    validation:
      - Run: [command to verify]
    
    estimated_completion: 2 hours
```

## 13. Conflict Detection & Resolution

### Conflict Analysis

| Conflict ID | Conflicting Spec | Conflict Description | Resolution Strategy |
|-------------|------------------|---------------------|---------------------|
| CNF-001 | [Spec name] | [Conflict description] | [Resolution] |

**Resolution Notes**:
- [Details about conflict resolution]

## 14. Files Added to Context

[List files read or referenced during specification creation]

## 15. Always Follow Project Instructions

This specification adheres to the following project-wide instructions:

[Reference applicable instruction files from `.agents/rules/`]

---

**Next Steps**:

1. Complete all sections marked with brackets [...]
2. Run validation: `python .agents/skills/spec-generator/scripts/validate-spec.py {output_file}`
3. Check conflicts: `python .agents/skills/spec-generator/scripts/check-conflicts.py '{purpose}' --type {spec_type}`
4. Move to `/specs/` when complete

**Status**: This is a template skeleton. Replace all bracketed placeholders with actual content.
"""
    
    return {
        "status": "success",
        "output_file": output_file,
        "content_length": len(frontmatter),
        "spec_type": spec_type,
        "filename_slug": slugify(purpose)[:50]
    }


def write_spec_file(filename, content):
    """Write specification file to disk."""
    try:
        Path(filename).write_text(content, encoding='utf-8')
        return {
            "status": "success",
            "file": str(filename),
            "bytes_written": len(content)
        }
    except Exception as e:
        return {
            "status": "error",
            "error": str(e),
            "file": str(filename)
        }


if __name__ == "__main__":
    import argparse
    
    parser = argparse.ArgumentParser(description="Initialize a specification file from template")
    parser.add_argument("--purpose", required=True, help="Specification purpose")
    parser.add_argument("--type", required=True, help="Specification type (architecture/design/process/infrastructure/data/schema/tool/bugfix)")
    parser.add_argument("--file", required=False, help="Output filename (auto-generated if not provided)")
    parser.add_argument("--commit", required=False, help="Git commit hash")
    parser.add_argument("--branch", required=False, help="Git branch name")
    
    args = parser.parse_args()
    
    result = create_spec_file(
        purpose=args.purpose,
        spec_type=args.type,
        output_file=args.file,
        git_commit=args.commit,
        git_branch=args.branch
    )
    
    print(json.dumps(result, indent=2))
    sys.exit(0 if result["status"] == "success" else 1)
