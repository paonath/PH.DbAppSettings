# AGENTS.md Templates per Project Layer

Generate one AGENTS.md per project by substituting `{SolutionName}` and listing all concrete entity names.
Keep each file ≤ 60 lines.

---

## Root repository AGENTS.md

**File:** `AGENTS.md` (repo root)

Content to include:
- One-paragraph description of the solution purpose
- Reference to `specs/{solution-name}-blueprint.md` for full architecture
- Reference to `.agents/templates/solution-architecture-template.md` for conventions
- Cross-cutting rules: no EF migrations (SQL scripts in `diff/`); specs in `specs/`; completed specs in `specs/implemented/`
- Never: duplicate entity classes between projects; add business logic to the Api layer

---

## {SolutionName}.Api — AGENTS.md

**Purpose:** Minimal API entry point. Routes HTTP, registers DI, configures middleware and authentication.

**Belongs here:**
- `Program.cs` — DI registration, middleware pipeline, all `MapGroup` calls
- `ServiceRegistration.cs` — extension methods for scoped service and validator registration
- `GlobalUsings.cs` — global using directives
- `Endpoints/{Entity}/{Entity}Endpoints.cs` — one static class per entity _(list each entity explicitly)_
- `Endpoints/EndpointMap.cs` — registers all endpoint groups

**Does NOT belong here:**
- Business logic → `{SolutionName}.Services.Components`
- Database access → `{SolutionName}.Dal`
- DTO definitions → `{SolutionName}.Models`
- FluentValidation validators → `{SolutionName}.Services.Components`

**Conventions:**
- Endpoint handlers are `private static async` methods
- One subfolder per entity under `Endpoints/`
- Authorization via `.RequireAuthorization()` at group level — never omitted
- No manual validation: inject `IValidator<T>` from DI only

**Dependencies:** Dal, Models, Services, Services.Components, Common (optional)

---

## {SolutionName}.Dal — AGENTS.md

**Purpose:** Data-access layer. Owns all EF entity classes and the DbContext. Boundary between application and database.

**Belongs here:**
- `Context.cs` — DbContext; one `DbSet<{Entity}>` per entity _(list each entity and its DbSet explicitly)_
- `IUnitOfWork.cs` + `UnitOfWork.cs` — unit of work pattern
- `DapperBase.cs` — raw SQL helper
- EF entity classes — one class per database table, mirroring columns exactly

**Does NOT belong here:**
- DTOs → `{SolutionName}.Models`
- Business logic → `{SolutionName}.Services.Components`
- FluentValidation validators → `{SolutionName}.Services.Components`

**Conventions:**
- Entity classes are never exposed outside this project
- Every new entity → new `DbSet<T>` added to `Context.cs`
- Every schema change → new numbered SQL script in `diff/` — never EF Migrations

**Dependencies:** Common (optional)

---

## {SolutionName}.Models — AGENTS.md

**Purpose:** DTO layer. Contains only C# `record` types for API input/output. No EF entity classes.

**Belongs here:**
- `{Entity}Dto` — read model returned by endpoints; maps from `{Entity}` (adds Id) _(list all)_
- `{Entity}CreateDto` — write payload for POST/PUT endpoints _(list all)_
- One folder per entity: `{Entity}/{Entity}Dto.cs` + `{Entity}CreateDto.cs`

**Does NOT belong here:**
- EF entity classes → `{SolutionName}.Dal`
- Business logic → `{SolutionName}.Services.Components`
- Service interfaces → `{SolutionName}.Services`

**Conventions:**
- All records use `init` properties only
- `{Entity}Dto` extends (or includes all properties of) `{Entity}CreateDto`, adding only `Id`
- No mapping logic inside record types themselves

**Dependencies:** Common (optional)

---

## {SolutionName}.Services — AGENTS.md

**Purpose:** Contract layer. Contains **only** C# `interface` declarations. Zero implementation code.

**Belongs here:**
- `I{Entity}Service.cs` — interface for each entity's business operations _(list each entity explicitly)_
- Standard CRUD contract: `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`

**Does NOT belong here:**
- Service implementations → `{SolutionName}.Services.Components`
- DTO definitions → `{SolutionName}.Models`
- EF entity classes → `{SolutionName}.Dal`
- Any `class` that is not an `interface`

**Conventions:**
- Every interface method is `async` (returns `Task` or `Task<T>`)
- Methods operate only on `{Dto}` types — never on `{Entity}` classes
- No concrete class belongs in this project

**Dependencies:** Models, Common (optional)

---

## {SolutionName}.Services.Components — AGENTS.md

**Purpose:** Implementation layer. Contains service implementations and FluentValidation validators. Registered via `ServiceRegistration.cs` in the Api project.

**Belongs here:**
- `{Entity}Service.cs` — implements `I{Entity}Service` from Services _(list each entity)_
- `{Entity}Validator.cs` — FluentValidation validator for `{Entity}CreateDto` _(list each entity)_
- `{Entity}ComponentService.cs` — optional secondary component services

**Does NOT belong here:**
- Service interfaces → `{SolutionName}.Services`
- DTO definitions → `{SolutionName}.Models`
- Endpoint routing → `{SolutionName}.Api`

**Conventions:**
- Every `{Entity}Service` takes `IUnitOfWork` + `ILogger<T>` via constructor injection
- Never bypass `IUnitOfWork` for database access
- Every new service + validator must be registered in `{SolutionName}.Api/ServiceRegistration.cs`
- No ad-hoc validation in service methods — delegate to `{Entity}Validator` only

**Dependencies:** Models, Services, Dal, Common (optional)

---

## {SolutionName}Common — AGENTS.md

**Purpose:** Cross-cutting utilities. Constants, base classes, extension methods, and helpers shared across all projects.

**Belongs here:**
- Constants and enums used by more than one layer
- Base classes with no layer-specific dependencies
- Extension methods with no layer-specific dependencies

**Does NOT belong here:**
- Anything specific to a single layer
- Business logic or validation rules
- EF entities or DTOs

**Conventions:**
- This project depends on **nothing** — no references to other solution projects
- Think twice before adding here; prefer the appropriate layer when in doubt

**Dependencies:** none
