---
name: skill-creator
description: |
  Create, validate, package, and distribute AI Agent Skills for Antigravity, Claude, AI Agent, and other agent frameworks.
  Use when: (1) designing new agent skills, (2) validating skill structure,
  (3) organizing skill resources, (4) packaging and distributing skills,
  (5) sharing skills via repositories, (6) ensuring Agent Skills standard compliance
---

# AI Agent Skill Creator

Create professional, production-ready Agent Skills that work seamlessly across AI coding assistants such as Antigravity, Claude, and AI Agent. This skill guides you through a structured 6-step process.

## Quick Start: The 6-Step Process

1. **Understand** - Clarify what your skill does and when the AI agent should use it
2. **Plan resources** - Decide on SKILL.md body, bundled resources, and progressive disclosure
3. **Initialize** - Create skill directory and SKILL.md skeleton with proper frontmatter
4. **Develop** - Write SKILL.md body, add scripts/examples, organize references
5. **Validate** - Check compliance with Agent Skills standard
6. **Package & distribute** - Create .skill file and share via `.agents/skills/`, `.agents/skills/`, or awesome-agent

---

## Step 1: Define Your Skill Purpose

Clarify what your skill does and when the AI agent should use it:

- **One-sentence purpose**: "This skill helps the agent [action] by [what it provides]"
- **Identify triggers**: User requests that should activate your skill
- **Assess scope**: Single domain (narrow) or multiple domains (broad)

Example skill definitions:
- "Assign least-privilege Azure roles" → `azure-role-selector`
- "Validate and format JSON" → `json-validator`
- "Deploy to AWS/GCP/Azure" → `cloud-deployment`

---

## Step 2: Choose Your Skill Pattern

Agent Skills follow three organizational patterns:

### Pattern 1: High-Freedom Guidance (no scripts)
- Multiple valid approaches; context-dependent decisions
- Examples: Code review guidance, architecture decisions, best practices
- Structure: SKILL.md only (all guidance inline)

### Pattern 2: Medium-Freedom Scripts (guidance + validation)
- Preferred patterns exist; deterministic operations needed
- Examples: JSON validation, format conversion, file processing
- Structure: SKILL.md + scripts/ + examples/

### Pattern 3: Low-Freedom Multi-Domain (per-domain patterns)
- Different frameworks/providers with incompatible approaches
- Examples: Cloud deployment (AWS/GCP/Azure), language-specific tools
- Structure: SKILL.md + scripts/ + examples/ + references/ (domain guides)

**Decision**: Choose based on whether the agent needs:
- Multiple valid approaches? → Pattern 1
- Deterministic scripts? → Pattern 2 or 3
- Multi-domain selection? → Pattern 3

---

## Step 3: Create Skill Directory & SKILL.md

Create directory structure:
```bash
mkdir -p .agents/skills/skill-name/{scripts,examples,references}
```

Create SKILL.md with required frontmatter:
```yaml
---
name: skill-name
description: |
  [One-sentence purpose]. Use when: (1) [trigger 1], (2) [trigger 2], (3) [trigger 3]
---
```

**Requirements**:
- name: lowercase, hyphens only, max 64 characters
- description: clear triggers + use cases, max 1024 characters
- Body: under 500 lines, uses imperative form ("Use", "Generate", "Run")

---

## Step 4: Write SKILL.md Body

Structure your SKILL.md body based on your pattern:

**Pattern 1 (Guidance)**:
```
## Overview
## Principle 1: [Name]
## Principle 2: [Name]
## Decision Tree
## Examples
```

**Pattern 2 (Scripts)**:
```
## Overview
## Step 1: [Operation] (AI agent executes: python scripts/...)
## Step 2: [Next Operation]
## Examples
## Troubleshooting
```

**Pattern 3 (Multi-Domain)**:
```
## Domain Selection
## Domain 1: [Name] (guidance + scripts)
## Domain 2: [Name] (guidance + scripts)
## Comparison Table
## Troubleshooting
```

### Content Guidelines

- Use imperative form: "Use...", "Generate...", "Run...", not "This is..."
- Include inline examples (code blocks) for common cases
- Reference advanced topics to external files in references/
- Provide decision trees to help the AI agent choose
- Keep SKILL.md focused on core guidance

---

## Step 5: Create Scripts (if needed)

Scripts are executed by the AI agent for deterministic operations:

```python
#!/usr/bin/env python3
"""Script description for the AI Agent."""

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
    return {"status": "success", "data": {}}

if __name__ == "__main__":
    main()
```

**Requirements**:
- Deterministic: Same input → same output
- Error handling: All exceptions caught
- JSON output: For AI agent parsing
- Timeout: Complete within 30 seconds
- Test locally: `python scripts/name.py test_input`

---

## Step 6: Validate Your Skill

Run validation before packaging:

```bash
python scripts/validate_skill.py .agents/skills/skill-name/
```

Expected output:
```json
{
  "status": "pass",
  "errors": [],
  "warnings": []
}
```

**Validation checks**:
- ✓ SKILL.md exists
- ✓ Frontmatter: valid YAML with name + description only
- ✓ Name: lowercase, hyphens, max 64 chars
- ✓ Description: max 1024 chars, includes triggers
- ✓ Body: uses imperative form, under 500 lines
- ✓ Files: all referenced files exist
- ✓ Scripts: tested and working
- ✓ No credentials: no API keys/secrets embedded

---

## Step 7: Package Your Skill

Create distributable .skill file (zip archive):

```bash
python scripts/package_skill.py .agents/skills/skill-name/
```

Output:
```
skill-name.skill created (zip archive with preserved structure)
Ready to deploy to: .agents/skills/ or ~/.agents/skills/
```

---

## Step 8: Distribute Your Skill

**Option 1: Project Skills** (.agents/skills/)
```
your-repo/.agents/skills/skill-name/
```
AI Assistants (Antigravity, Claude, AI Agent) auto-discover skills from .agents/skills/ or project settings.

**Option 2: Personal Skills** (~/.agents/skills/)
```
~/.agents/skills/skill-name/
```
Available to you across all repositories.

**Option 3: Community Sharing** (e.g. GitHub Awesome AI Agent or other AI registries)
- Fork or upload to relevant community registries (e.g., [agent-registries](https://github.com/agent-registries) for AI Agent)
- Add skill to `skills/` directory
- Submit PR for community review

---

## Skill Directory Structure

**Flat structure required** (no nested directories):
```
skill-name/
├── SKILL.md (required)
├── scripts/ (flat: no subdirs)
│   ├── validate.py
│   └── format.py
├── examples/ (flat: no subdirs)
│   ├── basic-example.json
│   └── advanced-pattern.yaml
└── references/ (flat: no subdirs)
    ├── advanced-patterns.md
    ├── troubleshooting.md
    └── comparison.md
```

### File Purposes

- **SKILL.md**: Guidance for the AI Agent (all required content)
- **scripts/**: Executable code (Python/Bash/JS) for deterministic operations
- **examples/**: Code samples and templates for output generation
- **references/**: Detailed guides for advanced topics (linked from SKILL.md)

### Progressive Disclosure

AI Agents load your skill in 3 levels:

1. **Metadata** (always): name + description → The agent decides to load skill based on trigger matching
2. **Body** (on trigger): SKILL.md body → The agent uses the guidance to generate responses
3. **Resources** (on-demand): scripts/examples/references → The agent uses these as needed

---

## Common Issues & Solutions

**Q: My SKILL.md exceeds 500 lines**
A: Split detailed content into references/ directory. SKILL.md should focus on core guidance.

**Q: Link validation fails**
A: Use relative paths `./references/file.md`, ensure files exist, no leading slashes.

**Q: Script times out or fails**
A: Test locally: `python scripts/script.py test_input`. Add error handling, optimize performance.

**Q: Frontmatter validation fails**
A: Check YAML syntax, ensure only `name` and `description` fields (no extras).

**Q: Description doesn't help the agent discover my skill?**
A: Add explicit "Use when:" with 3+ specific triggers/keywords.

---

## Next Steps

1. **Choose pattern** (Pattern 1/2/3) based on your skill type
2. **Create directory** and SKILL.md skeleton
3. **Develop content** following pattern guidelines
4. **Write scripts** (if needed) with error handling
5. **Create examples** (inline or in examples/)
6. **Validate**: `python scripts/validate_skill.py ...`
7. **Package**: `python scripts/package_skill.py ...`
8. **Deploy**: Copy to `.agents/skills/` (or `.agents/skills/` depending on the platform) or publish to your agent registry

---

## Documentation

Detailed documentation in references/:
- **validation-rules.md**: Complete validation requirements
- **design-patterns.md**: Pattern organization strategies
- **workflow-guide.md**: Step-by-step creation walkthrough

Example skills in examples/:
- **minimal-skill.md**: Pattern 1 (High-freedom guidance)
- **medium-skill.md**: Pattern 2 (Scripts + guidance)
- **complex-skill.md**: Pattern 3 (Multi-domain)

---

## Official Resources

- [VS Code Agent Skills Documentation](https://code.visualstudio.com/docs/copilot/customization/agent-skills)
- [Agent Skills Standard (agentskills.io)](https://agentskills.io/)
- [GitHub Awesome Copilot](https://github.com/github/awesome-copilot)

---

**Ready to create your AI Agent Skill?**

Follow the 8 steps above, refer to references/ for detailed guidance, and check examples/ for inspiration.

Created: January 13, 2026 | Status: Ready for use across AI frameworks (Antigravity, Claude, AI Agent)
