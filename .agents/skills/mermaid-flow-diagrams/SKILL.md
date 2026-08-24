---
name: mermaid-flow-diagrams
description: Use whenever producing a markdown (.md) document — for human readers, other AI agents/skills, or both — that describes a complex flow, such as a multi-step process, branching logic, state transitions, a sequence of interactions between actors/systems/services, or structured relationships (data/entity models, class/type structures, ACL/permission hierarchies). Applies to any skill or agent writing README files, architecture docs, ADRs, workflow/runbook descriptions, API/integration docs, or AI-readable context files. Trigger this even if the calling skill or user didn't explicitly ask for a "diagram" — words like flow, workflow, sequence, pipeline, state machine, architecture, process, or descriptions of how components/systems interact are enough. Do NOT trigger for simple linear explanations, single-step instructions, or documents with no structural complexity to convey.
---

# Mermaid Flow Diagrams

## Why this matters

Markdown documents produced by skills and agents often serve two audiences at once: a human reading the file, and another AI agent that will later read the same file as context (to continue work, debug, or reason about the system). Both audiences benefit from the same thing when a flow gets complex: an explicit, unambiguous representation of the structure — not just prose.

Mermaid is well suited for this because it's plain text (diffable, greppable, version-controllable) that most markdown renderers display as a real diagram, and that LLMs parse reliably as a graph rather than having to infer structure from a paragraph. A diagram next to the prose reduces the chance that a future reader — human or AI — misreads the order of steps, missing branches, or which actor does what.

**The diagram is an aid, not a replacement.** It sits alongside the textual description, never instead of it. Someone skimming needs the prose to understand intent and context; someone tracing exact logic needs the diagram. Neither one alone tells the full story — the prose carries the "why," the diagram carries the "shape."

## When to add a diagram

Add a Mermaid diagram when the content you're documenting has genuine structural complexity:

- A process with more than ~3 sequential steps, or any branching/conditional logic
- Interactions between two or more actors, services, or systems (e.g., client → API → auth provider → database)
- A state machine (an entity that moves through distinct states over time)
- Relationships between data entities, types, or classes that aren't obvious from a flat list
- A pipeline or workflow that other steps/skills depend on

**Don't** add one for:
- A single linear sequence with no branches ("first do X, then Y, then Z" — that's fine as a numbered list)
- Something already fully clear from a short paragraph
- Padding — a diagram with 2 trivial nodes adds noise, not clarity

If you're unsure, ask: "would a reader (human or AI) get something from seeing the structure that they wouldn't get from the sentence describing it?" If no, skip it.

## How to add it

Use a fenced code block with the `mermaid` language tag, placed immediately after (or before) the paragraph describing the same flow:

```markdown
The service authenticates requests using one of three schemes depending on
the caller's context, then issues a JWT scoped to that scheme.

​```mermaid
sequenceDiagram
    participant Client
    participant API
    participant LDAP
    participant EntraID

    Client->>API: Login request
    alt LDAP scheme
        API->>LDAP: Validate credentials
    else Entra ID scheme
        API->>EntraID: Validate OIDC token
    end
    API-->>Client: JWT issued
​```

The chosen scheme is recorded in the token's issuer claim so downstream
services know which validator to use.
```

Notice the pattern: prose before, diagram in the middle, prose continues after if there's more to say. Never let the diagram be the *only* explanation of a non-trivial flow.

## Choosing the right diagram type

| Use case | Diagram type | Keyword to open with |
|---|---|---|
| Ordered steps, decisions, branches | Flowchart | `flowchart TD` (or `LR` for wide/short flows) |
| Interactions between actors/systems over time, request/response | Sequence diagram | `sequenceDiagram` |
| An entity moving through distinct states | State diagram | `stateDiagram-v2` |
| Data/entity relationships, schema | Entity-relationship diagram | `erDiagram` |
| Types, classes, inheritance, object structure | Class diagram | `classDiagram` |
| Chronology of events/releases | Timeline / Gantt | `timeline` / `gantt` |

When a flow doesn't cleanly fit one type (e.g., it's part sequence, part decision tree), pick the type that captures the *dominant* structure and let the prose cover the rest — don't force one diagram to do everything.

### Quick syntax reference

**Flowchart** — steps and branches:
```
flowchart TD
    A[Receive request] --> B{Valid token?}
    B -->|yes| C[Process request]
    B -->|no| D[Return 401]
```

**Sequence diagram** — who talks to whom, in order:
```
sequenceDiagram
    participant Client
    participant Server
    Client->>Server: Request
    Server-->>Client: Response
```

**State diagram** — lifecycle of an entity:
```
stateDiagram-v2
    [*] --> Draft
    Draft --> Submitted
    Submitted --> Approved
    Submitted --> Rejected
    Rejected --> Draft
    Approved --> [*]
```

**ER diagram** — data relationships:
```
erDiagram
    NODE ||--o{ NODE_CLOSURE : "has ancestors/descendants"
    NODE ||--o{ ACL_ENTRY : "has permissions"
```

**Class diagram** — types and structure:
```
classDiagram
    class NodeId {
        +Guid Value
    }
    class Node {
        +NodeId Id
        +string Name
    }
    Node --> NodeId
```

## Writing good labels

Use descriptive node and participant names — `ValidateToken`, not `A`; `AuthService`, not `S2`. A diagram with generic single-letter labels loses most of its value: the reader (or the AI parsing it later) has to cross-reference a legend instead of reading the graph directly. Keep labels short but meaningful, matching the actual names used in the codebase or spec where possible (e.g., real class names, real service names) so the diagram stays traceable to the real system.

## Avoiding broken diagrams

A diagram that fails to render is worse than no diagram — it's dead weight in the file. Before finalizing:

- Every fenced block opens and closes with matching triple-backtick + `mermaid` / triple-backtick
- Special characters in labels that can break parsing (`()`, `{}`, `"`, `:`, `#`) are quoted, e.g. `A["Check (retry < 3)"]`
- Sequence diagrams don't use `style` directives — they're not supported inside `sequenceDiagram`
- Diagrams stay reasonably sized (roughly under ~20-25 nodes/participants); if a flow is bigger than that, split it into two focused diagrams rather than one sprawling one that's hard to read at a glance
- If you have a way to render/preview Mermaid in your current environment, do a quick check before finishing — otherwise, keep the syntax to the well-established patterns in this file, which are known to render cleanly on GitHub, GitLab, VS Code-family editors, and Obsidian

## Summary checklist

Before finishing a document that includes a Mermaid diagram, confirm:

- [ ] The diagram documents something with genuine structure (branches, multiple actors, states, or relationships) — not a trivial linear list
- [ ] The same flow is also explained in prose — the diagram supplements, never substitutes
- [ ] The diagram type matches the content (sequence vs. flowchart vs. state vs. ER vs. class)
- [ ] Node/participant labels are descriptive, not generic letters
- [ ] The fenced block and syntax are well-formed
