# AI Agent Skill Creator Workflow Guide

Step-by-step walkthrough for creating and publishing a AI Agent Skill using proven patterns.

## Table of Contents
1. [Pre-Creation Planning](#pre-creation-planning)
2. [Skill Initialization](#skill-initialization)
3. [Content Development](#content-development)
4. [Validation & Testing](#validation--testing)
5. [Packaging & Distribution](#packaging--distribution)
6. [Maintenance & Updates](#maintenance--updates)

---

## Pre-Creation Planning

### Step 1: Define Skill Purpose

**Goal**: Write a clear, one-sentence skill definition.

**Questions to answer**:
1. What problem does this skill solve for AI Agent?
2. What specific domains/languages/tools does it cover?
3. Who are the primary users?
4. What are 3-5 specific use cases/triggers?

**Example**:
```
PROBLEM: AI Agent needs guidance on assigning Azure roles with least-privilege access
DOMAINS: Azure IAM, role management, security
USERS: DevOps engineers, security teams, developers
TRIGGERS: "Which Azure role...", "How to assign permissions", "Least privilege access"
```

### Step 2: Choose Complexity Pattern

**Decision**: Which pattern fits your skill?

- **Pattern 1 (High-freedom)**: Guidance-only, no scripts, multiple valid approaches
  - Example: Code review guidance, architecture decisions
  
- **Pattern 2 (Medium-freedom)**: Guidance + scripts, some variation acceptable
  - Example: JSON validation, format conversion
  
- **Pattern 3 (Low-freedom)**: Multi-domain with incompatible approaches per domain
  - Example: Cloud deployment (AWS/GCP/Azure), language-specific tools

See [design-patterns.md](./design-patterns.md) for decision tree.

### Step 3: Gather Content Requirements

**For Pattern 1** (Guidance):
- Core principles/guidance
- Decision trees for choosing approaches
- 3-5 inline examples

**For Pattern 2** (Scripts):
- Core guidance (overview + step-by-step)
- 2-5 scripts (validation, conversion, processing)
- Example inputs/outputs
- Simple examples inline, complex in examples/

**For Pattern 3** (Multi-domain):
- Domain selection guidance
- Per-domain principles and patterns
- Per-domain scripts (validation, code generation)
- Per-domain examples
- Domain comparison reference

### Step 4: Name Your Skill

**Requirements**:
- Lowercase letters, numbers, hyphens only
- Max 64 characters
- Clear, descriptive name matching primary domain
- No spaces, underscores, or capitals

**Examples**:
- ✓ `azure-role-selector`, `json-validator`, `typescript-patterns`
- ✗ `AzureRoleSelector`, `json_validator`, `ts-patterns-and-examples`

---

## Skill Initialization

### Step 5: Create Directory Structure

```bash
# Create skill directory under .agents/skills/
mkdir -p .agents/skills/skill-name/{scripts,examples,references}

# Create SKILL.md with minimal skeleton
touch .agents/skills/skill-name/SKILL.md
```

### Step 6: Write SKILL.md Skeleton

**Frontmatter** (exact format):
```yaml
---
name: skill-name
description: |
  [One-sentence purpose]. Use when: (1) [trigger 1], (2) [trigger 2], (3) [trigger 3]
---
```

**Requirements**:
- Exact name (lowercase, hyphens only)
- Description includes use-case triggers
- Description max 1024 characters

**Example**:
```yaml
---
name: json-validator
description: |
  Validate and format JSON files with AI Agent using deterministic validation scripts.
  Use when: (1) validating JSON syntax, (2) fixing formatting issues, (3) converting JSON formats
---
```

### Step 7: Create Body Skeleton

Based on your pattern, create appropriate body sections:

**Pattern 1** (Guidance):
```markdown
# [Skill Name]

## Overview
[What it does, when to use]

## Principle 1: [Name]
[Guidance + examples]

## Decision Tree
[Help AI Agent choose]

## Examples
[Inline code examples]
```

**Pattern 2** (Scripts):
```markdown
# [Skill Name]

## Overview
[What it does, when to use]

## Step 1: [Operation]

AI Agent executes:
\`\`\`bash
python scripts/operation.py input.file
\`\`\`

[Explanation]

## Step 2: [Next Operation]
[Similar structure]

## Troubleshooting
[Common issues]
```

**Pattern 3** (Multi-domain):
```markdown
# [Skill Name]

## Domain Selection

[Help AI Agent choose domain]

## Domain 1: [Name]

[Domain-specific guidance + scripts]

## Domain 2: [Name]

[Domain-specific guidance + scripts]

## Comparison Table

[Show differences between domains]
```

---

## Content Development

### Step 8: Write Core Content

**For each section**:

1. **Identify audience**: What does AI Agent need to know?
2. **Use imperative form**: "Use...", "Generate...", "Run..."
3. **Provide examples**: Show input → process → output
4. **Include decision trees**: Help AI Agent choose
5. **Reference external files**: For complex/advanced content

**Writing guidelines**:
- Keep sentences clear and concise
- Assume AI Agent is intelligent; add only needed context
- Show practical examples, not theory
- Challenge every sentence: "Does AI Agent need this?"

### Step 9: Create Scripts (if Pattern 2 or 3)

**Requirements**:
- Deterministic (same input → same output)
- JSON output for AI Agent parsing
- Error handling with clear messages
- Timeout <30 seconds

**Template**:
```python
#!/usr/bin/env python3
"""Concise script description for AI Agent."""

import json
import sys

def main():
    if len(sys.argv) < 2:
        print(json.dumps({"error": "Usage: python script.py <input>"}))
        sys.exit(1)
    
    try:
        result = process(sys.argv[1])
        print(json.dumps(result, indent=2))
        sys.exit(0)
    except Exception as e:
        print(json.dumps({"error": str(e)}))
        sys.exit(1)

def process(input_file):
    """Main processing logic."""
    # Your implementation here
    return {"status": "success", "data": {}}

if __name__ == "__main__":
    main()
```

**Steps**:
1. Create script in `scripts/` directory
2. Make executable: `chmod +x scripts/script_name.py`
3. Test with sample input: `python scripts/script_name.py test_input.json`
4. Verify JSON output: `python scripts/script_name.py test_input.json | python -m json.tool`

### Step 10: Create Examples

**Placement**:
- Simple examples: Inline in SKILL.md (code blocks)
- Complex examples: In `examples/` directory with clear naming

**For each example**:
- Show realistic scenario
- Provide complete input
- Show expected output
- Add comments explaining key decisions

**Examples directory structure**:
```
examples/
├── basic-example.json
├── advanced-pattern.yaml
├── template-output.html
└── ...
```

### Step 11: Create Reference Files (if needed)

**When to create references**:
- SKILL.md exceeds 500 lines
- Advanced patterns not needed for basic use
- Separate concerns (e.g., troubleshooting vs. patterns)
- Domain-specific guidance (Pattern 3)

**Reference file structure**:
```
references/
├── advanced-patterns.md (detailed patterns)
├── troubleshooting.md (common issues)
├── domain-specific.md (for one domain)
└── ...
```

**Links from SKILL.md**:
```markdown
See [advanced-patterns.md](./references/advanced-patterns.md) for complex scenarios.
```

---

## Validation & Testing

### Step 12: Run Validation Script

```bash
python scripts/validate_skill.py .agents/skills/skill-name/
```

**Expected success output**:
```json
{
  "status": "pass",
  "checks": {
    "skill_md_exists": true,
    "frontmatter_format": true,
    ...
  },
  "errors": [],
  "warnings": []
}
```

**Fix any errors** before proceeding.

### Step 13: Test Scripts (if applicable)

For each script in `scripts/`:

```bash
# Test with sample input
python scripts/validate_skill.py test_input.json

# Verify JSON output
python scripts/validate_skill.py test_input.json | python -m json.tool

# Check exit codes
python scripts/validate_skill.py test_input.json && echo "Success" || echo "Failed"
```

**Requirements**:
- ✓ Exits with code 0 on success
- ✓ Exits with code 1 on failure
- ✓ Outputs valid JSON
- ✓ Completes in <30 seconds
- ✓ Handles edge cases gracefully

### Step 14: Manual Testing

**Test with realistic scenarios**:

1. **Scenario 1**: User's most common use case
   - Input: Real data
   - Expected: Expected outcome
   - Verify: Matches expectations

2. **Scenario 2**: Boundary/edge case
   - Input: Edge case data (empty, very large, special characters)
   - Expected: Graceful handling
   - Verify: No crashes or unclear errors

3. **Scenario 3**: Error case
   - Input: Invalid data
   - Expected: Clear error message
   - Verify: AI Agent understands issue

### Step 15: Test in VS Code

If possible, test skill in AI Agent VS Code:

1. Copy skill to `.agents/skills/skill-name/`
2. Trigger skill in VS Code (mention skill name/triggers)
3. Verify: SKILL.md body loads
4. Verify: Scripts execute correctly
5. Verify: Examples display properly

---

## Packaging & Distribution

### Step 16: Package Skill

```bash
python scripts/package_skill.py .agents/skills/skill-name/
```

**Output**:
```json
{
  "status": "success",
  "skill_name": "skill-name",
  "output_file": "./dist/skill-name.skill",
  "archive_size": "1.2 MB",
  "file_count": 15,
  "next_steps": [
    "Copy to project: .agents/skills/skill-name/",
    "Or: ~/.agents/skills/skill-name/",
    "Or: Submit to https://github.com/agent-registries"
  ]
}
```

### Step 17: Choose Distribution Channel

**Option 1: Project Skills (.agents/skills/)**
```
your-repo/
└── .agents/skills/
    └── skill-name/
        ├── SKILL.md
        ├── scripts/
        ├── examples/
        └── references/
```
- AI Agent auto-discovers in VS Code, CLI, coding agent
- Shared with team members
- Not publicly available

**Option 2: Personal Skills (~/.agents/skills/)**
```
~/.agents/skills/
└── skill-name/
    ├── SKILL.md
    ├── scripts/
    └── references/
```
- Available only to you
- Used across all repositories
- Not shared with team

**Option 3: Community Repository (awesome-agent)**

1. Fork [agent-registries](https://github.com/agent-registries)
2. Create `skills/skill-name/SKILL.md` with your skill
3. Submit pull request
4. Community reviews and accepts
5. Skill available to all GitHub users via awesome-agent

**Recommended**: Start with Option 1 (project), then Option 3 (community).

### Step 18: Verify Distribution

**After deploying**:

```bash
# Verify skill location
ls -la .agents/skills/skill-name/

# Test skill loading in AI Agent
# (mention skill name in VS Code to trigger)

# Verify script execution
python scripts/validate_skill.py .agents/skills/skill-name/
```

---

## Maintenance & Updates

### Step 19: Gather Feedback

**After deployment**:
- Monitor AI Agent usage
- Collect feedback from users
- Track common issues in troubleshooting
- Identify gaps in documentation

### Step 20: Version Updates

When updating skill:

1. **Update SKILL.md** with improvements
2. **Update/add scripts** as needed
3. **Add examples** of new features
4. **Update references** with new patterns
5. **Re-validate**: `python scripts/validate_skill.py ...`
6. **Re-package**: `python scripts/package_skill.py ...`
7. **Re-deploy** to `.agents/skills/` or awesome-agent

### Step 21: Deprecation (if needed)

If replacing with new skill:

1. Update SKILL.md description with deprecation notice
2. Add link to replacement skill
3. Keep scripts working for existing users
4. Remove from awesome-agent (if published)

---

## Troubleshooting During Creation

### Issue: "Validation failed: Broken links"

**Cause**: Referenced files don't exist in skill directory

**Solution**:
1. Check file names match exactly
2. Use relative paths: `./references/file.md` not `references/file.md`
3. Create missing files or update references

### Issue: "SKILL.md exceeds 500 lines"

**Cause**: Body too large for efficient AI Agent loading

**Solution**:
1. Move advanced patterns to `references/` directory
2. Keep SKILL.md focused on core guidance
3. Link to reference files: `See [file.md](./references/file.md)`

### Issue: "Script fails or times out"

**Cause**: Script logic error or inefficiency

**Solution**:
1. Test locally: `python scripts/script.py test_input`
2. Check for infinite loops or large data processing
3. Add error handling for edge cases
4. Optimize performance (should complete <30s)

### Issue: "Description doesn't help AI Agent discover skill"

**Cause**: Vague or missing use-case triggers

**Solution**:
1. Add explicit "Use when:" section with 3+ triggers
2. Include keywords users search for
3. Include specific domains/tools
4. Keep under 1024 characters

---

## Quick Reference Checklist

### Before Validation ✓
- [ ] Skill directory created: `.agents/skills/skill-name/`
- [ ] SKILL.md exists with valid frontmatter
- [ ] name field: lowercase, hyphens only, <64 chars
- [ ] description field: includes triggers, <1024 chars
- [ ] Body uses imperative form: "Use", "Generate", "Run"
- [ ] All file references use relative paths

### After Validation ✓
- [ ] Validation script: Status = "pass"
- [ ] All errors fixed (if any)
- [ ] Warnings reviewed and addressed
- [ ] Scripts tested and working
- [ ] Examples provided and tested

### Before Packaging ✓
- [ ] Manual testing completed
- [ ] Edge cases handled gracefully
- [ ] Error messages clear
- [ ] Performance acceptable (<30s)
- [ ] README not included (only SKILL.md + resources)

### Before Distribution ✓
- [ ] Packaging successful (.skill file created)
- [ ] Archive integrity verified
- [ ] Distribution channel chosen
- [ ] Team notified (if project skill)
- [ ] Documentation updated (if awesome-agent)

---

## Example Walkthrough: JSON Validator

### Pre-Creation
```
PURPOSE: Validate and format JSON files with AI Agent
PATTERN: Pattern 2 (Scripts + guidance)
TRIGGERS: "Validate JSON", "Fix JSON formatting", "Check JSON schema"
NAME: json-validator
```

### Initialization
```bash
mkdir -p .agents/skills/json-validator/{scripts,examples,references}
touch .agents/skills/json-validator/SKILL.md
```

### Content Development
1. Write SKILL.md with overview + step-by-step usage of scripts
2. Create `scripts/validate_json.py` (checks syntax)
3. Create `scripts/format_json.py` (beautifies)
4. Create `scripts/validate_schema.py` (schema validation)
5. Create example JSON files in `examples/`
6. Create `references/json-schema-examples.md`

### Validation
```bash
python scripts/validate_skill.py .agents/skills/json-validator/
# Output: Status "pass" ✓
```

### Testing
```bash
python scripts/validate_json.py examples/config.json
# Output: Valid JSON with structure summary ✓

python scripts/format_json.py examples/minified.json formatted.json
# Output: Pretty-printed JSON ✓
```

### Packaging
```bash
python scripts/package_skill.py .agents/skills/json-validator/
# Output: json-validator.skill created ✓
```

### Distribution
```bash
# Copy to project
cp -r .agents/skills/json-validator/* .agents/skills/json-validator/

# Test in VS Code
# (AI Agent discovers and loads skill) ✓
```

---

**Ready to create your AI Agent Skill?**

1. Follow this workflow from beginning to end
2. Refer to [design-patterns.md](./design-patterns.md) for pattern details
3. Refer to [validation-rules.md](./validation-rules.md) for compliance requirements
4. Check examples in `examples/` for inspiration
5. Use [GitHub Awesome AI Agent](https://github.com/agent-registries) for community reference
