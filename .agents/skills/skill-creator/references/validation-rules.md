# Skill Validation Rules Reference

Complete validation checklist for AI Agent Skills compliance with Agent Skills standard.

## Table of Contents
1. [YAML Frontmatter Validation](#yaml-frontmatter-validation)
2. [Naming Conventions](#naming-conventions)
3. [Directory Structure](#directory-structure)
4. [Content Quality](#content-quality)
5. [Resource Organization](#resource-organization)
6. [Compliance Checklist](#compliance-checklist)

---

## YAML Frontmatter Validation

### Required Fields

Every SKILL.md must have YAML frontmatter with exactly two fields:

```yaml
---
name: skill-name
description: |
  Multi-line description...
---
```

### Field Specifications

#### `name` Field
- **Type**: String (lowercase, hyphens only)
- **Pattern**: `^[a-z0-9]+(-[a-z0-9]+)*$`
- **Max length**: 64 characters
- **Examples**: 
  - ✓ `skill-creator`, `json-validator`, `azure-role-selector`
  - ✗ `Skill Creator` (spaces), `skill_creator` (underscore), `skill-creator-example-tool` (too long)

#### `description` Field
- **Type**: String (multiline recommended using `|`)
- **Max length**: 1024 characters
- **Required elements**:
  - One-sentence purpose (what skill does)
  - "Use when:" with 3+ specific triggers/use cases
- **Example**:
  ```yaml
  description: |
    Create, validate, package, and distribute AI Agent Skills for VS Code, CLI, and coding agent.
    Use when: (1) designing new AI Agent skills, (2) validating skill structure,
    (3) organizing skill resources, (4) packaging for AI Agent distribution
  ```

### Frontmatter Format

- **Delimiters**: Must start with `---` and end with `---`
- **Indentation**: Standard YAML (2-space indentation)
- **No extra fields**: Only `name` and `description` allowed (no metadata, version, author, etc.)
- **Syntax**: Valid YAML (use YAML linter to verify)

---

## Naming Conventions

### Skill Name Rules

1. **Characters**: Lowercase letters (a-z), numbers (0-9), hyphens (-) only
2. **Starting character**: Must start with lowercase letter
3. **Ending character**: Must not end with hyphen
4. **Hyphens**: Use as word separators; no consecutive hyphens
5. **Length**: Maximum 64 characters

### Examples

**Valid**:
- `code-review-guide`
- `json-validator`
- `pdf-processor`
- `azure-role-selector`
- `typescript-patterns`
- `nextjs-deployment-2024` (includes numbers)

**Invalid**:
- `Code-Review-Guide` (capitals)
- `code_review_guide` (underscore)
- `code--review` (consecutive hyphens)
- `-code-review` (starts with hyphen)
- `code-review-guide-tool-for-javascript-development` (exceeds 64 chars)

---

## Directory Structure

### Required Structure

Every skill must have at minimum:
```
skill-name/
└── SKILL.md
```

### Recommended Structure

```
skill-name/
├── SKILL.md (required)
├── scripts/ (optional: executable code)
│   ├── script1.py
│   ├── script2.sh
│   └── ...
├── examples/ (optional: code samples)
│   ├── example1.js
│   ├── example2.json
│   └── ...
└── references/ (optional: detailed guides)
    ├── advanced-patterns.md
    ├── troubleshooting.md
    └── ...
```

### Directory Rules

#### scripts/ Directory
- **Purpose**: Executable files for AI Agent terminal execution
- **Allowed types**: Python (.py), Bash (.sh), JavaScript (.js), Go (.go), etc.
- **Naming**: Use descriptive names (validate_skill.py, format_json.py)
- **Testing**: All scripts must be tested for deterministic reliability
- **Nesting**: Flat structure (no subdirectories within scripts/)

#### examples/ Directory
- **Purpose**: Code samples and templates for AI Agent output generation
- **Allowed types**: Any file type (JSON, YAML, JavaScript, HTML, etc.)
- **Naming**: Clear, descriptive names (basic-example.js, advanced-pattern.py)
- **Nesting**: Flat structure preferred; nested only if organizing by framework
- **Content**: Working, runnable examples with comments explaining key decisions

#### references/ Directory
- **Purpose**: Detailed documentation guides referenced from SKILL.md body
- **Allowed types**: Markdown (.md) files only
- **Naming**: Clear, semantic names (validation-rules.md, design-patterns.md)
- **Nesting**: **Flat only** - no subdirectories within references/
- **File size**: <10,000 words OR include table of contents if larger
- **Linking**: All files must be referenced from SKILL.md

### Flat Structure Requirement

**✗ Invalid** (nested references/):
```
references/
└── patterns/
    ├── async-patterns.md
    └── sync-patterns.md
```

**✓ Valid** (flat):
```
references/
├── async-patterns.md
└── sync-patterns.md
```

---

## Content Quality

### SKILL.md Body Requirements

1. **Imperative Form**: Use action-oriented language
   - ✓ "Use this pattern", "Generate code", "Run validation", "Execute script"
   - ✗ "This pattern is...", "It is recommended that...", "You might consider..."

2. **Structure**: Organized with clear sections
   - `## Overview` - What skill does and when to use it
   - `## Step-by-Step Process` - Clear procedural guidance
   - `## Examples` - Inline examples for common use cases
   - `## [Domain] Patterns` - Domain-specific guidance if multi-domain
   - `## Troubleshooting` - Common issues and solutions

3. **Length**: Under 500 lines
   - If exceeding 500 lines, split detailed content into `references/` subdirectory
   - Keep SKILL.md focused on core guidance and decision-making

4. **Examples**: Include code examples
   - Use triple-backtick blocks: ` ```language\n code \n``` `
   - Provide practical, runnable examples
   - Include edge cases where relevant

5. **References**: Link to external resources correctly
   - Use relative paths: `[link text](./references/file.md)`
   - Not absolute paths or external URLs (unless necessary)
   - All referenced files must exist in skill directory

6. **No Duplication**: SKILL.md body and references should not duplicate content
   - Body: overview, decision trees, simple examples
   - References: detailed patterns, advanced topics, edge cases

### Markdown Syntax

- **Valid headers**: Use `#`, `##`, `###` (not multiple `#` on same line)
- **Code blocks**: Use triple backticks with language specification
- **Lists**: Use `-` for unordered, `1.` for ordered
- **Links**: Use `[text](path)` format
- **No unclosed elements**: All code blocks, quotes, and emphasis must be closed

---

## Resource Organization

### Script Best Practices

**Deterministic Reliability**:
```python
#!/usr/bin/env python3
"""Script description for AI Agent execution."""

import json
import sys

def main():
    """Main entry point - must output JSON for AI Agent."""
    if len(sys.argv) < 2:
        print(json.dumps({"error": "Missing argument"}))
        sys.exit(1)
    
    try:
        result = do_work(sys.argv[1])
        print(json.dumps(result))
        sys.exit(0)
    except Exception as e:
        print(json.dumps({"error": str(e)}))
        sys.exit(1)

if __name__ == "__main__":
    main()
```

**Key requirements**:
- Deterministic: Same input always produces same output
- Error handling: All exceptions caught and reported
- Output format: JSON for AI Agent parsing
- Exit codes: 0 for success, 1 for failure
- Timeout: Should complete within 30 seconds

### Example File Best Practices

- **Comment key decisions**: "Why this pattern?"
- **Show variations**: Provide multiple approaches if relevant
- **Mark important parts**: Highlight critical sections
- **Keep concise**: Provide working examples, not tutorials
- **Test thoroughly**: Examples should be runnable and tested

### Reference File Best Practices

- **Table of contents**: For files >100 lines, include TOC at top
- **Clear sections**: Use descriptive heading names
- **Link references**: Cross-reference between files using relative links
- **Edge cases**: Include edge cases and gotchas
- **Examples**: Reference examples from `examples/` directory when relevant
- **Avoid duplication**: Don't repeat content from SKILL.md body

---

## Compliance Checklist

### Pre-Validation Checklist

Before running validation script, verify:

- [ ] SKILL.md exists in skill directory root
- [ ] YAML frontmatter starts with `---` and has closing `---`
- [ ] `name` field present and matches skill directory name
- [ ] `description` field present and includes use-case triggers
- [ ] No extra fields in frontmatter (only name + description)
- [ ] Skill directory uses kebab-case naming (lowercase with hyphens)
- [ ] Only approved subdirectories present (scripts/, examples/, references/)

### Structural Validation

- [ ] File: SKILL.md exists
- [ ] Frontmatter: Valid YAML syntax
- [ ] Frontmatter: Has exactly `name` and `description` fields
- [ ] Name: Lowercase, hyphens only, max 64 characters
- [ ] Description: Max 1024 characters, includes 3+ use cases
- [ ] References: All internal links use relative paths
- [ ] References: All referenced files exist
- [ ] Scripts: Only executable files (no data files)
- [ ] Examples: Clear, descriptive naming
- [ ] References: Flat structure (no nested directories)

### Content Quality Validation

- [ ] Body: Uses imperative form ("Use", "Generate", "Run")
- [ ] Body: Includes overview section
- [ ] Body: Provides step-by-step guidance or decision trees
- [ ] Examples: Inline in SKILL.md for common cases
- [ ] References: Linked for complex/advanced content
- [ ] Body: Under 500 lines (else split to references/)
- [ ] No duplication: Between body and reference files
- [ ] Scripts: Tested for deterministic execution
- [ ] No credentials: No API keys, passwords, or secrets

### Compliance Validation

- [ ] Agent Skills standard: Follows agentskills.io requirements
- [ ] AI Agent compatibility: Works in VS Code, CLI, coding agent
- [ ] Portability: Uses only relative paths and standard formats
- [ ] Security: No sensitive data embedded
- [ ] Distribution: Ready for `.agents/skills/` or awesome-agent

### Quality Assurance

- [ ] README not included (only SKILL.md and resources)
- [ ] No extraneous files (changelogs, installation guides, etc.)
- [ ] Consistent formatting and tone throughout
- [ ] Clear naming conventions applied consistently
- [ ] All examples tested and working
- [ ] Scripts tested for AI Agent terminal execution

---

## Automation: Validation Script

Run automated validation:

```bash
python scripts/validate_skill.py .agents/skills/skill-name/
```

Expected output:
```json
{
  "status": "pass",
  "checks": {
    "skill_md_exists": true,
    "frontmatter_format": true,
    "frontmatter_required_fields": true,
    "skill_name_format": true,
    "description_length": true,
    "file_organization": true,
    "references_flat": true,
    "body_length": true,
    "content_quality": true,
    "resource_links": true
  },
  "errors": [],
  "warnings": []
}
```

---

## Troubleshooting Validation Failures

### Error: "SKILL.md not found"
- **Cause**: File missing from skill directory
- **Fix**: Create SKILL.md in skill directory root

### Error: "Invalid YAML frontmatter"
- **Cause**: YAML syntax error or missing `---` delimiters
- **Fix**: Ensure frontmatter starts/ends with `---` and follows YAML syntax

### Error: "Missing required field: 'name'"
- **Cause**: Frontmatter lacks `name` field
- **Fix**: Add `name: skill-name` (lowercase, hyphens only)

### Error: "Skill name must be lowercase with hyphens only"
- **Cause**: Name contains uppercase, underscores, or other invalid characters
- **Fix**: Rename to lowercase with hyphens: `code-review-guide` not `Code_Review_Guide`

### Error: "Description exceeds 1024 characters"
- **Cause**: Description field too long
- **Fix**: Condense description; remove examples or detailed explanations

### Error: "Broken links (files not found)"
- **Cause**: SKILL.md references files that don't exist
- **Fix**: Create referenced files or update links to existing files

### Warning: "references/ must be flat"
- **Cause**: Nested subdirectories within references/
- **Fix**: Move nested files to flat references/ directory level

### Warning: "SKILL.md body exceeds 500 lines"
- **Cause**: Body too long
- **Fix**: Split detailed content into reference files and link from SKILL.md

---

## Official Standards Reference

- **Agent Skills Standard**: https://agentskills.io/
- **VS Code Agent Skills**: https://code.visualstudio.com/docs/copilot/customization/agent-skills
- **GitHub Awesome Copilot**: https://github.com/github/awesome-copilot
