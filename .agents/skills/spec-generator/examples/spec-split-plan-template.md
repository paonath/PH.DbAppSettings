---
title: "Spec Split Plan: {topic}"
date_created: YYYY-MM-DD HH:mm:ss
original_purpose: "{verbatim Final Interpretation}"
status: pending
---

# Spec Split Plan: {topic}

## Reason for Split

{Explanation of why the original spec was too broad/ambiguous.  
List assessment criteria triggered: FA-001, FA-002, FA-003, FA-004.}

## Original SpecPurpose

> {verbatim Final Interpretation from Clarification Log}

## Proposed Specifications

Execute the following prompts **in sequence** using the `spec-generator` skill.
Each prompt is self-contained and ready to use.

---

### Spec 1 of N: {Sub-spec Title}

**Type**: {architecture|design|process|infrastructure|data|schema|tool}  
**Rationale**: {Why this must be defined first — it usually unblocks the others}  
**Depends on**: None

**Prompt to run**:
```
@spec-generator {Precise, unambiguous SpecPurpose for this sub-spec only}
```

---

### Spec 2 of N: {Sub-spec Title}

**Type**: {type}  
**Rationale**: {Why this comes second}  
**Depends on**: Spec 1

**Prompt to run**:
```
@spec-generator {Precise, unambiguous SpecPurpose for this sub-spec only}
```

---

### Spec N of N: {Sub-spec Title}

**Type**: {type}  
**Rationale**: {Why this comes last}  
**Depends on**: Spec 1, Spec 2

**Prompt to run**:
```
@spec-generator {Precise, unambiguous SpecPurpose for this sub-spec only}
```

---

## Execution Order Summary

| Order | Spec | Type | Depends on |
|-------|------|------|------------|
| 1 | {Sub-spec 1 title} | {type} | — |
| 2 | {Sub-spec 2 title} | {type} | Spec 1 |
| N | {Sub-spec N title} | {type} | Spec 1, 2 |

## Cross-cutting Notes

{Architectural decisions, shared terminology, or constraints that apply across all sub-specs.}
