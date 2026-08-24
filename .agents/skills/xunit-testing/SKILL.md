---
name: xunit-testing
description: |
  Reusable skill for creating, reviewing, and updating xUnit tests in .NET projects.
  Use when: (1) adding tests for services, handlers, validators, or repositories,
  (2) improving naming, assertions, and determinism, (3) designing data-driven tests,
  (4) validating authorization and error paths, (5) reducing flaky tests without mocks.
---

# xUnit Testing Skill

## Overview

- Use this skill to produce production-grade xUnit tests with clear intent and stable execution.
- Keep examples generic so the skill can be reused in any .NET solution.
- Prefer real dependencies and concrete stubs/fakes over mocking frameworks.

## Triggers

- Create new tests for a class, service, endpoint handler, validator, or repository.
- Refactor existing tests to improve readability, reliability, and coverage.
- Add theory-driven coverage for multiple input scenarios.
- Add tests for authorization, validation errors, and edge cases.

## Input Contract

- Target under test: file path or symbol name.
- Test level: `unit`, `integration`, `handler-direct`.
- Coverage intent: happy path, invalid input, authorization, boundary conditions.
- Constraints: allowed libraries, execution time, fixture availability.

## Core Rules

### 1. Test Naming

- Use `Verb_Scenario_ExpectedResult` for all test methods.
- Keep method names specific enough to understand behavior without opening production code.
- Use `Fact` for a single scenario; use `Theory` for data variation.

### 2. AAA Structure

- Keep explicit `Arrange`, `Act`, `Assert` sections in each test.
- Test one behavioral intent per method.
- Keep assertions focused on the intended behavior.

### 3. Dependency Strategy

- Prefer real DI and real implementations when practical.
- Do not require mocking frameworks.
- Use minimal concrete stubs/fakes only when isolation is necessary.
- Use `IClassFixture<T>` for shared setup per test class.
- Use `ICollectionFixture<T>` only when shared setup across classes is truly needed.

### 4. Determinism

- Use unique seeded data per test.
- Avoid relying on test execution order.
- Avoid global static mutable state.
- Keep async flows fully async and awaited.
- Avoid time-based flakiness unless time behavior is the explicit subject.

### 5. Assertion Quality

- For typed endpoint/handler results, assert exact result type first.
- Assert key payload values after type assertions.
- Use collection assertions (`Single`, `Empty`, `Contains`, `All`) for filtering/query logic.
- Use exception assertions for failure paths (`Throws`, `ThrowsAsync`).

## Data-Driven Testing Rules

- Use `InlineData` for small static datasets.
- Use `MemberData` for computed or larger datasets.
- Use `ClassData` when dataset composition is complex.
- Keep parameter names explicit and semantically meaningful.

## Execution Workflow

1. **Research existing helpers**: Before writing or editing any tests, use TokenSave MCP tools (specifically `tokensave_search`, `tokensave_files`, or `tokensave_context`) to search for existing test patterns, fixtures, or helpers in the test project to avoid duplicating shared fixtures.
2. Identify the behavior contract and expected outcomes.
3. Choose test style: `unit`, `integration`, or `handler-direct`.
4. Build minimal setup using fixture/scope or concrete stub/fake.
5. Implement tests in this order:
   - success path
   - validation/error path
   - authorization path
   - boundary and edge cases
6. Run focused tests.
7. Run broader suite after focused tests pass.
8. **Compress large test outputs**: If running tests produces very large console outputs or log files (>100 lines), use the `headroom_compress` tool to shrink the log output before analyzing it.


## Pattern Templates

### Pattern A: Unit Test

```csharp
public sealed class CalculatorTests
{
    [Fact]
    public void Add_WithTwoPositiveNumbers_ReturnsSum()
    {
        // Arrange
        var sut = new Calculator();

        // Act
        var result = sut.Add(2, 3);

        // Assert
        Assert.Equal(5, result);
    }
}
```

### Pattern B: Data-Driven Theory Test

```csharp
public sealed class EmailValidatorTests
{
    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    public void Validate_WithVariousInputs_ReturnsExpected(string input, bool expected)
    {
        // Arrange
        var sut = new EmailValidator();

        // Act
        var result = sut.IsValid(input);

        // Assert
        Assert.Equal(expected, result);
    }
}
```

### Pattern C: Integration Test With Fixture

```csharp
public sealed class OrderRepositoryTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public OrderRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Save_WithValidOrder_PersistsToDatabase()
    {
        // Arrange
        var sut = new OrderRepository(_fixture.Context);
        var order = new Order("ord-1", 3);

        // Act
        await sut.SaveAsync(order, CancellationToken.None);
        var saved = await sut.GetByIdAsync("ord-1", CancellationToken.None);

        // Assert
        Assert.NotNull(saved);
        Assert.Equal(3, saved!.Quantity);
    }
}
```

### Pattern D: Direct Handler Test

```csharp
public sealed class ProductHandlersTests
{
    [Fact]
    public async Task GetById_WithExistingId_ReturnsOk()
    {
        // Arrange
        var service = new StubProductService(new ProductDto("p-1", "Keyboard"));

        // Act
        var result = await ProductHandlers.GetById("p-1", service, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<Ok<ProductDto>>(result.Result);
        Assert.Equal("p-1", ok.Value!.Id);
    }
}
```

### Pattern E: Authorization Test

```csharp
[Fact]
public async Task Delete_WhenUserHasNoRole_ReturnsForbid()
{
    // Arrange
    var ctx = HttpContextFactory.WithoutRoles();
    var service = new StubOrderService();

    // Act
    var result = await OrderHandlers.Delete("ord-1", ctx, service, CancellationToken.None);

    // Assert
    Assert.IsType<ForbidHttpResult>(result.Result);
}
```

## Setup and Teardown Patterns

- Use constructor setup for simple, synchronous dependencies.
- Use `IDisposable` cleanup for synchronous teardown.
- Use `IAsyncLifetime` for async setup/teardown.
- Use fixture classes for expensive shared setup.

## Optional xUnit Features

- Use `Trait` for coarse test categorization when useful.
- Use `ITestOutputHelper` for diagnostics in hard-to-debug scenarios.
- Use conditional skip sparingly and always include a reason.

## Anti-Patterns

- Do not test multiple unrelated behaviors in a single test method.
- Do not test private implementation details directly.
- Do not use ambiguous names like `Test1` or `ShouldWork`.
- Do not couple tests through shared mutable state.
- Do not hide arrange/setup complexity inside assertions.

## Quality Gate

- [ ] Test class ends with `Tests`.
- [ ] Method names follow `Verb_Scenario_ExpectedResult`.
- [ ] `AAA` sections are explicit.
- [ ] `Fact` vs `Theory` choice is intentional.
- [ ] Assertions are semantic and specific.
- [ ] Dependencies are real or concrete stubs/fakes.
- [ ] No deterministic risks (time/order/shared state).
- [ ] Error and authorization paths are covered when applicable.

## Output Contract

When executing this skill, return:

- Changed files.
- Added/updated tests grouped by behavior.
- Commands used to validate tests.
- Test execution summary.
- Residual risks or missing coverage.

## Reuse Notes

- Keep domain terms generic unless the target repository requires specific terminology.
- Add repository adapters only when explicitly requested.
- Keep this file self-contained so it can be reused without bundled examples.
