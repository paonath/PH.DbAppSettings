# Conflict Resolution Guide

When creating specifications, conflicts and overlaps may be discovered with existing specs. This guide explains how to identify, document, and resolve conflicts.

## Types of Conflicts

### 1. Exact Duplicate (Conflict Level: CRITICAL)

**Symptom**: Another spec exists with identical or nearly identical scope

**Example**:
- Existing: `spec-architecture-jwt-auth.md`
- New: `spec-architecture-jwt-authentication-api.md`
- Problem: Same feature described twice

**Resolution**:
- ❌ Do NOT create duplicate
- ✅ Use existing spec instead
- ✅ If improvement needed, create update to existing spec
- ✅ Increment `version` in frontmatter (1.0.0 → 1.1.0)

**Section 13 Entry**:
```
| CNF-001 | spec-architecture-jwt-auth.md | Exact duplicate scope | Use existing spec, do not create |
```

### 2. Overlapping Scope (Conflict Level: HIGH)

**Symptom**: New spec overlaps with existing spec but has different focus/approach

**Example**:
- Existing: `spec-schema-api-contracts.md` (defines all API contracts)
- New: `spec-architecture-authentication-api.md` (includes auth API contract)
- Problem: Authentication API defined in both places

**Resolution**:
- ✅ Create new spec if it adds significant value
- ✅ Add to `related_specs` in frontmatter of both specs
- ✅ In section 13, document relationship and how specs complement each other
- ✅ Ensure no contradictory requirements

**Update existing spec**:
```yaml
related_specs:
  - spec-architecture-authentication-api.md
```

**Section 13 Entry in NEW spec**:
```
| CNF-001 | spec-schema-api-contracts.md | Overlapping scope: both define API contracts | Document relationship - auth API will be detailed in this spec, general patterns in api-contracts.md |
```

### 3. Contradictory Requirements (Conflict Level: CRITICAL)

**Symptom**: New spec has requirements that contradict existing spec

**Example**:
- Existing: `spec-architecture-jwt-auth.md` says "JWT tokens expire after 1 hour"
- New: `spec-architecture-token-refresh.md` says "JWT tokens expire after 30 minutes"
- Problem: Contradictory expiry time

**Resolution**:
- ✅ Resolve conflict before creating spec
- ✅ Get stakeholder approval on updated approach
- ✅ In new spec, use `supersedes` field to replace old spec
- ✅ Clearly document rationale for change in section 13
- ✅ Plan deprecation of old spec

**New spec frontmatter**:
```yaml
supersedes:
  - spec-architecture-jwt-auth.md
```

**Section 13 Entry**:
```
| CNF-001 | spec-architecture-jwt-auth.md | Contradictory token expiry (was 1h, now 30min) | This spec supersedes old spec. Change approved by security team on 2025-01-15. Rationale: shorter expiry improves security for active user sessions. |
```

### 4. Technology Stack Conflict (Conflict Level: HIGH)

**Symptom**: New spec uses different technology than existing architecture specs

**Example**:
- Existing architecture standard: "Use .NET 8"
- New spec proposes: "Use Node.js 20"
- Problem: Violates established tech stack

**Resolution**:
- ⚠️ Request clarification from tech lead
- ✅ If justified, document rationale clearly
- ✅ Consider: Is exception justified? Does it require new implementation patterns?
- ✅ Add to `related_specs` to link with tech stack spec

**Section 13 Entry**:
```
| CNF-001 | spec-architecture-tech-stack.md | Different tech choice: proposes Node.js instead of .NET | Justification: This specific microservice benefits from Node.js event loop model. Approved by tech lead on 2025-01-15. Will be documented as architecture exception in main tech stack spec. |
```

### 5. Incomplete Implementation (Conflict Level: MEDIUM)

**Symptom**: New spec describes next phase of partially-implemented spec

**Example**:
- Existing: `spec-architecture-user-auth.md` (implemented with basic auth)
- New: `spec-architecture-oauth2-integration.md` (adds OAuth2 to auth system)
- Problem: Related but different scope

**Resolution**:
- ✅ Create new spec (different scope)
- ✅ Link as `related_specs` in both specs
- ✅ Document dependency in tasks (new spec depends on existing)
- ✅ In section 5, document dependency on existing system

**New spec frontmatter**:
```yaml
related_specs:
  - spec-architecture-user-auth.md
```

**Section 13 Entry**:
```
| CNF-001 | spec-architecture-user-auth.md | Related implementation phase | Complements existing auth system. This spec builds on existing user auth (REQ: system must already have basic JWT auth implemented). See section 5 for dependencies. |
```

## Conflict Detection Workflow

### Step 1: Search Existing Specs

Run conflict detection script:
```bash
python .agents/skills/spec-generator/scripts/check-conflicts.py \
  "Your specification purpose here" \
  --type architecture
```

**Output example**:
```json
{
  "analysis": {
    "status": "conflict",
    "conflicts": [
      {
        "file": "spec-architecture-jwt-auth.md",
        "reason": "High overlap (score: 0.85)",
        "suggestion": "Review existing spec before creating duplicate"
      }
    ]
  }
}
```

### Step 2: Manual Review

Read each conflict spec:
1. Compare scope with new spec
2. Identify type of conflict (duplicate, overlapping, contradictory, etc.)
3. Determine severity (critical, high, medium, low)

**Questions to ask**:
- Is this a duplicate with different name?
- Do both specs add value or can they be merged?
- Are there contradictory requirements?
- Do they complement each other?
- Is one an update/replacement of the other?

### Step 3: Document in Section 13

Create conflict table in new spec:

```markdown
## 13. Conflict Detection & Resolution

### Conflict Analysis

| Conflict ID | Conflicting Spec | Conflict Description | Resolution Strategy |
|-------------|------------------|---------------------|---------------------|
| CNF-001 | spec-name.md | [Description] | [Strategy] |
| CNF-002 | spec-name-2.md | [Description] | [Strategy] |
```

### Step 4: Update Frontmatter

Update related/superseding specs:

```yaml
related_specs:
  - spec-related-1.md
  - spec-related-2.md
supersedes:
  - spec-old-version.md (if applicable)
```

### Step 5: Plan Spec Lifecycle

For related specs, consider lifecycle:

```
Active specs:
  - spec-architecture-jwt-auth.md (v1.0 - established)
  - spec-architecture-token-refresh.md (v1.0 - new enhancement)
  
Linked:
  - token-refresh depends on jwt-auth
  - Related in implementation
  
Implemented specs:
  - jwt-auth (ready for architecture/authentication folder)
```

## Resolution Strategies

### Strategy 1: Use Existing Spec

**When**: Duplicate or new spec adds no value

**Action**:
```
Do not create new spec
Use existing spec instead
Reference existing spec in documentation/tasks
```

**Example**: New spec is `spec-architecture-jwt-tokens.md` but `spec-architecture-jwt-auth.md` already covers this → use existing

### Strategy 2: Link as Related

**When**: Specs complement each other

**Action**:
```yaml
# In new spec
related_specs:
  - spec-architecture-old-related.md

# Update old spec to include new in related_specs
```

**Example**: 
- Spec A: JWT authentication
- Spec B: OAuth2 integration
- Both related, different scope → link them

### Strategy 3: Supersede & Replace

**When**: New spec is improved version of old spec

**Action**:
```yaml
# In new spec
version: 1.0.0
supersedes:
  - spec-architecture-old-auth-v1.md

# In section 13
| CNF-001 | spec-architecture-old-auth-v1.md | Superseded by improved version | This spec replaces old version. See migration guide below. |
```

**Example**: `spec-architecture-jwt-auth-v1.md` → `spec-architecture-jwt-auth-v2.md` with better security

### Strategy 4: Scope Separation

**When**: Specs cover different aspects of same domain

**Action**:
```yaml
# In both specs
related_specs:
  - [other related spec]

# In section 13, document scope boundary
| CNF-001 | spec-other.md | Different scope - both cover auth but: this covers JWT tokens, other covers user management | Complements. JWT tokens section 4 documents token format, user management spec covers user lifecycle. |
```

**Example**:
- Spec A: User authentication (login/logout)
- Spec B: JWT token management (generation/validation)
- Different scope → document relationship clearly

### Strategy 5: Defer Decision

**When**: Unclear how to resolve conflict

**Action**:
```yaml
# In section 13
| CNF-001 | spec-other.md | Needs stakeholder review - unclear if duplicate or complement | Defer to tech lead for clarification before implementation |

# In status field
status: review  # Need approval before draft → approved
```

**Example**: Conflict between new microservice spec and existing monolith architecture → needs tech lead review

## Conflict Resolution Decision Tree

```
New spec conflicts with existing?
│
├─ YES: Exact duplicate scope?
│  │
│  ├─ YES → Strategy 1: Use existing
│  │
│  └─ NO: Contradictory requirements?
│     │
│     ├─ YES: Can conflict be resolved?
│     │  │
│     │  ├─ YES (new approach better) → Strategy 3: Supersede
│     │  ├─ YES (keep old approach) → Strategy 1: Use existing
│     │  └─ UNCLEAR → Strategy 5: Defer
│     │
│     └─ NO: Complementary or different scope?
│        │
│        ├─ OVERLAPPING but different focus → Strategy 2: Link as related
│        ├─ SCOPED SEPARATION → Strategy 4: Document boundaries
│        └─ INDEPENDENT → No conflict
│
└─ NO: Proceed with spec creation
```

## Examples: Before & After

### Example 1: Duplicate Detection

**Before**:
```
Purpose: Implement OAuth2 authentication
Conflict check result: High overlap with spec-architecture-oauth2.md (score: 0.92)
```

**After**:
```
Action: Do not create spec
Rationale: spec-architecture-oauth2.md already comprehensively covers this scope
Alternative: Create enhancement spec for OAuth2 if planning new features (e.g., multi-factor auth)
```

### Example 2: Overlapping Specs

**Before**:
```
New spec: spec-design-login-form.md
Existing: spec-design-authentication.md
Conflict: Both cover authentication UI
```

**After**:
```yaml
# In new spec (spec-design-login-form.md)
related_specs:
  - spec-design-authentication.md

# Section 13
| CNF-001 | spec-design-authentication.md | Related: both cover auth UI but focus differs | This spec focuses on login form component details. See spec-design-authentication.md for overall auth flow and user experience. |
```

### Example 3: Superseding Version

**Before**:
```
Existing: spec-infrastructure-cicd-pipeline-v1.md
New: spec-infrastructure-cicd-pipeline-v2.md with improved deployment strategy
Conflict: Same scope, different approach
```

**After**:
```yaml
# In new spec
version: 2.0.0
supersedes:
  - spec-infrastructure-cicd-pipeline-v1.md
status: draft  # Need approval to replace v1

# Section 13
| CNF-001 | spec-infrastructure-cicd-pipeline-v1.md | Superseded: same scope with improved deployment strategy (blue-green vs rolling) | This v2 spec replaces v1. Key differences: section 4 describes new blue-green deployment. See migration guide at end of spec. |

# At end of spec
## Migration from v1 to v2
- v1 used rolling deployment
- v2 uses blue-green deployment
- Deployment scripts in the project's DAL/migration scripts folder
- Expected rollout: 2 sprints
```

---

**Key Takeaway**: Always run conflict detection, document findings in section 13, and resolve before implementation begins.
