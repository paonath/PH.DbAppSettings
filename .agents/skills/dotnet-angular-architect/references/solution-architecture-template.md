# MinimalAPI + Angular — Reusable Solution Template

> **Usage**: Supply the three planning variables below. Every project name, namespace, folder path, and file name is derived from them.

---

## Planning Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `{SolutionName}` | Root name of the solution | `MyApp` |
| `{Entity}` | A **database entity** (table mapped via DAL) | `Project` |
| `{Dto}` | DTO record that represents `{Entity}` — lives in Models | `ProjectDto` |
| `{Service}` | C# **interface** declaring the business contract — no implementation | `IProjectService` |
| `{ServiceImpl}` | C# **class** implementing `{Service}` — lives in Services.Components | `ProjectService` |

Technology choices fixed at planning time (not encoded in this template):

- .NET version (server runtime)
- Angular version (client framework)
- Database engine + ORM/micro-ORM
- Authentication provider

---

## 0. Concept Definitions

| Concept | Nature | Rule |
|---------|--------|------|
| `{Entity}` | EF Core / ORM entity class | Maps 1-to-1 to a database table. Lives exclusively in the **DAL** project. Never exposed outside the DAL. |
| `{Dto}` | C# `record` with `init` properties | Represents `{Entity}` for API input/output. Lives in **Models**. Two variants per entity: `{Entity}Dto` (read) and `{Entity}CreateDto` (write). |
| `{Service}` | C# `interface` | Declares the business contract for an entity domain. Lives in **Services**. Contains no implementation whatsoever. |
| `{ServiceImpl}` | C# `class` implementing `{Service}` | Contains the actual business logic. Lives in **Services.Components**. Registered in `ServiceRegistration.cs`. |

---

## 0.1 Project Dependency Diagram

```
┌─────────────────────────────────────────────────────┐
│                    {SolutionName}.Api                │
│  (depends on: Dal, Models, Services,                 │
│               Services.Components, Common)           │
└──────────┬──────────────────────────────────────────┘
           │
    ┌──────▼────────────────────────────────────────┐
    │          {SolutionName}.Services.Components    │
    │  (depends on: Models, Services, Dal)           │
    └──────┬────────────────┬────────────────────────┘
           │                │
    ┌──────▼──────┐  ┌──────▼──────┐
    │  .Services  │  │    .Dal     │
    │  (→ Models) │  │             │
    └──────┬──────┘  └─────────────┘
           │
    ┌──────▼──────┐
    │   .Models   │
    └─────────────┘

    ┌─────────────────────────────────────────┐
    │   Common    (no dependencies)           │
    │   optional dependency for ALL projects  │
    └─────────────────────────────────────────┘
```

**Rules:**

- `Api` → depends on all projects (optionally `Common`).
- `Services` → depends on `Models` (optionally `Common`).
- `Services.Components` → depends on `Models`, `Services`, `Dal` (optionally `Common`).
- `Dal` → no mandatory dependencies (optionally `Common`).
- `Models` → no mandatory dependencies (optionally `Common`).
- `Common` → no dependencies.
- **No circular dependencies allowed.**

---

## 1. Naming Derivation Table

| Token | Pattern | Example (`{SolutionName}` = `MyApp`) |
|-------|---------|---------------------------------------|
| `{SolutionName}` | _(input)_ | `MyApp` |
| `{SolutionName}.Api` | Entry-point project | `MyApp.Api` |
| `{SolutionName}.Dal` | Data-access layer project | `MyApp.Dal` |
| `{SolutionName}.Models` | DTO models project | `MyApp.Models` |
| `{SolutionName}.Services` | Business logic project | `MyApp.Services` |
| `{SolutionName}.Services.Components` | Component/validator project | `MyApp.Services.Components` |
| `{SolutionName}Common` | Shared utilities project | `MyAppCommon` |
| `{SolutionName}.Services.Components.Tests` | Integration test project | `MyApp.Services.Components.Tests` |
| `{SolutionName}.Services.Component.Xunit3.Tests` | xUnit 3 test project | `MyApp.Services.Component.Xunit3.Tests` |
| Solution file | `{SolutionName}.sln` | `MyApp.sln` |
| API entry csproj | `{SolutionName}.Api.csproj` | `MyApp.Api.csproj` |
| Root namespace | `{SolutionName}` | `MyApp` |

---

## 2. Monorepo Folder Structure

```
{repo-root}/
├── client/                              # Angular SPA
│   ├── angular.json
│   ├── package.json
│   ├── tsconfig.json
│   └── src/
│       └── app/
│           ├── app.component.ts
│           ├── app.routes.ts
│           ├── models/                  # TypeScript DTO interfaces
│           │   └── {entity}/            # one folder per entity domain
│           └── service/                 # Angular services
│               ├── baseApiService.ts    # shared HTTP base
│               └── {entity}.service.ts
│
├── server/
│   └── {SolutionName}.Api/              # .NET solution root
│       ├── {SolutionName}.sln
│       ├── {SolutionName}.Api/          # Minimal API entry point
│       │   ├── AGENTS.md                # agent guide: endpoints, DI, auth conventions
│       │   ├── Program.cs
│       │   ├── GlobalUsings.cs
│       │   ├── ServiceRegistration.cs
│       │   └── Endpoints/
│       │       ├── EndpointMap.cs       # registers all MapGroup calls
│       │       └── {Entity}/
│       │           └── {Entity}Endpoints.cs
│       ├── {SolutionName}.Dal/          # Data-access layer
│       │   ├── AGENTS.md                # agent guide: entity classes, DbContext conventions
│       │   ├── Context.cs               # DbContext — contains DbSet<{Entity}>
│       │   ├── IUnitOfWork.cs
│       │   ├── UnitOfWork.cs
│       │   └── DapperBase.cs
│       ├── {SolutionName}.Models/       # DTOs only — one folder per entity
│       │   ├── AGENTS.md                # agent guide: DTO record conventions
│       │   └── {Entity}/
│       │       ├── {Entity}Dto.cs       # read DTO (maps from {Entity})
│       │       └── {Entity}CreateDto.cs # write DTO (input payload)
│       ├── {SolutionName}.Services/     # Business logic interfaces only (no implementations)
│       │   ├── AGENTS.md                # agent guide: interface-only conventions
│       │   └── I{Entity}Service.cs
│       ├── {SolutionName}.Services.Components/   # Implementations + validators
│       │   ├── AGENTS.md                # agent guide: service impl + validator conventions
│       │   ├── {Entity}Service.cs       # implements I{Entity}Service
│       │   ├── {Entity}Validator.cs
│       │   └── {Entity}ComponentService.cs
│       ├── {SolutionName}Common/        # Shared utilities and constants
│       │   └── AGENTS.md                # agent guide: cross-cutting utilities conventions
│       ├── {SolutionName}.Services.Components.Tests/
│       └── {SolutionName}.Services.Component.Xunit3.Tests/
│
├── diff/                                # Numbered SQL schema scripts
│   ├── 0 --- full-db-create-script.sql
│   └── {N} - {Description}.sql
│
├── specs/                               # Feature specs (design)
│   └── implemented/                     # Completed specs
│
└── docs/                                # Documentation
```

---

## 3. Server — Layer Responsibilities

| Project | Responsibility |
|---------|---------------|
| `{SolutionName}.Api` | Minimal API entry point — endpoint routing, DI registration, middleware, auth |
| `{SolutionName}.Dal` | `DbContext`, `IUnitOfWork`, raw SQL via `DapperBase`; owns all `{Entity}` EF classes |
| `{SolutionName}.Models` | DTOs only (`{Entity}Dto`, `{Entity}CreateDto`) — no EF entities here |
| `{SolutionName}.Services` | Business logic **interfaces only** — no implementations; all method signatures use Dto types |
| `{SolutionName}.Services.Components` | FluentValidation validators; registered in `ServiceRegistration.cs` |
| `{SolutionName}Common` | Cross-cutting constants, base classes, utilities |

### Endpoint conventions

- One static class `{Entity}Endpoints.cs` per entity, under `Endpoints/{Entity}/`.
- Routes: `GET /api/{entity}/`, `GET /api/{entity}/{id}`, `POST /api/{entity}/`, `PUT /api/{entity}/{id}`, `DELETE /api/{entity}/{id}`.
- Each group registered via `MapGroup` in `Program.cs`.
- Authorization applied at group level — never omit it.
- Validation via FluentValidation only — no ad-hoc manual checks inside handlers.

### DTO conventions (`{SolutionName}.Models`)

- `{Entity}Dto` — read model returned by endpoints; maps from `{Entity}` (DAL entity).
- `{Entity}CreateDto` — write payload received by `POST`/`PUT` endpoints.
- Both are `record` types with `init` properties.
- No EF entity classes belong here.
- `{Entity}Dto` extends `{Entity}CreateDto` adding only the Id property.

### Database change convention

Every schema change → new numbered file in `diff/`:

```
diff/{N} - {PascalDescription}.sql
```

- `{N}` = next sequential integer (no gaps, no EF migrations).

### AGENTS.md per-layer guidance

Every .NET project contains an `AGENTS.md` at its root. It guides AI agents operating in that specific layer with:

- What **belongs** in this project
- What does **not** belong here (and where it should go)
- Layer-specific naming and structural conventions
- Allowed project dependencies

Generate AGENTS.md content using the `solution-architect` skill at `.agents/skills/solution-architect/`.
Templates for each layer: `.agents/skills/solution-architect/references/agents-md-guide.md`.

---

## 4. Client — Layer Responsibilities

| Concern | Convention |
|---------|-----------|
| HTTP | All services extend `BaseApiService`; resource path set in constructor |
| State | Service-based RxJS — no NgRx/Akita |
| Models | TypeScript interfaces in `models/{entity}/` — mirror server DTOs |
| Components | Standalone components with inline template and styles |
| Auth | `@azure/msal-browser` — token attached in `BaseApiService` interceptor |

### Angular service conventions

- One service per entity domain: `{entity}.service.ts` in `service/`.
- Extends `BaseApiService`, passing the API route segment (e.g. `'{entity}'`) in the constructor.
- Exposes typed `Observable` methods mirroring CRUD endpoints.

### Angular model conventions

- Interfaces only (no classes) in `models/{entity}/{entity}.model.ts`.
- One `{Entity}Dto` interface and one `{Entity}CreateDto` interface per entity.
- Property names match the server DTO (camelCase in TypeScript).

### Angular component conventions

- Standalone components with inline `template` and `styles`.
- Use `inject()` for service injection.
- Feature components live in a subfolder named after the feature.

---

## 5. Auth — Microsoft Entra ID

| Side | Config location | Key values |
|------|----------------|------------|
| Server | `appsettings.json` → `"AzureAd"` section | `TenantId`, `ClientId`, `Audience` |
| Client | `environment.ts` → `msalConfig` / `apiConfig` | `clientId`, `authority`, `scopes` |

---

## 6. Build & Run

### Server

```
cd server/{SolutionName}.Api
dotnet restore && dotnet build
dotnet run --project {SolutionName}.Api/{SolutionName}.Api.csproj
```

Swagger UI available at `/swagger` in development.

### Client

```
cd client
npm install
npm start        # dev server
npm run build    # production build  (--base-href /client/)
```

---

## 7. Testing Conventions

### Server

- Framework: xUnit v3 (native Assert.* methods only — no third-party assertion libraries).
- Test classes inherit `TestBase<TFixture>`.
- `TestEntityFactory` generates test data deterministically.
- All test methods are `async Task`.
- No mocking libraries — inject real service instances via DI.

### Client

- Framework: Karma + Jasmine.

---

## 8. New Entity Checklist

Steps to add a new `{Entity}` to an existing solution (in order):

| # | Artifact | Location |
|---|----------|----------|
| 1 | Schema script | `diff/{N} - Add{Entity}Table.sql` |
| 2 | EF entity class | `{SolutionName}.Dal` |
| 3 | `DbSet<{Entity}>` registration | `{SolutionName}.Dal/Context.cs` |
| 4 | `{Entity}Dto` + `{Entity}CreateDto` | `{SolutionName}.Models/{Entity}/` |
| 5 | `I{Entity}Service` interface | `{SolutionName}.Services/` |
| 6 | `{Entity}Service` implementation | `{SolutionName}.Services.Components/` |
| 7 | `{Entity}Validator` | `{SolutionName}.Services.Components/` |
| 8 | Register service & validator | `{SolutionName}.Api/ServiceRegistration.cs` |
| 9 | `{Entity}Endpoints.cs` | `{SolutionName}.Api/Endpoints/{Entity}/` |
| 10 | `MapGroup` call | `{SolutionName}.Api/Program.cs` |
| 11 | TypeScript model interfaces | `client/src/app/models/{entity}/` |
| 12 | Angular service | `client/src/app/service/{entity}.service.ts` |
| 13 | Angular feature component(s) | `client/src/app/{feature}/` |

---

## 9. Instantiation Example

> `{SolutionName}` = **`Portal`** · `{Entity}` = **`Project`** · `{Dto}` = **`ProjectDto`**

| Token | Resolved value |
|-------|---------------|
| `{SolutionName}.Api` | `Portal.Api` |
| `{SolutionName}.Dal` | `Portal.Dal` |
| `{SolutionName}.Models` | `Portal.Models` |
| `{SolutionName}.Services` | `Portal.Services` |
| `{SolutionName}.Services.Components` | `Portal.Services.Components` |
| `{SolutionName}Common` | `PortalCommon` |
| Solution file | `Portal.sln` |
| DAL entity class | `Project` (in `Portal.Dal`) |
| DTO read model | `ProjectDto` (in `Portal.Models/Project/`) |
| DTO write model | `ProjectCreateDto` (in `Portal.Models/Project/`) |
| Service interface | `IProjectService` |
| Service class | `ProjectService` |
| Validator | `ProjectValidator` |
| Endpoint class | `ProjectEndpoints` |
| API route | `/api/project/` |
| Angular model file | `models/project/project.model.ts` |
| Angular service | `project.service.ts` |
