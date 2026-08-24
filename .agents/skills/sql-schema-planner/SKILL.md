---
name: sql-schema-planner
description: |
  Plan and generate SQL DDL scripts for new schema objects (tables, views, stored procedures)
  following project naming conventions. Use when: (1) a user needs to design or plan new
  database tables or entities, (2) a user asks to model a new SQL schema, (3) another skill
  needs database planning support. DO NOT USE for queries, SELECT statements, reporting SQL,
  or any DML (INSERT/UPDATE/DELETE).
---

# SQL Schema Planner

Plan and generate SQL DDL for new schema objects following project naming conventions.
This skill is **exclusively a schema design and planning tool** — it never executes SQL and never produces queries.

## Critical Rules

### Rule 1 — Planning Only (HARD BOUNDARY)

- ✅ Design new tables, views, stored procedure **skeletons**
- ✅ Generate DDL: `CREATE TABLE`, `CREATE VIEW`, `CREATE PROCEDURE`, `CREATE INDEX`, `CREATE DATABASE`
- ✅ Produce output as markdown (default) or `.sql` file
- ✅ Be invoked by other skills for database design support
- ❌ **NEVER** generate `SELECT`, `INSERT`, `UPDATE`, `DELETE`, or any query SQL
- ❌ **NEVER** assist with querying, reporting, or interrogating a database
- ❌ **NEVER** write stored procedure logic — emit `/* TODO: implement logic */` placeholders only

**If the user asks for a query or DML**: decline, explain the scope boundary, and suggest a general-purpose agent.

### Rule 2 — NEVER Execute SQL

- ✅ Generate static text output only
- ❌ Never connect to any database or run any SQL command
- ❌ Never use any tool that executes SQL

---

## Scope Reference

| Object | Supported |
|--------|-----------|
| `CREATE TABLE` + `CREATE INDEX` | ✅ Always |
| `CREATE VIEW` | ✅ On request |
| `CREATE PROCEDURE` (skeleton only) | ✅ On request |
| `CREATE DATABASE` | ✅ On request |
| `ALTER TABLE` | ❌ Out of scope |
| DML / SELECT / queries | ❌ **NEVER** |

---

## Step 1 — Validate Input

Before generating any output, verify:

1. **Scope check**: Is the request DDL/schema planning? If not → decline and redirect immediately.
2. **Server**: Identify target server. Default: **SQL Server 2016 Express** if unspecified — state the assumption in output.
3. **Entities**: Are names clear and in English? If not → start a `qa` session (`.agents/skills/qa/SKILL.md`) with a clarification question.
4. **Relationships**: Are FK relationships stated or safely inferable? If not → add to the `qa` checklist.
5. **Columns**: Is the entity purpose clear enough to determine required columns? If not → add to the `qa` checklist.

Maximum **3 clarification iterations** (via `qa` skill), then proceed with documented assumptions.

---

## Step 2 — Apply Naming Conventions

Self-validate and self-correct before generating. See `references/naming-conventions.md` for full rules.

**Quick reference**:

| Element | Convention | Example |
|---------|-----------|---------|
| Tables / Views | PascalCase, singular | `User`, `OrderDetail` |
| Primary Key column | `Id` — always first | `Id varchar(128) not null` |
| PK strategy | **ASSIGNED** — application sets value | No `DEFAULT NEWID()`, no sequences |
| FK column | `{ReferencedTable}Id` | `PublisherId`, `CategoryId` |
| PK constraint | `{Table}_pk` | `Author_pk` |
| FK constraint | `{Table}_{RefTable}_Id_fk` | `Book_Publisher_Id_fk` |
| Non-unique index | `IX_{Table}_{Column}` | `IX_Book_Title` |
| Unique index | `UX_{Table}_{Column}` | `UX_Book_ISBN` |
| Schema (MSSQL) | `dbo` | `dbo.Author` |
| Schema (PostgreSQL) | `public` | `public.author` |
| Schema (MySQL/MariaDB) | none | `Author` |
| Columns nullable | `NOT NULL` by default | Add `null` only when user marks optional |
| DateTime column (UTC) | Suffix `Utc` — **always** | `CreateDateUtc`, `UpdateDateUtc`, `DeleteDateUtc` |
| DateTime column (local) | Suffix `LocalTime` — **only when explicitly requested** | `EventStartLocalTime` |

**PK type rule**: `varchar(128) NOT NULL` is the default. Use `int IDENTITY` / `SERIAL` / `AUTO_INCREMENT` **only on explicit user request** — add a `-- Note: PK override requested` comment.

**DateTime rule**: All date/time columns MUST be stored as UTC. Column names MUST carry the `Utc` suffix (`CreateDateUtc`, `UpdateDateUtc`, `DeleteDateUtc`). Use `LocalTime` suffix **only when the user explicitly requests local time**. Default types: `datetime2 not null` (MSSQL), `timestamp not null` (PostgreSQL), `datetime not null` (MySQL/MariaDB — no native tz, UTC by convention).

**On first table in any script**, add: `-- PK is application-assigned (varchar 128, GUID-compatible)`

If a naming check fails: self-correct silently and note the change with a `-- Note:` SQL comment.

---

## Step 3 — Generate and Validate SQL

Generate DDL for the target server using the Server Syntax Matrix (see `references/server-syntax-matrix.md`).

**Key syntax rules by server**:

- **SQL Server**: use `GO` batch separator after each statement; use `sp_addextendedproperty` for column descriptions on junction tables
- **PostgreSQL**: no `GO`; use `COMMENT ON COLUMN` for descriptions; `text` instead of `varchar(max)`
- **MySQL / MariaDB**: no `GO`; use inline `COMMENT 'text'` on column definition; `text` instead of `varchar(max)`; `tinyint(1)` for booleans

**Inline comments policy**:
- Add `--` comments only when column purpose is non-obvious from the name
- Always describe FK columns in junction/association tables
- Never add redundant comments (e.g., `-- Primary key` on `Id`)

If a syntax check fails: self-correct and use the right syntax for the target server.

---

## Output Format

### Default — Markdown with embedded DDL

~~~markdown
## Schema: [EntityName(s)]

**Server**: SQL Server 2016 Express | **Strategy**: ASSIGNED PKs

```sql
-- [table DDL here]
```

> 💡 Suggestions
> - FieldName (type) — reason
~~~

### On explicit `.sql` request

Plain `.sql` file containing DDL only, with a file-header comment block:
```sql
-- Schema: [EntityName(s)]
-- Server: [target]
-- Generated: [date]
-- PK strategy: ASSIGNED (application-assigned varchar(128))
```

---

## Skill-to-Skill Interface

When invoked by another skill, accept these parameters:

```
Tables:        [list of entity names or descriptions]
Server:        <sql-server | postgresql | mariadb | mysql>   (default: sql-server)
Output:        <markdown | sql>                               (default: markdown)
Schema:        <schema name>                                  (default: dbo / public)
Relationships: [list of FK relationships]                    (optional)
```

Return: markdown block with DDL in ` ```sql ` fences + Suggestions section.

---

## Suggestions Policy

- **Never add unrequested fields**
- Always end output with a `> 💡 Suggestions` blockquote for useful omitted fields
- Format: `FieldName (type) — reason` — max 5 items; omit if nothing meaningful

---

## Examples

See `examples/` for full working DDL:
- `example-mssql-library.md` — Library schema on SQL Server 2016 (canonical reference)
- `example-postgres-library.md` — Same schema on PostgreSQL 12

---

## References

See `references/` for detailed rules:
- `naming-conventions.md` — Complete naming convention rules
- `server-syntax-matrix.md` — Full syntax comparison across all supported servers
