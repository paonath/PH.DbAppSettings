# Naming Convention Guide

## Specification Filename Format

All specification files MUST follow this exact format:

```
spec-[type]-[description].md
```

### Components

**`spec-`** (prefix)
- Always lowercase
- Required on all specification files
- Helps identify spec files in directory listings

**`[type]`** (specification category)
- Exactly one type per specification
- Must be lowercase
- No hyphens within type name

Valid types:

| Type | Usage | Examples |
|------|-------|----------|
| `architecture` | System design, technical decisions, technology choices | jwt-auth-api, microservices-pattern, database-sharding |
| `design` | UI/UX design, visual design, interface design | user-dashboard, form-validation, navigation-menu |
| `process` | Workflows, procedures, team practices, processes | code-review, deployment-process, onboarding-workflow |
| `infrastructure` | DevOps, deployment, cloud resources, CI/CD pipelines | azure-cicd-pipeline, kubernetes-setup, docker-deployment |
| `data` | Database schemas, data models, migrations, data structures | user-schema, order-model, audit-migration |
| `schema` | API contracts, interfaces, data schemas | api-contracts, dto-definitions, event-schema |
| `tool` | Developer tools, build systems, testing utilities, automation | webpack-config, test-runner, linting-setup |
| `bugfix` | Bug fixes, issue resolutions, hotfixes | order-deadlock, memory-leak, cache-invalidation |

**`[description]`** (specification focus)
- Concise slug describing the specification
- Must be lowercase
- Hyphens separate words
- No underscores, spaces, or uppercase letters
- 2-50 characters recommended
- No special characters

### Length Constraints

- **Total filename**: Maximum 80 characters (including `.md` extension)
  - Reason: Filesystem compatibility, readable in most terminals
- **Description portion**: Maximum 50 characters
  - Reason: Readable at a glance, clear purpose

### Examples

**Good Examples** ✅
- `spec-architecture-jwt-auth-api.md` (37 chars)
  - Type: architecture | Description: jwt-auth-api
  
- `spec-infrastructure-azure-cicd-pipeline.md` (44 chars)
  - Type: infrastructure | Description: azure-cicd-pipeline
  
- `spec-design-user-dashboard.md` (29 chars)
  - Type: design | Description: user-dashboard
  
- `spec-bugfix-order-status-deadlock.md` (35 chars)
  - Type: bugfix | Description: order-status-deadlock
  
- `spec-data-audit-logging-schema.md` (33 chars)
  - Type: data | Description: audit-logging-schema

**Bad Examples** ❌
- `spec-JwtAuthApi.md` (Mixed case - violates lowercase rule)
- `spec_jwt_auth_api.md` (Underscores instead of hyphens)
- `jwt-auth-api.md` (Missing `spec-` prefix)
- `spec-jwt-auth-rest-api-implementation-guide.md` (Too long: 47 chars for description)
- `spec-jwt_auth-api.md` (Underscores mixed with hyphens)
- `spec-auth-jwt-api-token-based-authentication-with-refresh-tokens.md` (Too long: 77 chars for description)
- `Spec-JWT-Auth-API.md` (Mixed case, wrong prefix)

### Filename to File Sorting

When sorted alphabetically:
```
spec-architecture-audit-system.md
spec-architecture-jwt-auth-api.md
spec-bugfix-cache-invalidation.md
spec-data-user-schema.md
spec-design-login-form.md
spec-infrastructure-cicd-pipeline.md
spec-process-code-review.md
spec-schema-api-contracts.md
spec-tool-webpack-config.md
```

**Benefit**: Specifications are grouped by type, making it easy to scan `/specs/` directory

### Collision Avoidance

If your description would create a duplicate filename:

1. Add specificity:
   - `spec-architecture-api.md` → `spec-architecture-api-v2.md`
   - `spec-process-deployment.md` → `spec-process-deployment-manual.md`

2. Add sub-domain:
   - `spec-design-form.md` → `spec-design-form-validation.md`
   - `spec-data-user.md` → `spec-data-user-profile.md`

3. Combine terms more precisely:
   - `spec-process-testing.md` → `spec-process-unit-testing.md`

### Directory Organization

After creation, specs are stored in:

```
/specs/                           # Active specifications (drafts, under review)
  spec-architecture-*.md
  spec-design-*.md
  spec-process-*.md
  spec-infrastructure-*.md
  spec-data-*.md
  spec-schema-*.md
  spec-tool-*.md

/specs/implemented/               # Completed specifications
  /architecture/
    architecture-jwt-auth-api.md       # Note: "spec-" prefix removed
  /design/
    design-user-dashboard.md
  /process/
    process-code-review.md
  ...
```

**Important**: When moving from `/specs/` to `/specs/implemented/`, REMOVE the `spec-` prefix from the filename.

### Tools for Naming

To generate a filename from a purpose:

```bash
# From purpose: "Implement JWT authentication with refresh tokens"
# Output: "spec-architecture-jwt-auth-refresh-tokens.md"

python .agents/skills/spec-generator/scripts/init-spec.py \
  --purpose "Implement JWT authentication with refresh tokens" \
  --type architecture
```

The script automatically slugifies the description and validates the filename.

### Validation Rules

Before finalizing filename, verify:

- [ ] Starts with `spec-`
- [ ] One type from valid list
- [ ] Lowercase only (a-z, 0-9, hyphens)
- [ ] No underscores, spaces, or special characters
- [ ] Ends with `.md`
- [ ] Total length ≤ 80 characters
- [ ] Description length ≤ 50 characters
- [ ] No duplicate filenames in `/specs/`

**Automated validation**:
```bash
python .agents/skills/spec-generator/scripts/validate-spec.py spec-file.md
```

### Migration to Implemented

Only move to `/specs/implemented/` when specification is 100% implemented and verified:

1. Rename file (remove `spec-` prefix):
   - `spec-architecture-jwt-auth-api.md` → `architecture-jwt-auth-api.md`

2. Move to correct subdirectory:
   - `spec-architecture-*.md` → `specs/implemented/architecture/`
   - `spec-design-*.md` → `specs/implemented/design/`

3. Example:
   ```
   /specs/
     spec-architecture-jwt-auth-api.md  ← before
   
   /specs/implemented/
     /architecture/
       architecture-jwt-auth-api.md     ← after
   ```

---

**Reference**: See [create.specification.instructions.md](../../instructions/create.specification.instructions.md) for complete specification guidelines.
