# xUnit Testing SKILL - Quick Start & Navigation

## Version Policy

- Use ALWAYS the latest stable xUnit version available at implementation time.
- Official xUnit website: https://xunit.net/
- Official xUnit documentation: https://xunit.net/docs/getting-started/v3/getting-started

## What Is This?

A reusable xUnit testing skill for creating, reviewing, and improving C# tests in modern .NET projects.

**Use This SKILL When**:
- Creating new test files
- Adding tests to existing code
- Reviewing test quality
- Learning xUnit patterns
- Testing repositories, services, or entities

## 30-Second Template

Copy this to start a new test:

```csharp
using Xunit;
using Example.Models;

namespace Example.Tests;

public class ArticoloTests
{
    /// <summary>
    /// Create_WithValidData_ReturnsEntity: Tests that entity creation succeeds
    /// </summary>
    [Fact]
    public void Create_WithValidData_ReturnsEntity()
    {
        // Arrange
        var id = "test-123";
        var name = "Test Article";
        
        // Act
        var articolo = new Articolo { Id = id, Name = name };
        
        // Assert
        Assert.NotNull(articolo);
        Assert.Equal(id, articolo.Id);
    }
}
```

**Key Pattern**: `[Verb]_[Scenario]_[ExpectedResult]`

## Main Topics

| Topic | Location | When to Read |
|-------|----------|--------------|
| **Overview** | [SKILL.MD](SKILL.MD#overview) | First time using the skill |
| **Core Rules** | [SKILL.MD](SKILL.MD#core-rules) | Naming, AAA, determinism, assertions |
| **Data-Driven Rules** | [SKILL.MD](SKILL.MD#data-driven-testing-rules) | Fact vs Theory, test data inputs |
| **Execution Workflow** | [SKILL.MD](SKILL.MD#execution-workflow) | Step-by-step test implementation |
| **Pattern Templates** | [SKILL.MD](SKILL.MD#pattern-templates) | Copy templates for common scenarios |
| **Quality Gate** | [SKILL.MD](SKILL.MD#quality-gate) | Final validation before commit |

## Examples

Complete test examples are in `examples/`:

- `test-unit-entity-creation.cs` — Basic unit test with [Fact]
- `test-unit-entity-validation.cs` — Parametrized test with [Theory]
- `test-integration-efcore.cs` — Database operations
- `test-integration-repository.cs` — Repository pattern
- `test-validation-rules.cs` — FluentValidation tests

## Quick Rules

✅ **DO**
- Use `[Fact]` for single test case
- Use `[Theory]` + `[InlineData]` for multiple scenarios
- Name tests: `Verb_Scenario_Result` (e.g., `Create_WithValidData_ReturnsEntity`)
- Use ALWAYS the latest stable xUnit version
- Inject real implementations (no Moq)
- Document test with XML comment

❌ **DON'T**
- Use Moq or mock frameworks
- Have >1 assertion per concept (2-5 total per test)
- Name tests vaguely (`Test1`, `CreateTest`)
- Depend on other tests or execution order
- Use current DateTime in tests

## Scope

- This README is generic and reusable across projects.
- Replace sample types in examples with your project domain models.

## File Structure

```
.agents/skills/xunit-testing/
├── SKILL.md                          # Full guidance & patterns
├── README.md                         # This file
├── examples/                         # Copy-paste test examples
│   ├── test-unit-entity-creation.cs
│   ├── test-unit-entity-validation.cs
│   ├── test-integration-efcore.cs
│   ├── test-integration-repository.cs
│   └── test-validation-rules.cs
└── references/                       # Reference docs
    ├── naming-conventions.md
    ├── assertions-guide.md
    ├── patterns.md
    └── di-patterns.md
```

## Next Steps

1. **Read** [SKILL.MD](SKILL.MD) for the complete workflow
2. **Select** the right pattern in `Pattern Templates`
3. **Apply** the `Quality Gate` checklist
4. **Run** focused tests before full suite execution
5. **Use** the latest stable xUnit version from https://xunit.net/

---

**Last Updated**: 2026-01-16 | **Version**: 1.0 | **Status**: Production
