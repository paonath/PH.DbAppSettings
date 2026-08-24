# Design Patterns for AI Agent Skills

Reference guide for organizing AI Agent Skills by complexity level and domain structure.

## Table of Contents
1. [Pattern 1: High-Freedom Guidance Skills](#pattern-1-high-freedom-guidance-skills)
2. [Pattern 2: Medium-Freedom Script-Based Skills](#pattern-2-medium-freedom-script-based-skills)
3. [Pattern 3: Low-Freedom Multi-Domain Skills](#pattern-3-low-freedom-multi-domain-skills)
4. [Choosing Your Pattern](#choosing-your-pattern)
5. [Real-World Examples](#real-world-examples)

---

## Pattern 1: High-Freedom Guidance Skills

**When to use**: Multiple valid approaches; context-dependent decisions.

**Characteristics**:
- AI Agent needs general guidance, not deterministic operations
- Multiple equally-valid solutions exist
- Domain allows for flexibility and adaptation
- No complex file operations or schema validation needed

**Examples**: Code review guidance, architectural decisions, best practices, debugging strategies

### Structure

```
skill-name/
├── SKILL.md (all guidance inline)
└── (optional) examples/
    └── practical-example.md
```

### SKILL.md Template

```markdown
---
name: skill-name
description: |
  [One-sentence purpose]. Use when: (1) [trigger 1], (2) [trigger 2], (3) [trigger 3]
---

# Skill Name

## Overview
- What AI Agent can help with
- When to use this skill
- Key principles

## Principle 1: [Name]
[Guidance with examples]

## Principle 2: [Name]
[Guidance with examples]

## Decision Tree
```
When faced with [scenario]:
├─ If [condition]: Use [approach]
└─ Otherwise: Use [approach]
```

## Examples
[Inline code examples]
```

### Content Guidelines

- **Inline everything**: All guidance in SKILL.md body (no references)
- **Use examples sparingly**: Brief code samples, full examples in examples/
- **Provide decision trees**: Help AI Agent choose between approaches
- **Include principles**: State why approaches work, not just how
- **Keep concise**: High-freedom skills tend to be shorter (<300 lines)

### Example Skill

See `examples/minimal-skill.md` for a complete high-freedom skill example (Code Review Guide).

---

## Pattern 2: Medium-Freedom Script-Based Skills

**When to use**: Preferred patterns exist; deterministic operations needed.

**Characteristics**:
- AI Agent needs patterns/guidance + deterministic operations
- Scripts provide validation, conversion, or file operations
- Some variation acceptable; core logic must be consistent
- Reusable, testable scripts improve reliability

**Examples**: JSON validation, formatting, conversions; deployment validation; file processing

### Structure

```
skill-name/
├── SKILL.md (core guidance + script references)
├── scripts/
│   ├── validate_format.py
│   ├── convert_format.py
│   └── ...
├── examples/
│   ├── valid-example.json
│   ├── output-example.yaml
│   └── ...
└── (optional) references/
    └── advanced-patterns.md
```

### SKILL.md Template

```markdown
---
name: skill-name
description: |
  [Purpose]. Use when: (1) [trigger 1], (2) [trigger 2], (3) [trigger 3]
---

# Skill Name

## Overview
- What AI Agent can accomplish
- When to use scripts vs inline guidance

## Step 1: [Operation Name]

AI Agent executes:
```bash
python scripts/operation.py input.file
```

[Explanation of step]

### Example
[Input and expected output]

## Step 2: [Next Operation]

AI Agent runs:
```bash
python scripts/next_op.py input.file --option value
```

[Explanation]

## Script Reference

See [advanced-patterns.md](./references/advanced-patterns.md) for complex scenarios.

## Troubleshooting

[Common issues and solutions]
```

### Content Guidelines

- **Script integration**: Mention scripts explicitly with `python scripts/name.py`
- **Examples**: Provide input/output examples for each script
- **Simple inline**: Include simple cases inline in SKILL.md
- **Complex patterns**: Reference external files for advanced use cases
- **Body length**: 200-400 lines (core guidance + script usage)

### Script Template

```python
#!/usr/bin/env python3
"""
Skill script for AI Agent terminal execution.

Usage: python script.py <input> [options]
Output: JSON result for AI Agent parsing
"""

import json
import sys
from typing import Dict, Any

def process(input_file: str, **options) -> Dict[str, Any]:
    """Main processing logic."""
    try:
        # Processing logic here
        result = {"status": "success", "data": {}}
        return result
    except Exception as e:
        return {"status": "error", "error": str(e)}

def main():
    """Entry point for AI Agent."""
    if len(sys.argv) < 2:
        print(json.dumps({"error": "Usage: python script.py <input>"}))
        sys.exit(1)
    
    result = process(sys.argv[1])
    print(json.dumps(result, indent=2))
    sys.exit(0 if result.get("status") == "success" else 1)

if __name__ == "__main__":
    main()
```

### Example Skill

See `examples/medium-skill.md` for a complete medium-freedom skill example (JSON Validator).

---

## Pattern 3: Low-Freedom Multi-Domain Skills

**When to use**: Different frameworks/providers with incompatible patterns.

**Characteristics**:
- Multiple domains/frameworks with different best practices
- AI Agent must choose correct domain-specific approach
- Scripts and patterns vary by domain
- Consistency critical; little variation acceptable

**Examples**: Cloud deployment (AWS/GCP/Azure), language-specific tooling, framework-specific guides

### Structure

```
skill-name/
├── SKILL.md (domain selection + core guidance)
├── scripts/
│   ├── validate_domain1.py
│   ├── validate_domain2.py
│   └── ...
├── examples/
│   ├── domain1-example.yaml
│   ├── domain2-example.json
│   └── ...
└── references/
    ├── domain1-patterns.md
    ├── domain2-patterns.md
    ├── domain3-patterns.md
    └── troubleshooting.md
```

### SKILL.md Template

```markdown
---
name: skill-name
description: |
  [Purpose for all domains]. Use when: (1) [trigger 1], (2) [trigger 2], (3) [trigger 3]
---

# Skill Name

## Domain Selection

First, AI Agent identifies which domain applies:

```
What [scope] are you working with?
├─ Domain 1 (AWS, TypeScript, etc.)
├─ Domain 2 (GCP, Python, etc.)
├─ Domain 3 (Azure, Go, etc.)
└─ Multi-domain
```

## Domain 1: [Name]

### Overview
[Domain 1 specific guidance]

### Step-by-Step
1. [First step for domain 1]
2. [Second step for domain 1]

AI Agent validates:
```bash
python scripts/validate_domain1.py config.yaml
```

See [domain1-patterns.md](./references/domain1-patterns.md) for advanced patterns.

## Domain 2: [Name]

### Overview
[Domain 2 specific guidance]

### Step-by-Step
1. [First step for domain 2]
2. [Second step for domain 2]

AI Agent validates:
```bash
python scripts/validate_domain2.py config.json
```

See [domain2-patterns.md](./references/domain2-patterns.md) for advanced patterns.

## Domain 3: [Name]

[Similar structure]

## Decision Guide

Use [domain1] when:
- [Condition A]
- [Condition B]

Use [domain2] when:
- [Condition C]
- [Condition D]

## Common Issues

See [troubleshooting.md](./references/troubleshooting.md) for domain-specific issues.

## Comparison Table

| Aspect | Domain 1 | Domain 2 | Domain 3 |
|--------|----------|----------|----------|
| [Aspect 1] | [D1 value] | [D2 value] | [D3 value] |
| [Aspect 2] | [D1 value] | [D2 value] | [D3 value] |
```

### Content Guidelines

- **Domain selection first**: SKILL.md helps AI Agent choose domain
- **Domain sections**: Separate section for each domain
- **Comparison table**: Quick reference for domain differences
- **Validation per domain**: Different scripts for different domains
- **Body length**: 300-500 lines (domain selection + core guidance per domain)
- **References**: Detailed patterns in separate domain-specific files

### Reference File Organization

For each domain, create:
- `domain-name-patterns.md`: Detailed patterns and best practices
- `domain-name-examples.md` (if >10 examples): Organized examples
- `domain-name-troubleshooting.md` (if >5 issues): Domain-specific issues

Plus shared:
- `troubleshooting.md`: Cross-domain issues
- `comparison.md` (optional): Feature/capability comparison

### Example Skill

See `examples/complex-skill.md` for a complete low-freedom skill example (Cloud Deployment).

---

## Choosing Your Pattern

### Decision Tree for Pattern Selection

```
Does AI Agent need multiple valid approaches, or just one correct way?
├─ Multiple valid approaches
│  └─ Are the approaches domain-independent?
│     ├─ YES → Pattern 1 (High-freedom guidance)
│     └─ NO → Pattern 3 (Multi-domain)
│
└─ One correct way
   └─ Does it require deterministic scripts?
      ├─ YES → Pattern 2 (Script-based)
      └─ NO → Pattern 1 (High-freedom guidance)

Does AI Agent need to execute deterministic operations?
├─ YES → Use Pattern 2 or Pattern 3 (with scripts)
└─ NO → Use Pattern 1 or Pattern 3 (guidance-only)

Will this skill work across multiple frameworks/providers?
├─ YES → Use Pattern 3 (Multi-domain)
├─ Somewhat → Use Pattern 2 (Script-based, per-domain)
└─ NO → Use Pattern 1 or Pattern 2
```

### Complexity vs. Domain Count

| Complexity | Domains | Recommended Pattern |
|------------|---------|-------------------|
| Low (guidance only) | 1 | Pattern 1 |
| Low (guidance only) | 2+ | Pattern 3 |
| Medium (scripts needed) | 1 | Pattern 2 |
| Medium (scripts needed) | 2+ | Pattern 3 |
| High (deterministic + options) | 1+ | Pattern 2 or 3 |

---

## Real-World Examples

### Example 1: Code Review Guidance (Pattern 1)

**Skill**: `code-review-guide`
- All guidance inline in SKILL.md
- General principles apply across languages
- No external scripts (patterns are flexible)
- Optional: `examples/` with code review examples

**Files**:
- SKILL.md (guidelines + decision trees)
- examples/python-review-checklist.md
- examples/javascript-review-checklist.md

**Why Pattern 1**: Multiple valid review approaches; AI Agent adapts to context.

---

### Example 2: JSON Validation (Pattern 2)

**Skill**: `json-validator`
- Core guidance in SKILL.md
- Deterministic validation scripts
- Examples of valid/invalid JSON
- Reference for advanced schema patterns

**Files**:
- SKILL.md (overview + script usage + examples)
- scripts/validate_json.py (deterministic validation)
- scripts/format_json.py (deterministic formatting)
- scripts/validate_schema.py (schema validation)
- examples/config.json
- examples/api-response.json
- references/json-schema-patterns.md

**Why Pattern 2**: Validation must be deterministic; scripts ensure consistency.

---

### Example 3: Cloud Deployment (Pattern 3)

**Skill**: `cloud-deployment`
- Domain selection in SKILL.md (AWS/GCP/Azure)
- Provider-specific guidance for each domain
- Provider-specific validation scripts
- Provider-specific pattern references

**Files**:
- SKILL.md (domain selection + core guidance per domain)
- scripts/validate_aws.py, validate_gcp.py, validate_azure.py
- examples/aws-cloudformation.yaml, gcp-deployment.yaml, azure-template.bicep
- references/aws-patterns.md, gcp-patterns.md, azure-patterns.md
- references/multi-cloud-guide.md
- references/troubleshooting.md

**Why Pattern 3**: AWS, GCP, Azure have incompatible tools/patterns; each domain needs separate guidance.

---

## Progressive Disclosure in Patterns

All patterns use 3-level progressive disclosure:

### Level 1: Metadata (Always Available)
```yaml
name: skill-name
description: What skill does + triggers
```
AI Agent sees name + description, decides to load full SKILL.md.

### Level 2: SKILL.md Body (On Trigger)
- Pattern 1: Full guidance inline (high-freedom allows flexibility)
- Pattern 2: Core guidance + script integration (medium-freedom with scripts)
- Pattern 3: Domain selection + per-domain guidance (low-freedom with choices)

### Level 3: Resources (On-Demand)
- `scripts/`: Executed when AI Agent needs deterministic operations
- `examples/`: Referenced when AI Agent generates code
- `references/`: Loaded when AI Agent needs detailed patterns

---

## Anti-Patterns to Avoid

❌ **Mixing patterns**: Don't use Pattern 1 (no scripts) but include complex scripts; choose appropriate pattern

❌ **Deep nesting**: references/ should be flat; don't create references/patterns/aws/

❌ **Duplication**: Don't repeat content between SKILL.md and references/

❌ **Vague descriptions**: Make triggers explicit so AI Agent knows when to use skill

❌ **Over-engineered scripts**: Keep scripts simple and deterministic; avoid complex logic

❌ **No examples**: All patterns need examples (inline or in examples/)

---

## Pattern Evolution

As skills mature:

1. **Start with Pattern 1**: Simple guidance for clear use case
2. **Evolve to Pattern 2**: Add scripts for reliability as usage grows
3. **Grow to Pattern 3**: Split into multiple domains as scope expands

Migration example:
```
code-review-guide (Pattern 1)
  → add validation scripts
  → code-review-patterns (Pattern 2)
  → split by language
  → python-review, javascript-review, go-review (Pattern 1 each)
```
