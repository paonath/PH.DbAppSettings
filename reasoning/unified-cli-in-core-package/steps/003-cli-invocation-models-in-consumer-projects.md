---
step: "003"
title: "Developer Invocation Ergonomics and Execution Models in Consumer Projects"
status: "completed"
created_at: "2026-08-24T14:54:35+02:00"
---

# Developer Invocation Ergonomics and Execution Models in Consumer Projects

## Purpose

Examine how a developer or CI/CD pipeline would execute CLI operations if the tooling is embedded inside the single `PH.DbAppSettings` package, comparing ergonomics, setup friction, and operational workflows.

## Evaluation of Invocations Models

### Model 1: In-App CLI Interceptor (`dotnet run -- dbappsettings ...`)

#### Concept
The core library provides an extension method on `IHost` or `WebApplication` (e.g. `app.RunDbAppSettingsCli(args)` or auto-intercepted during `AddDbAppSettings`):

```csharp
var app = builder.Build();

// Intercepts CLI commands if present in args; returns true and exits process if a command was handled
if (app.RunDbAppSettingsCli(args)) return;

app.Run();
```

#### Execution Syntax
```bash
# In the project directory:
dotnet run -- dbappsettings analyze appsettings.json
dotnet run -- dbappsettings import appsettings.json -y
dotnet run -- dbappsettings rewrite-json -o appsettings.json

# In production / Docker container:
dotnet MyApp.dll dbappsettings ingest appsettings.json -y
```

#### Key Ergonomic Advantages
1. **Zero Configuration Re-entry**: The CLI automatically leverages the host application's configured `AppDbContext`, connection strings, SQL dialect, and encryption keys. The user **never has to pass `-c "Data Source=..."` or `-d postgres`** because the app already knows its storage engine!
2. **Single Assembly Distribution**: Exactly 1 package is installed (`PH.DbAppSettings`).
3. **Container Entrypoint Automation**: Docker init containers can execute `dotnet MyApp.dll dbappsettings ingest appsettings.json -y` directly before starting the web server.

#### Trade-offs
- Requires running through `dotnet run` (small compilation check overhead during local dev).
- Requires a one-line interceptor in the host application's `Program.cs`.

---

### Model 2: MSBuild Custom Target (`dotnet msbuild /t:...`)

#### Concept
The NuGet package ships `build/PH.DbAppSettings.targets`.

#### Execution Syntax
```bash
dotnet msbuild /t:DbAppSettingsAnalyze /p:SourceFile=appsettings.json
```

#### Key Ergonomic Deficiencies
- Passing options via MSBuild properties (`/p:Flag=Value`) is verbose, rigid, and prone to shell escaping errors.
- Cannot easily support interactive user confirmation prompts (e.g. `Are you sure you want to delete appsettings.json? [y/N]`).
- Poor terminal formatting capabilities compared to standard console runners.

---

### Model 3: Standalone Global Tool (`dbappsettings`) [Status Quo]

#### Concept
Separate package `PH.DbAppSettings.Cli` installed globally or locally via `dotnet-tools.json`.

#### Execution Syntax
```bash
dbappsettings analyze appsettings.json
dbappsettings import appsettings.json -c "Data Source=app.db" -d sqlite
```

#### Key Ergonomic Advantages
- Can run anywhere on the filesystem without a .NET project.
- Rich interactive Spectre.Console terminal UI.

#### Trade-offs
- Developers must install and update a second package (`PH.DbAppSettings.Cli`).
- Must pass explicit connection strings and dialects on every invocation.

---

## Comparison Matrix

| Criteria | In-App CLI Interceptor (Single Pkg) | MSBuild Targets (Single Pkg) | Standalone Global Tool (Separate Pkg) |
| :--- | :--- | :--- | :--- |
| **Number of Packages** | **1 (`PH.DbAppSettings`)** | **1 (`PH.DbAppSettings`)** | 2 (`Core` + `Cli`) |
| **Connection String Auto-Detection** | **Automatic from App DI** | Manual or custom parsing | Manual `-c` flag |
| **Driver Compatibility** | **Uses Host App's Driver** | Complex MSBuild resolution | Bundles all 4 drivers |
| **Transitive Bloat** | **Zero (uses host DI)** | Zero | High (40+ MB in tool) |
| **Invocation Ergonomics** | `dotnet run -- dbappsettings ...` | `dotnet msbuild /t:...` | `dbappsettings ...` |
| **Interactive Prompts** | Supported (`System.Console`) | Not supported / Clumsy | Supported (`Spectre.Console`) |

## Handoff

- Findings: The In-App CLI Interceptor (`dotnet run -- dbappsettings ...`) is the most ergonomic single-package approach. It auto-resolves database connections and avoids all driver bloat.
- Confidence: high.
- Assumptions: Standard ASP.NET Core / Generic Host entry points are used by consumer projects.
- Open questions: How does this balance against users who want a standalone CLI for external database management?
