# Spec Generator Skill - Quick Start Guide

## Overview

The `spec-generator` skill is a comprehensive Skill designed to help AI coding agents (such as Antigravity, Claude, and AI Agent) create AI-ready, production-grade specification files following the project's established template and best practices.

**Key Benefits**:
- 🎯 Ensures specification compliance with template standards
- 🔍 Detects conflicts with existing specifications
- ✅ Validates AI-readiness automatically
- 📋 Generates atomic task breakdowns for implementation
- 🔒 Enforces security and best practice guidelines

## Quick Start

### 1. Create a New Specification

Use the `/spec-add` command with your specification purpose:

```bash
/spec-add "Implement JWT authentication with refresh tokens and rate limiting"
```

Or programmatically:

```bash
python .agents/skills/spec-generator/scripts/init-spec.py \
  --purpose "Your specification purpose here" \
  --type architecture \
  --file spec-architecture-my-spec.md
```

### 2. Check for Conflicts

Before creating, verify no duplicates or conflicts exist:

```bash
python .agents/skills/spec-generator/scripts/check-conflicts.py \
  "Your specification purpose" \
  --type architecture
```

**Output**: JSON showing conflicts, related specs, and recommendations

### 3. Validate Your Specification

After completing your spec, validate it:

```bash
python .agents/skills/spec-generator/scripts/validate-spec.py \
  specs/spec-architecture-my-spec.md
```

**Output**: Validation report with errors, warnings, and AI-readiness checklist

## Specification Types

Choose the right type for your specification:

| Type | Usage | Example |
|------|-------|---------|
| `architecture` | System design, technical decisions | `spec-architecture-jwt-auth-api.md` |
| `design` | UI/UX design, components | `spec-design-user-dashboard.md` |
| `process` | Workflows, procedures | `spec-process-code-review.md` |
| `infrastructure` | DevOps, deployment, CI/CD | `spec-infrastructure-azure-cicd.md` |
| `data` | Database schemas, data models | `spec-data-user-schema.md` |
| `schema` | API contracts, interfaces | `spec-schema-api-contracts.md` |
| `tool` | Developer tools, build systems | `spec-tool-webpack-config.md` |
| `bugfix` | Bug fixes, hotfixes | `spec-bugfix-cache-deadlock.md` |

## File Structure

```
.agents/skills/spec-generator/
├── SKILL.md                                  # Main skill documentation
├── scripts/
│   ├── init-spec.py                         # Create spec from template
│   ├── check-conflicts.py                   # Detect conflicts
│   ├── validate-spec.py                     # Validate specification
│   └── README.md                            # Script documentation
├── examples/
│   ├── spec-architecture-jwt-auth-example.md    # Full example
│   ├── spec-bugfix-order-status-deadlock.md     # Bugfix example
│   └── spec-design-user-dashboard.md            # Design example
├── references/
│   ├── naming-convention.md                 # Filename rules
│   ├── validation-rules.md                  # Validation checklist
│   └── conflict-resolution.md               # Conflict handling guide
└── README.md                                # This file
```

## Workflow: Step-by-Step

### Step 1: Plan Your Specification

Define what you want to specify:
- What feature/system is this about?
- Why is it needed?
- What's in scope? What's out?
- What constraints exist?

**Example SpecPurpose**:
```
"Implement JWT authentication with refresh tokens, rate limiting (5 attempts/15min), 
and audit logging for secure user authentication across mobile and web clients"
```

### Step 2: Initialize Specification

Create skeleton from template:

```bash
cd /specs
python ../.agents/skills/spec-generator/scripts/init-spec.py \
  --purpose "Your specification purpose" \
  --type architecture
```

This creates:
- File with correct naming: `spec-architecture-[slug].md`
- Frontmatter with metadata (title, version, date, owner)
- 15 template sections with placeholders
- Comments explaining each section

### Step 3: Fill in Content

Work through each section:

1. **Purpose & Scope** - Define what's included/excluded
2. **Definitions** - Define all acronyms and terms
3. **Requirements** - List MUST/SHALL/SHOULD/MAY requirements
4. **Architecture** - Describe system design and API contracts
5. **Dependencies** - Document tech stack and integrations
6. **Acceptance Criteria** - Define testable success criteria
7. **Test Strategy** - Plan testing approach
8. **Examples** - Provide code/config examples
9. **Validation** - Self-check checklist
10. **AI-Readiness** - Complete 10-item checklist
11. **References** - Link related specs and docs
12. **Tasks** - Break down into atomic implementation tasks
13. **Conflicts** - Document any conflicts found
14. **Context** - List files referenced
15. **Instructions** - Reference project-wide rules

### Step 4: Validate & Review

Run validation script:

```bash
python .agents/skills/spec-generator/scripts/validate-spec.py specs/spec-architecture-jwt-auth.md
```

**Must pass**:
- ✅ All 15 sections present
- ✅ Frontmatter valid YAML
- ✅ Filename follows naming convention
- ✅ ≥8 of 10 AI-readiness checks pass
- ✅ Requirements use RFC 2119 keywords
- ✅ Tasks are atomic and complete
- ✅ No conflicts with existing specs

### Step 5: Submit for Review

Move spec to `/specs/` (if not already there) with status: `draft`

Request review from:
- Architecture team (for architecture specs)
- Product team (for feature specs)
- DevOps team (for infrastructure specs)
- etc.

### Step 6: Implement

Once approved (status: `approved`), implementation begins using the spec as blueprint.

**Important**: Specification is read-only at this point. Implementation creates the code.

## Using the Examples

Three complete examples are included:

### Example 1: Full Architecture Spec
[spec-architecture-jwt-auth-example.md](examples/spec-architecture-jwt-auth-example.md)

**Covers**: 
- Complete API authentication system
- JWT tokens with refresh mechanism
- Rate limiting and security
- 10 atomic implementation tasks
- Full acceptance criteria

**Use this**: To understand structure and detail level expected for complex features

### Example 2: Bugfix Specification
[spec-bugfix-order-status-deadlock.md](examples/spec-bugfix-order-status-deadlock.md)

**Covers**:
- Problem analysis
- Root cause documentation
- SQL procedure fix
- Concurrency testing

**Use this**: When creating bugfix or hotfix specifications

### Example 3: Design Specification
[spec-design-user-dashboard.md](examples/spec-design-user-dashboard.md)

**Covers**:
- Component structure
- TypeScript types
- Responsive design requirements
- Accessibility requirements

**Use this**: When specifying frontend components or UI/UX

## References

### Main Resources

- [SKILL.md](SKILL.md) - Complete skill documentation
- [naming-convention.md](references/naming-convention.md) - Filename rules and examples
- [validation-rules.md](references/validation-rules.md) - Validation checklist
- [conflict-resolution.md](references/conflict-resolution.md) - Handling conflicts

### Project Instructions

- [create.specification.instructions.md](../../instructions/create.specification.instructions.md) - Master template
- [dotnet.minimalapi.instructions.md](../../instructions/dotnet.minimalapi.instructions.md) - REST API conventions
- [dotnet.csharp.instructions.md](../../instructions/dotnet.csharp.instructions.md) - C# coding standards
- [xunit.dotnet.instructions.md](../../instructions/xunit.dotnet.instructions.md) - Testing standards

### External References

- [RFC 2119 - Requirement Keywords](https://tools.ietf.org/html/rfc2119)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [Specification by Example](https://en.wikipedia.org/wiki/Specification_by_example)

## Common Tasks

### Create a New Feature Specification

```bash
cd /specs
python ../.agents/skills/spec-generator/scripts/init-spec.py \
  --purpose "Implement user profile editing with photo upload and validation" \
  --type architecture
```

Fill in sections. Run validation. Submit for review.

### Find Related Specifications

```bash
python .agents/skills/spec-generator/scripts/check-conflicts.py \
  "Your search purpose"
```

This finds existing specs with similar scope or overlapping requirements.

### Validate All Specs in Directory

```bash
for file in specs/spec-*.md; do
  echo "Validating $file..."
  python .agents/skills/spec-generator/scripts/validate-spec.py "$file"
done
```

### Move Spec to Implemented

Only do this when implementation is 100% complete and verified:

```bash
# Remove spec- prefix and move to implemented directory
mv specs/spec-architecture-jwt-auth.md specs/implemented/architecture/architecture-jwt-auth.md
```

## Troubleshooting

### Validation Fails: "Missing section"

**Issue**: Validator reports "Missing section: ## 2. Definitions"

**Solution**: Add missing section to spec file. Use template as reference.

### Validation Fails: "Filename too long"

**Issue**: "Filename length 95 exceeds recommended 80 characters"

**Solution**: Shorten description slug:
- ❌ `spec-architecture-jwt-authentication-with-refresh-tokens.md` (81 chars)
- ✅ `spec-architecture-jwt-auth-refresh.md` (38 chars)

### Conflict Detection Shows False Positive

**Issue**: Spec marked as conflicting but it's actually different

**Solution**: 
1. Review conflict in spec section 13
2. Update `related_specs` if they complement each other
3. Document relationship clearly in conflict analysis

### Can't Find Existing Spec

**Issue**: Your spec might be duplicate but validator doesn't find it

**Solution**:
1. Check `/specs/` directory manually
2. Check `/specs/implemented/` directory
3. Search for keywords: `grep -r "keyword" specs/`

## Advanced Usage

### Integration with CI/CD

Add to pipeline to validate specs on commit:

```yaml
# .github/workflows/validate-specs.yml
name: Validate Specifications

on: [push]

jobs:
  validate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Validate specs
        run: |
          for file in specs/spec-*.md; do
            python .agents/skills/spec-generator/scripts/validate-spec.py "$file" || exit 1
          done
```

### Bulk Operations

Generate multiple specs from CSV:

```bash
# specs.csv
purpose,type
"Implement user authentication",architecture
"Design login form",design
"Setup CI/CD pipeline",infrastructure

# Process each
while IFS=',' read -r purpose type; do
  python init-spec.py --purpose "$purpose" --type "$type"
done < specs.csv
```

## Best Practices

1. **Start with Purpose**: Clear SpecPurpose prevents scope creep
2. **Fill Sections Sequentially**: Definitions → Requirements → Tasks
3. **Use Examples**: Reference similar specs in examples/
4. **Validate Early**: Run validator frequently to catch issues
5. **Ask for Clarity**: If requirements ambiguous, get clarification before writing
6. **Document Conflicts**: Always record in section 13
7. **Link Related**: Use frontmatter to link complementary specs
8. **Keep AI-Ready**: Aim for all 10 AI-readiness checks passed

## Support

For questions or issues:

1. Check [SKILL.md](SKILL.md) for detailed documentation
2. Review examples/ for similar specifications
3. Check references/ for specific guidelines
4. Consult project instructions in `.agents/rules/`

---

**Ready to create your first specification?**

1. Define your SpecPurpose
2. Choose the spec type (architecture/design/process/etc)
3. Run `init-spec.py` to generate skeleton
4. Fill in sections following template
5. Run `validate-spec.py` to check compliance
6. Submit for review

Good luck! 🚀
