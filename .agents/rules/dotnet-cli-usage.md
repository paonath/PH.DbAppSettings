---
trigger: model_decision
description: .NET CLI commands and patterns for .NET projects
globs: '**/*.csproj, **/*.sln, **/*.slnx'
---

## Project-Specific Commands

All commands run from the **solution root** (where the `.slnx` or `.sln` file is located).

```bash
# Build
dotnet build <SolutionName>.slnx

# Test (all)
dotnet test <SolutionName>.slnx

# Test (filtered by class)
dotnet test <SolutionName>.slnx --filter "FullyQualifiedName~<ServiceClassName>Tests"

# Test (filtered by method)
dotnet test <SolutionName>.slnx --filter "FullyQualifiedName~<ServiceClassName>Tests.<MethodName>"

# Clean before baseline analysis
dotnet clean <SolutionName>.slnx && dotnet build <SolutionName>.slnx
```

## General CLI Rules

- Run `dotnet` commands from the solution root (never individual project folders).
- Build at solution level to catch dependency issues.
- Use `dotnet clean` before full rebuilds when there are unexplained issues.
- Use `-c Release` for production builds and publishes.
- Use `Debug` builds during development for stack traces and diagnostics.
- **For large build output** (warnings/errors list): use `headroom_compress` to compress the output text and prevent context window exhaustion.

## NuGet Package Management

- Use `dotnet package add PackageName` (not manual `.csproj` edits).
- Pin versions explicitly in production projects.
- Run `dotnet restore` after manually editing package references.
- Use the `nuget-manager` skill for complex NuGet operations.

## Database (project-specific)

- **No EF Core Migrations**: schema changes go to numbered scripts in the project's designated diff/migration folder.
- Schema source of truth: the project's baseline SQL script (e.g., `lastVersionOfDb.sql` or equivalent).
- Never run `dotnet ef database drop`.

## Project Creation

- Always include `-f <tfm>` to pin the target framework.
- Determine the framework version per project from existing `.csproj` files.
- Use PascalCase with dots for project names: `<SolutionName>.Api`, `<SolutionName>.Dal`.
- After creating a project, add it to the solution: `dotnet sln add`.

## Testing

- Always run tests at the solution level.
- Use `--filter` to run a subset during development; full suite before committing.
- Never use `--no-build` in CI.
- **For verbose test execution logs**: use `headroom_compress` to compress logs before analysis, keeping the context clear.

## Anti-Patterns

- MUST NOT manually edit `<PackageReference>` to add packages; use `dotnet package add`.
- MUST NOT run migrations directly against production databases.
- MUST NOT omit `-f <tfm>` when creating new projects.