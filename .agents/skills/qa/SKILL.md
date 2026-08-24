---
name: 'qa'
description: 'Runs interactive Q&A sessions. Use for human or agent-to-agent requirement gathering and clarification.'
---

# Q&A Skill

## Scope

- Conduct step-by-step interactive Q&A sessions.
- Applicable to human-agent and agent-agent (sub-agent) interactions.

## Principle 1: Build the Checklist First

- Analyse the prompt and identify all required questions.
- Output the checklist at the start of the session.
- Update checklist state (`[X]`, `[ ]`, `[~]`) after each answer.

```markdown
**Checklist** (items to cover):
- [ ] 1. <Question area 1>
- [ ] 2. <Question area 2>
```

## Principle 2: Strict Single Question Per Turn

- You **MUST ALWAYS** ask exactly one question per turn.
- Bundling multiple questions is a strict violation.
- Every question **MUST** include multiple-choice options.
- Every question **MUST** include an open free-text option.
- Every question **MUST** include an escape hatch option (Skip/I do not know).

## Principle 3: Interaction Formats

### Human Interaction

- Use the `ask_question` tool when available to present the question and options.

### Agent-to-Agent Interaction

- Use text-based communication via `send_message`.
- Format questions strictly in markdown.
- Sub-agents **MUST** reply with the chosen option letter or clear free-text.

```markdown
**[Context / Why this matters]**
<1-2 sentence explanation of why you need this information>

**Question N / TOTAL: <question text>**

Suggested answers:
A) <option 1>
B) <option 2>
C) Skip for now / I do not know
Free answer: Type your own response
```

## Principle 4: Handling Answers and Ambiguity

- Mark checklist item `[X]` and proceed if the answer is clear.
- You **MUST** disambiguate ambiguous answers from humans or agents.
- Ask further specific questions until obtaining unambiguous answers.
- Reopen earlier items if new answers contradict previous assumptions.
- Print a summary of resolved answers periodically to maintain context.
- Summarise all answers and confirm before closing when all items are `[X]`.

## Principle 5: Enrich Questions When Needed

- Ground questions using available agents or skills (`csharp-dto-generator`, `sql-schema-planner`).
- Search attached documentation or files before asking.
- Perform online searches only when local context is insufficient.
- Mention the source when enriching a question.

## Principle 6: Pre-Send Validation

- Self-correct before sending any message.
- Verify presence of a question mark.
- Verify presence of multiple-choice options.
- Verify presence of a free-text option.
- Verify there is exactly one question.
- Fix any violations before sending.

## Notes

- Keep explanations brief.
- Skip obvious questions but list them as `[X]` in the checklist.
- Use temporary files for context management only if strictly necessary.
- Adhere to `tmp-files-policy`.
- Delete temporary files when the session concludes.
