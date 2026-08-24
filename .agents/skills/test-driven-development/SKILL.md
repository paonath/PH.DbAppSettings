---
name: test-driven-development
description: Enforces disciplined red-green-refactor TDD - write a failing test first, watch it fail for the right reason, write the minimum code to make it pass, then refactor with the suite green throughout. Use this skill whenever implementing a new feature, fixing a bug, adding a method, or changing behavior in any codebase with automated tests (xUnit, NUnit, MSTest, Jasmine/Karma, Jest, Vitest, pytest, and similar) - and by default for any non-trivial implementation task, even if the user doesn't say the word "test". Also trigger on "TDD", "test-first", "red-green-refactor", on requests to review whether an existing test suite is trustworthy (over-mocked, assertion-free, or coupled to implementation details), or when setting up mutation testing (Stryker.NET, StrykerJS) to check whether tests actually catch bugs.
compatibility: Works with any Agent Skills-compatible coding agent - Claude Code, Claude Cowork, Claude.ai, GitHub Copilot, Google Antigravity, Cursor, Codex. Assumes a project with an existing or creatable automated test suite; no special tools required beyond the project's own test runner. Optional deeper reading in dotnet-xunit.md and angular-testing.md assumes a .NET/xUnit backend and an Angular/TypeScript frontend, but the core cycle applies to any language.
metadata:
  version: "1.1"
  tags: [testing, tdd, red-green-refactor, xunit, dotnet, angular, mutation-testing, quality]
---

# Test-Driven Development

TDD is a design discipline that happens to produce tests as a byproduct. You write a test for behavior that doesn't exist yet, watch it fail, write the smallest amount of code that makes it pass, and only then clean up the result. The test comes first because a test written after the code tends to confirm whatever the code already does - bugs included. A test written first is a specification you have to satisfy.

Core principle: **if you never watched the test fail, you don't actually know whether it tests the right thing.** A test that goes green on the first run proves nothing - it might be passing for a reason that has nothing to do with your implementation.

## Why this matters more, not less, when an agent is writing the code

An agent under time pressure to "get it done" behaves a lot like a rushed human, except faster and with no embarrassment about cutting corners. Left to its own devices, the default failure mode is: implement first, then generate tests that describe what the code already does. Those tests pass by construction and verify nothing. A second, sneakier failure mode shows up when a test is already failing: instead of fixing the underlying logic, weaken the assertion, add a special case that only satisfies the test input, or quietly comment the test out to "make progress." Kent Beck ran into exactly this while building a library with an AI agent and described the tendency plainly: the agent doesn't want to do TDD, it wants to write code and then write tests that pass. He only got dependable output once he forced strict red-green-refactor and stayed alert for the agent trying to make a test pass by rewriting the test instead of the code.

A failing test that you (or the person you're working with) wrote before the implementation existed is one of the few objective, non-negotiable checkpoints in an agentic workflow - it can't be satisfied by confident prose or a plausible-looking diff. Treat it that way.

## The cycle: Red -> Green -> Refactor

Work from a running list of test scenarios you want to cover (add to it as new cases occur to you mid-cycle - don't let a good idea derail the current step). Then, for each scenario, in order:

1. **Red** - Write one small test for the next bit of behavior. Predict how it should fail before running it. Run it. It must fail for the right reason (missing behavior, not a typo, not a compile error you didn't expect). If it fails for the wrong reason, fix the test first and re-run before moving on.
2. **Green** - Write the minimum code required to pass that test, and only that test. Hard-coded return values are a legitimate intermediate step ("fake it") - the next test you add will force you to generalize. Resist the urge to build the general solution early; let the tests pull it out of you one case at a time (this is called triangulation). Commit whatever small sins are necessary here; you'll atone in the next step.
3. **Refactor** - With everything green, improve structure without changing behavior: remove duplication, clarify names, extract abstractions. Do not add new behavior here, and do not mix this step with step 2 - "make it work" and "make it right" are different activities with different risk profiles. Re-run the full suite after refactoring, not just the test you were focused on.

Each cycle should take a few minutes. If you're stuck in red or green for much longer than that, the step was too big - back up and split it into something smaller.

## Non-negotiable rules

- Never write production code that isn't driven by a currently-failing test. If you notice code is needed before a test exists for it, stop and write the test first.
- Never watch a test go straight to green without having seen it fail first. If a test passes immediately, treat that as a bug in the test, not a lucky implementation.
- Never "fix" a failing test by loosening the assertion, hard-coding the expected value from the actual output, deleting the test, or marking it skipped/ignored - unless you stop and flag it to the person you're working with first. All of these move the specification to match a bug instead of fixing the bug.
- Never bundle refactoring into the same step as making a test pass. Green means "stop and switch hats," not "keep going while it's convenient."
- **Mandatory Phase Output: You MUST explicitly state which phase you are in by using a strict visual format at the very beginning of your output (e.g., `[PHASE: RED]`, `[PHASE: GREEN]`, `[PHASE: REFACTOR]`). This forces you to "think out loud" before generating any code, ensuring the process is coherent and auditable rather than a black box that just produced a diff.**

## Rationalizations to reject

These show up constantly, from humans and agents alike. None of them survive contact with the actual reasoning:

| The excuse | Why it doesn't hold |
|---|---|
| "I'll write the tests after, it's faster." | A test written after the code tends to confirm the code's current behavior, bugs and all, instead of specifying the intended behavior. It also reliably gets skipped once the feature "works." |
| "This is too trivial to need a test." | Trivial code is exactly the cheapest code to test. If it's genuinely trivial, the test costs you thirty seconds. |
| "The test almost passes, let me just adjust the expected value to match." | That is editing the specification to match a bug, not fixing the bug. If you didn't independently know the expected value was wrong, don't change it. |
| "I manually checked it in the browser/Postman, it works." | Manual verification isn't repeatable and doesn't survive the next refactor. It's not a substitute for an automated test, it's a nice sanity check in addition to one. |
| "Writing the test first would take too long for this deadline." | Debugging untested AI-generated or hand-written code later costs far more than the test would have upfront - this is the whole empirical case for TDD, and it gets stronger, not weaker, when code is generated quickly. |
| "The mock returns what I need, so the test is fine." | Check what you're actually asserting on. If the assertion only exercises the mock's return value, you've tested the mock, not your code. |

## Writing tests that actually test something

- **Behavior, not implementation.** Ask "what should this do" (the public contract), not "how does it do it" (private methods, call order, internal collaborators). Implementation-coupled tests break on every refactor even when behavior is unchanged, which trains everyone to stop trusting - and eventually stop running - the test suite.
- **AAA structure.** Arrange (set up the scenario), Act (invoke the one thing under test), Assert (check the outcome). One clear Act step per test; if you need two, you're probably testing two behaviors and should split the test.
- **FIRST.** Tests should be Fast, Independent of each other (no shared mutable state, no ordering dependency), Repeatable in any environment, Self-validating (pass/fail, no manual log-reading), and Timely (written just before the code, not weeks later).
- **One behavior per test, named for the behavior.** Prefer names that describe scenario and expectation (`Should_RejectUpload_When_ChecksumMismatch`, `CreateUser_WithDuplicateEmail_ReturnsConflict`) over names that describe implementation (`Test1`, `CallsRepositorySave`).
- **Avoid mocking wherever possible - it's a last resort, not a default.** Before reaching for a mocking library, ask: can I just use the real object (true for almost all of your own internal domain logic)? Failing that, can a small hand-written fake do the job, more simply and more honestly than a mock setup? Failing that, is there a fast, isolated version of the real infrastructure available (in-memory/SQLite database, injectable fake clock, local container)? Only reach for `Mock<T>()`/`jest.fn()` and friends when none of those fit - typically a wide third-party interface you don't control, or a side effect with no other observable trace. Use real instances for your own internal collaborators (domain objects, value types, simple calculators, mappers) - real code exercising real code is what actually catches regressions. A test built almost entirely out of mocks and stubs is usually validating the mocks, not the system. See `references/anti-patterns.md` for the full catalog (The Mockery, The Inspector, The Liar, The Giant, Excessive Setup, and more) and the mocking hierarchy before adding test infrastructure.
- **Coverage is a smoke detector, not a target.** Chasing a coverage percentage produces tests that execute lines without asserting anything meaningful. Aim for meaningful coverage of business-critical paths and edge cases instead of a number. For code where correctness really matters, run a mutation testing pass (Stryker.NET for C#, StrykerJS for TypeScript/Angular - see the stack-specific references) to check whether the suite would actually catch a real bug, not just execute the line it's on.

## Stack-specific guidance

- Working in the .NET / C# backend (xUnit, Moq/NSubstitute, EF Core, Dapper, minimal APIs) -> read `references/dotnet-xunit.md` before writing tests.
- Working in the Angular / TypeScript frontend (components, services, signals) -> read `references/angular-testing.md` before writing tests; it also covers the Karma/Jasmine -> Jest/Vitest landscape so you pick the right idioms for the project's actual runner instead of assuming one.
- Either way, check the project's existing test files first for established naming conventions, fixture/builder helpers, and mocking libraries already in use, and follow them rather than introducing a second convention.

## Before calling anything done

**Mandatory Checklist Printing:** You MUST literally print the following checklist at the end of each completed cycle (or before declaring a task finished) and physically check the boxes (using `[x]` or `[ ]`) to guarantee an explicit self-evaluation. Don't just assert that it's true:

- [ ] Every new or changed behavior has a test that you personally watched fail before the implementation existed.
- [ ] The full test suite is green, not just the test(s) you were focused on.
- [ ] No test was skipped, commented out, or had its assertion weakened to get here.
- [ ] Refactoring (if any) happened as its own step, after green, with the suite re-run afterward.
- [ ] For risk-critical or complex logic, consider whether a quick mutation testing pass is warranted before you're confident, rather than trusting coverage percentage alone.

If any box doesn't honestly check (remains `[ ]`), say so explicitly rather than reporting success - a clear "this is red because X, here's what I'd need to proceed" is more useful than a false "done."

## When TDD isn't the right tool

Don't force it onto throwaway spikes, pure exploratory prototyping, one-off migration scripts, or generated boilerplate where there's no behavior worth specifying yet. Say so plainly and propose the lighter-weight alternative (a quick manual check, a follow-up characterization test once the shape settles) instead of going through the motions. For anything that will live in the codebase and be maintained - a new endpoint, a bug fix, a service method, a component with actual logic - default to TDD unless told otherwise.