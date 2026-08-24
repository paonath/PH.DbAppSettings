---
trigger: model_decision
description: Markdown style rules for AI-destined files (specs, skills, AGENTS.md)
globs: '**/SKILL.md, **/specs/**/*.md, **/AGENTS.md, **/agents/**/*.md, **/instructions/**/*.md'
---

## Scope

These rules apply to all AI-destined files: skills, specs, prompts, instructions, AGENTS.md, agent definitions.
Relaxed for human-only files: README, user guides, changelogs.

## Structure

- Use YAML frontmatter for metadata; quoted, single-line values.
- Headers: `#` title, `##` major sections, `###` sub-sections.
- Order: frontmatter, purpose/scope, core rules, examples, checklist/validation.
- One blank line between sections; no trailing blank lines at end of file.

## Prose

- Prefer bullet lists over paragraphs for rules, options, facts.
- Maximum one sentence per bullet; no filler words ("note that", "please", "simply").
- Use **bold** for key phrases in instructions.
- Use UPPERCASE only for short, high-priority keywords.
- Avoid combining **bold** and UPPERCASE on the same text unless truly critical.
- No introductory or closing filler prose.

## Lists

- Flat lists for simple content.
- Nested lists when hierarchy improves precision or readability.
- No fixed nesting cap; keep nesting intentional and easy to scan.
- List items start with a verb for actions (`Use`, `Avoid`, `Set`).

## Tables

- Use only when 3+ items share 2+ attributes.
- Column count max 5; header names max 3 words each.

## Code Blocks

- Always specify language (` ```csharp `, ` ```yaml `, etc.).
- Include only the minimum code illustrating the rule.
- Prefer inline code for single symbols or short expressions.

## Checkboxes

- `[X]` completed/true, `[ ]` incomplete/false, `[~]` partial/in-progress.
- **MUST NOT** use emoji checkmarks.

## Prohibited

- No emoji or unicode icons in AI-destined files.
- No images or image references.
- No markdown links (`[label](url)`) — use backtick-wrapped paths instead.
- No HTML tags.
- No inline HTML comments.
- No repeated content — reference existing sections or files instead.

## Token Efficiency

- Cut any word, sentence, or section that does not add new information.
- Prefer abbreviations in headers when unambiguous (`NFR`, `DI`, `API`).
- Replace multi-sentence explanations with a single example when self-evident.

## Post-Write Checklist

- [ ] No emoji or unicode icons
- [ ] No images or HTML tags
- [ ] Header hierarchy clear and proportionate
- [ ] List nesting intentional and readable
- [ ] Emphasis correct (prefer bold over uppercase)
- [ ] No filler prose or redundant sentences
- [ ] Code blocks have language specifiers
- [ ] Tables used only for 3+ items with 2+ attributes
- [ ] Checkboxes use `[X]`/`[ ]`/`[~]`
- [ ] Frontmatter present and single-line-valued
- [ ] File ends with single newline
- [ ] No duplicated content