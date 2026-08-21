# Naming Conventions — SQL Schema Planner

Full naming rules for all schema objects. These rules apply across all supported servers unless noted.

---

## Tables

| Rule | Detail |
|------|--------|
| Case | **PascalCase** |
| Number | **Singular** noun |
| Language | **English** (unless user explicitly overrides) |
| Examples | `User`, `OrderDetail`, `ProductCategory`, `BookAuthor` |

---

## Views

| Rule | Detail |
|------|--------|
| Case | PascalCase |
| Prefix | No mandatory prefix; use descriptive name |
| Examples | `ActiveProduct`, `UserOrderSummary` |

---

## Primary Key

| Rule | Detail |
|------|--------|
| Column name | Always `Id` |
| Position | Always **first column** |
| Default type | `varchar(128) NOT NULL` |
| Default strategy | **ASSIGNED** — value set by application code (GUID/ULID), never by the database |
| Forbidden | `DEFAULT NEWID()`, `DEFAULT gen_random_uuid()`, sequences, `IDENTITY` unless overridden |
| Override | Use `int IDENTITY(1,1)` / `SERIAL` / `AUTO_INCREMENT` only on **explicit user request**; add `-- Note: PK override requested` comment |
| Script annotation | First `Id` column in every script gets: `-- PK is application-assigned (varchar 128, GUID-compatible)` |
| PK constraint name | `{Table}_pk` |

---

## Foreign Keys

| Rule | Detail |
|------|--------|
| Column name | `{ReferencedTable}Id` — concatenation of the referenced table name + `Id` |
| Examples | `PublisherId` (references `Publisher`), `CategoryId` (references `Category`) |
| Constraint name | `{Table}_{ReferencedTable}_Id_fk` |
| Example constraint | `Book_Publisher_Id_fk` |
| Nullable | FK columns are nullable by default (omitting `NOT NULL`) unless explicitly specified otherwise |

---

## Indexes

| Type | Convention | Example |
|------|-----------|---------|
| Non-unique | `IX_{Table}_{Column}` | `IX_Book_Title` |
| Unique | `UX_{Table}_{Column}` | `UX_Book_ISBN` |
| Composite | `IX_{Table}_{Col1}_{Col2}` | `IX_Order_UserId_Status` |

Create indexes on:
- All FK columns (always)
- Columns likely used in WHERE / JOIN clauses (on request)
- Columns requiring uniqueness (UX)

---

## Constraints

| Type | Convention | Example |
|------|-----------|---------|
| Primary key | `{Table}_pk` | `Author_pk` |
| Foreign key | `{Table}_{RefTable}_Id_fk` | `Book_Publisher_Id_fk` |
| Unique | `{Table}_{Column}_uq` | `Book_ISBN_uq` |
| Check | `{Table}_{Column}_chk` | `Order_Status_chk` |
| Default | inline `DEFAULT value` — no named constraint required | `IsActive bit default 1` |

---

## Columns

| Rule | Detail |
|------|--------|
| Case | PascalCase |
| Nullability | `NOT NULL` by default; add `null` (or omit `NOT NULL`) only when the user marks a field as optional |
| Omission | Never add columns not explicitly requested; suggest omitted useful columns in the Suggestions section |
| Boolean default | `bit default 1 not null` (MSSQL) / `boolean default true not null` (PostgreSQL) / `tinyint(1) default 1 not null` (MySQL) |

---

## DateTime / Temporal Columns

> **Rule**: All date/time values MUST be stored as UTC unless the user explicitly requests local time.

| Rule | Detail |
|------|--------|
| **Default** | Always UTC |
| **UTC suffix** | Append `Utc` to the column name — makes the timezone intent explicit in the schema |
| **LocalTime suffix** | Append `LocalTime` — use **only when explicitly requested** by the user |
| **Never** | Date+time columns without a timezone suffix (`CreateDate`, `UpdatedAt`, `ModifiedDate` are all wrong) |

### Naming Examples

| Concept | Correct name | Incorrect |
|---------|-------------|----------|
| Creation timestamp | `CreateDateUtc` | `CreateDate`, `CreatedAt`, `CreatedOn` |
| Last update timestamp | `UpdateDateUtc` | `UpdatedAt`, `ModifiedDate` |
| Soft-delete timestamp | `DeleteDateUtc` | `DeletedAt`, `DeletedOn` |
| Expiry date+time | `ExpiryDateUtc` | `ExpiryDate`, `ExpireAt` |
| Published date+time | `PublishDateUtc` | `PublishedAt` |
| Event start (local, explicit) | `EventStartLocalTime` | `EventStart`, `StartDate` |

### Data Types for UTC Storage

| Server | Recommended type | Notes |
|--------|-----------------|-------|
| SQL Server 2016+ | `datetime2 not null` | Preferred over `datetime`; higher precision |
| PostgreSQL 12+ | `timestamp not null` | Store UTC value; no offset stored |
| MySQL 8+ / MariaDB 10+ | `datetime not null` | No native tz — UTC by application convention |
| Date only (no time) | `date` (all servers) | No suffix required when no time component |

> For MSSQL: use `datetimeoffset` only when the offset itself must be persisted (e.g., original user timezone for audit purposes). In that case suffix is still `Utc` as the stored point-in-time is UTC-aligned.

### Suggestions Policy for DateTime

When the user designs an entity without audit timestamps, suggest in the Suggestions section:
- `CreateDateUtc (datetime2)` — record creation timestamp (UTC)
- `UpdateDateUtc (datetime2)` — last update timestamp (UTC)

---

## Schema Prefix

| Server | Default prefix | Example |
|--------|---------------|---------|
| SQL Server | `dbo` | `dbo.Author` |
| PostgreSQL | `public` | `public."Author"` |
| MySQL / MariaDB | none | `Author` |

User may override the schema name — accept via the `Schema` parameter in skill-to-skill calls.

---

## Stored Procedures

| Rule | Detail |
|------|--------|
| Name | PascalCase prefix `usp_` (user stored procedure) + verb + noun | 
| Example | `usp_GetBook`, `usp_CreateOrder` |
| Body | Skeleton only — `/* TODO: implement logic */` placeholder, no actual logic |

---

## Junction / Association Tables

- Name: concatenation of the two related table names in PascalCase (`BookAuthor`, `UserRole`)
- Always include an `Id varchar(128) not null` PK (ASSIGNED strategy applies)
- Always describe FK columns using server-appropriate comment mechanism:
  - SQL Server: `sp_addextendedproperty`
  - PostgreSQL: `COMMENT ON COLUMN`
  - MySQL/MariaDB: inline `COMMENT 'text'`
