---
step: "001"
title: "Technical Evaluation: NuGet Packaging Rules and .NET SDK Tool Mechanics"
status: "completed"
created_at: "2026-08-24T14:53:45+02:00"
---

# Technical Evaluation: NuGet Packaging Rules and .NET SDK Tool Mechanics

## Purpose

Analyze the internal structure, SDK behaviors, packaging targets, and lifecycle mechanics of .NET 10 NuGet packages when attempting to merge a class library and a CLI tool into a single assembly / single package.

## Discovered Facts

### 1. Structure of Standard Library Packages vs .NET Tool Packages

The .NET SDK (`Microsoft.NET.Sdk`) enforces strictly segregated packaging layouts in `.nupkg` archives:

| Feature / Artifact | Class Library Package (`<OutputType>Library</OutputType>`) | .NET Tool Package (`<PackAsTool>true</PackAsTool>`) |
| :--- | :--- | :--- |
| **Package Directory** | `lib/net10.0/` and/or `ref/net10.0/` | `tools/net10.0/any/` |
| **Manifest Metadata** | Standard nuspec metadata with `<dependencies>` | `DotnetToolSettings.xml` inside `tools/` |
| **Installation Method** | `<PackageReference Include="PackageId" />` in `.csproj` | `dotnet tool install --global` or `dotnet tool install --local` |
| **SDK Resolution** | Roslyn passes `lib/*.dll` to compiler as reference assemblies | .NET CLI copies binary to tool cache (`~/.dotnet/tools`) |
| **Executable Entry Point** | Not required (class library assembly) | Required (`<OutputType>Exe</OutputType>`, `Program.Main`) |

### 2. NuGet Resolution Constraints

1. **Pure Tool Package Collision**: If `<PackAsTool>true</PackAsTool>` is enabled on a project, `dotnet pack` generates assets exclusively in `tools/net10.0/any/`. If a consumer project adds `<PackageReference>` to this package, NuGet finds zero assemblies in `lib/` or `ref/`, resulting in compiler error `CS0246` (namespaces/types not found).
2. **Hybrid Package NuSpec Crafting**: A single `.nupkg` can technically be crafted manually (via custom `.nuspec` or MSBuild packaging targets) to contain both `lib/net10.0/PH.DbAppSettings.dll` and `tools/net10.0/any/PH.DbAppSettings.dll` with `DotnetToolSettings.xml`.
   - In this hybrid setup, `dotnet tool install -g PH.DbAppSettings` installs the CLI tool, while `<PackageReference Include="PH.DbAppSettings" />` compiles against the library.
   - However, NuGet treats package `<dependencies>` globally: all dependencies required for the tool (e.g. `Spectre.Console`, database drivers) become transitive dependencies of every consumer project adding the `<PackageReference>`.

### 3. Ecosystem Precedents in Microsoft .NET

Microsoft systematically separates tools from runtime libraries across the official ecosystem:
- `Microsoft.EntityFrameworkCore` (Core Library) vs `dotnet-ef` (Global/Local Tool).
- `Microsoft.Extensions.Diagnostics` (Runtime Library) vs `dotnet-dump` / `dotnet-trace` (Tools).
- `OpenIddict.Core` (Library) vs `OpenIddict.Cli` (Tool).

## Deductions and Analysis

- **Deduction 1 (SDK Separation Invariant)**: Out-of-the-box .NET SDK tooling does not support a single standard `.csproj` producing both a seamless `<PackageReference>` library and a standard `dotnet tool` executable without either custom nuspec packaging hacks or dependency pollution.
- **Deduction 2 (In-App Invocation Seam)**: If the goal is for a developer to install a single package and run commands without a separate tool install, the primary native mechanism in .NET is an **Embedded CLI Runner** inside the host application entry point (`dotnet run -- dbappsettings analyze`).

## Handoff

- Findings: .NET SDK packaging enforces separation between `lib/` and `tools/`. Hybrid packages are possible only with custom nuspec packaging, but share global dependency graphs.
- Confidence: high.
- Assumptions: The host application has `dotnet` SDK installed and can run `dotnet run` or local commands.
- Open questions: What is the exact dependency footprint if tool dependencies are pulled into the core assembly?
