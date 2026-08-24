---
description: Orchestrates TDD-first specification generation using spec-generator and test-driven-development skills, structuring tasks into explicit Red-Green-Refactor cycles.
---


1. Activate `prompt-clarifier` and `qa` skills if `SpecPurpose` or user prompt requires disambiguation.
2. Activate `test-driven-development` skill (`.agents/skills/test-driven-development/SKILL.md`) to analyze project test infrastructure (xUnit, Jest, Vitest, pytest, MSTest), test commands, and mocking rules.
3. Review existing source code and specs in `/specs/` to detect conflicts, name conventions, and domain contracts.
4. Activate `spec-generator` skill (`.agents/skills/spec-generator/SKILL.md`) to create a specification under `/specs/` with the following mandatory TDD structural overrides:
   - **Section 6 (Acceptance Criteria)**: Must format all acceptance criteria as Given/When/Then contracts detailing the expected test failure mode during the RED phase.
   - **Section 7 (Test Automation Strategy)**: Must specify exact test execution commands for individual test runs (RED/GREEN verification) and full-suite runs (REFACTOR verification), enforcing minimal mocking and AAA structure.
   - **Section 11 (Task Breakdown)**: Must structure all implementation work into explicit YAML TDD Triads:
     - `TASK-xxx-RED`: Write failing unit/integration test covering specific public behavior contract; state expected failure reason (`[PHASE: RED]`).
     - `TASK-xxx-GREEN`: Implement minimum code to satisfy the test contract (`[PHASE: GREEN]`).
     - `TASK-xxx-REFACTOR`: Refactor code and test structure while maintaining green test suite (`[PHASE: REFACTOR]`).
5. Validate the generated specification to guarantee that no production code task exists without a preceding failing test task.
6. Report completed spec location under `/specs/` and instruct user to execute via `spec-executor`.