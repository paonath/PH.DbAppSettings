# Server Syntax Matrix — SQL Schema Planner

Full syntax comparison across all supported servers. Use this as the reference during Step 3 (SQL Syntax Correctness).

---

## Supported Servers

| Server | Minimum Version | Default if unspecified |
|--------|----------------|----------------------|
| SQL Server | 2016 Express | ✅ **Yes** |
| PostgreSQL | 12+ | No |
| MariaDB | 10+ | No |
| MySQL | 8+ | No |

---

## Data Types

| Concept | SQL Server 2016+ | PostgreSQL 12+ | MariaDB 10+ / MySQL 8+ |
|---------|-----------------|----------------|------------------------|
| **Default ASSIGNED PK** | `varchar(128) NOT NULL` | `varchar(128) NOT NULL` | `varchar(128) NOT NULL` |
| Auto-increment PK *(override only)* | `int IDENTITY(1,1) NOT NULL` | `SERIAL` | `INT AUTO_INCREMENT NOT NULL` |
| Native UUID type | `uniqueidentifier` | `uuid` | `varchar(36)` |
| Short string | `varchar(n)` | `varchar(n)` | `varchar(n)` |
| Long string (bounded) | `varchar(n)` | `varchar(n)` | `varchar(n)` |
| Unbounded text | `varchar(max)` | `text` | `text` |
| Fixed-length string | `char(n)` | `char(n)` | `char(n)` |
| Boolean | `bit` — values `0`/`1` | `boolean` — `true`/`false` | `tinyint(1)` — `0`/`1` |
| Integer (32-bit) | `int` | `integer` | `int` |
| Integer (64-bit) | `bigint` | `bigint` | `bigint` |
| Decimal | `decimal(p,s)` | `decimal(p,s)` | `decimal(p,s)` |
| Float | `float` | `double precision` | `double` |
| Date only | `date` | `date` | `date` |
| Date + time **(UTC — default)** | `datetime2 not null` | `timestamp not null` | `datetime not null` |
| Date + time + offset stored | `datetimeoffset not null` | `timestamptz not null` | `datetime not null` *(no native tz — UTC by convention)* |
| Binary | `varbinary(max)` | `bytea` | `blob` |

---

## DDL Syntax

| Feature | SQL Server 2016+ | PostgreSQL 12+ | MariaDB 10+ / MySQL 8+ |
|---------|-----------------|----------------|------------------------|
| Batch separator | `GO` | *(none)* | *(none)* |
| Schema prefix | `dbo.Table` | `public.table` | `table` (no prefix) |
| Quoted identifiers | `[TableName]` or unquoted | `"TableName"` (preserves case) | `` `TableName` `` |
| Create table | `create table dbo.T (...)` | `create table public."T" (...)` | `create table T (...)` |
| Inline PK | `constraint T_pk primary key` | `constraint "T_pk" primary key` | `constraint T_pk primary key` |
| Inline FK | `constraint T_R_Id_fk references dbo.R` | `constraint "T_R_Id_fk" foreign key (...) references ...` | `constraint T_R_Id_fk references R (Id)` |
| Separate FK | `alter table ... add constraint ...` | `alter table ... add constraint ...` | `alter table ... add constraint ...` |
| Default value | `default value` inline | `default value` inline | `default value` inline |
| Boolean default true | `bit default 1 not null` | `boolean default true not null` | `tinyint(1) default 1 not null` |

---

## Column Descriptions / Comments

| Approach | SQL Server | PostgreSQL | MySQL / MariaDB |
|----------|-----------|------------|-----------------|
| **Junction table FK columns** | `exec sp_addextendedproperty ...` (after `GO`) | `comment on column schema."Table"."Col" is '...'` | Inline `comment '...'` on column definition |
| **Table-level description** | `exec sp_addextendedproperty ... 'TABLE', 'T', null, null` | `comment on table schema."T" is '...'` | `comment = '...'` at end of `create table` |
| **Inline column comment** | Not supported inline | Not supported inline | `column_name type comment 'text'` |

### SQL Server — sp_addextendedproperty template
```sql
exec sp_addextendedproperty 'MS_Description', 'description text',
     'SCHEMA', 'dbo', 'TABLE', 'TableName', 'COLUMN', 'ColumnName'
go
```

### PostgreSQL — COMMENT ON COLUMN template
```sql
comment on column public."TableName"."ColumnName" is 'description text';
```

### MySQL / MariaDB — Inline COMMENT template
```sql
create table TableName
(
    Id       varchar(128) not null,
    ParentId varchar(128) not null comment 'FK to Parent table',
    ...
)
```

---

## Index Syntax

| Feature | SQL Server | PostgreSQL | MySQL / MariaDB |
|---------|-----------|------------|-----------------|
| Non-unique | `create index IX_T_C on dbo.T (C)` | `create index "IX_T_C" on public."T" ("C")` | `create index IX_T_C on T (C)` |
| Unique | `create unique index UX_T_C on dbo.T (C)` | `create unique index "UX_T_C" on public."T" ("C")` | `create unique index UX_T_C on T (C)` |
| Batch separator | `go` after each | *(none)* | `;` |

---

## Server-Specific Limitations

### SQL Server 2016 Express
- Maximum database size: **10 GB**
- No SQL Server Agent (no scheduled jobs)
- No Resource Governor
- No Advanced Analytics
- `varchar(max)` stores up to 2 GB of text

### PostgreSQL 12+
- Identifiers are **lowercased** unless double-quoted — always use `"QuotedNames"` to preserve PascalCase
- `SERIAL` is shorthand for `integer` with an auto-sequence; prefer `GENERATED ALWAYS AS IDENTITY` in PG 10+

### MySQL 8+ / MariaDB 10+
- Strict mode (`STRICT_TRANS_TABLES`) active by default — `NOT NULL` without default will reject empty inserts
- No schema prefix; databases act as schemas
- `varchar(n)` limit is 65,535 bytes per row (shared with all columns); use `text` for large content
- `datetime` has no timezone awareness; always store UTC values and handle conversion application-side
- Column names for date+time fields MUST carry the `Utc` suffix (e.g., `CreateDateUtc`) — see `naming-conventions.md`
