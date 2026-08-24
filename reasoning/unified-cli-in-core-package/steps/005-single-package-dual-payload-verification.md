---
step: "005"
title: "Technical Verification: Packaging an Executable / Tool Directly Inside a Single NuGet Package"
status: "in_progress"
created_at: "2026-08-24T15:01:20+02:00"
---

# Technical Verification: Packaging an Executable / Tool Directly Inside a Single NuGet Package

## Purpose

Verify whether it is technically possible to distribute a CLI executable/tool binary (exe/dll) **directly inside the single `PH.DbAppSettings` NuGet package** alongside the library `lib/` assemblies, how NuGet and .NET SDK resolve both payloads, and what exact invocation mechanisms this enables.

---

## 1. NuGet Package Anatomy: Can a Package Contain Both `lib/` and `tools/`?

**YES, ABSOLUTELY.** 

A `.nupkg` archive is a structured zip file defined by the Open Packaging Conventions (OPC). The .NET SDK and NuGet package managers allow multiple target folders to coexist within the same `.nupkg`:

```
PH.DbAppSettings.1.0.0.nupkg
├── lib/
│   └── net10.0/
│       ├── PH.DbAppSettings.dll             <-- Referenced by consumer projects via <PackageReference>
│       └── PH.DbAppSettings.xml
├── tools/
│   └── net10.0/
│       └── any/
│           ├── DotnetToolSettings.xml       <-- Required for 'dotnet tool install'
│           ├── dbappsettings.dll            <-- Executable tool entry point
│           ├── dbappsettings.runtimeconfig.json
│           └── [bundled tool dependencies]  <-- Isolated inside tools/ folder
└── build/
    ├── PH.DbAppSettings.props
    └── PH.DbAppSettings.targets             <-- Automatically imported by MSBuild
```

---

## 2. How the Two Payloads Behave in .NET SDK

### Payload A: The Library (`lib/net10.0/`)
- When a consumer project adds `<PackageReference Include="PH.DbAppSettings" />`, NuGet parses the package's nuspec `<dependencies>` and maps `lib/net10.0/PH.DbAppSettings.dll` to Roslyn compiler reference assemblies.
- The consumer project **only sees the library types** (`AddDbAppSettings`, `AppSettingsDbContext`, `IDbAppSettingsWriter`, etc.).

### Payload B: The Tool Binary (`tools/net10.0/any/`)
- The .NET CLI tool resolver looks exclusively at `tools/<tfm>/any/DotnetToolSettings.xml`.
- When a developer runs:
  ```bash
  dotnet tool install --global PH.DbAppSettings
  # or in .config/dotnet-tools.json:
  dotnet tool install PH.DbAppSettings
  ```
  The .NET SDK extracts the `tools/` payload to `~/.dotnet/tools` and creates the `dbappsettings` CLI command.

### Payload C: The MSBuild Target (`build/PH.DbAppSettings.targets`)
- When the package is installed in a project, MSBuild imports `build/PH.DbAppSettings.targets`.
- This enables executing the bundled tool binary directly from within the project via MSBuild or custom runner without installing a global tool:
  ```xml
  <Target Name="DbAppSettings">
    <Exec Command="dotnet &quot;$(MSBuildThisFileDirectory)../tools/net10.0/any/dbappsettings.dll&quot; $(Args)" />
  </Target>
  ```

---

## 3. The Crucial Dependency Isolation Rule

In a standard single-package distribution, the biggest pitfall is **dependency pollution** in the package nuspec `<dependencies>` section.

### The Problem to Avoid:
If tool dependencies (such as heavy database drivers or UI libraries) are listed under the package's `<dependencies>`, they will be transitively restored into every consumer web app.

### The Proven Solution:
1. The `<dependencies>` section of `PH.DbAppSettings` **only lists core library dependencies** (`Dapper`, `EFCore.Relational`, `Microsoft.Extensions.*`).
2. The tool binaries in `tools/net10.0/any/` are packaged with their own private dependencies bundled **directly inside the `tools/` folder** (or self-contained / ILMerged / published), leaving the consumer project's compilation graph 100% clean!

---

## 4. How the User/Developer Invokes the Tool in Practice

With this single unified package, the developer has **three powerful options** using just the 1 package:

1. **Option 1 (In-App CLI Runner - Zero Install, Auto-DB detection)**:
   In any project referencing `PH.DbAppSettings`:
   ```bash
   dotnet run -- dbappsettings analyze appsettings.json
   dotnet run -- dbappsettings import appsettings.json -y
   ```
2. **Option 2 (Local / Global .NET Tool from Same Package)**:
   ```bash
   dotnet tool install --global PH.DbAppSettings
   dbappsettings analyze appsettings.json
   ```
3. **Option 3 (MSBuild Direct Execution in Project)**:
   ```bash
   dotnet msbuild /t:DbAppSettings /p:Args="analyze appsettings.json"
   ```

---

## 5. Summary Matrix of Possibility

| Aspect | Is It Possible? | Implementation Mechanism |
| :--- | :---: | :--- |
| **1 Single NuGet Package (`PH.DbAppSettings.nupkg`)** | **YES** | Custom MSBuild pack targets bundling `lib/`, `tools/`, and `build/`. |
| **Bundling Tool EXE/DLL inside package `tools/`** | **YES** | Target `tools/net10.0/any/` with `DotnetToolSettings.xml`. |
| **Zero Transitive Dependency Pollution** | **YES** | Tool dependencies bundled privately in `tools/` directory, excluded from nuspec `<dependencies>`. |
| **Usable as `<PackageReference>` in C# code** | **YES** | `lib/net10.0/PH.DbAppSettings.dll` exposed standardly. |
| **Usable as `dotnet tool install PH.DbAppSettings`** | **YES** | .NET SDK recognizes `DotnetToolSettings.xml` in `tools/`. |
| **Usable as `dotnet run -- dbappsettings ...`** | **YES** | In-app CLI dispatcher in `PH.DbAppSettings.dll`. |
