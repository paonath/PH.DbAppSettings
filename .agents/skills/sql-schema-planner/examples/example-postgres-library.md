# Example: Library Schema — PostgreSQL 12

**Server**: PostgreSQL 12+
**PK strategy**: ASSIGNED — all `Id` values are set by application code (`varchar(128)`, GUID-compatible)
**Output format**: DDL script without batch separators; `COMMENT ON COLUMN` for descriptions

---

## Key differences from SQL Server version

| Aspect | SQL Server | PostgreSQL |
|--------|-----------|------------|
| Batch separator | `GO` | *(none)* |
| Schema prefix | `dbo.Table` | `public.table` |
| Boolean | `bit default 1` | `boolean default true` |
| Large text | `varchar(max)` | `text` |
| Column descriptions (junction) | `sp_addextendedproperty` | `COMMENT ON COLUMN` |
| Case sensitivity | Identifiers case-insensitive | Identifiers lowercased unless quoted |

---

## DDL Script

```sql
create database Library;

\c Library;


create table public."Author"
(
    "Id"        varchar(128) not null, -- PK is application-assigned (varchar 128, GUID-compatible)
    "FirstName" varchar(255) not null,
    "LastName"  varchar(255) not null,
    "Biography" text,
    "BirthDate" date,
    constraint "Author_pk" primary key ("Id")
);

create table public."Category"
(
    "Id"          varchar(128) not null,
    "Name"        varchar(255) not null,
    "Description" varchar(1000),
    "IsActive"    boolean default true not null,
    constraint "Category_pk" primary key ("Id")
);

create table public."Publisher"
(
    "Id"      varchar(128) not null,
    "Name"    varchar(255) not null,
    "Address" varchar(500),
    "Phone"   varchar(50),
    "Email"   varchar(255),
    constraint "Publisher_pk" primary key ("Id")
);

create table public."Book"
(
    "Id"          varchar(128) not null,
    "Title"       varchar(500) not null,
    "ISBN"        varchar(20),
    "PublisherId" varchar(128),
    "CategoryId"  varchar(128),
    "PublishYear" int,
    "Pages"       int,
    "Language"    varchar(50),
    "Summary"     text,
    constraint "Book_pk" primary key ("Id"),
    constraint "Book_Publisher_Id_fk" foreign key ("PublisherId") references public."Publisher" ("Id"),
    constraint "Book_Category_Id_fk"  foreign key ("CategoryId")  references public."Category"  ("Id")
);

create index "IX_Book_ISBN"        on public."Book" ("ISBN");
create index "IX_Book_Title"       on public."Book" ("Title");
create index "IX_Book_CategoryId"  on public."Book" ("CategoryId");
create index "IX_Book_PublisherId" on public."Book" ("PublisherId");

-- Junction table: many-to-many between Book and Author
create table public."BookAuthor"
(
    "Id"       varchar(128) not null,
    "BookId"   varchar(128) not null,
    "AuthorId" varchar(128) not null,
    constraint "BookAuthor_pk"           primary key ("Id"),
    constraint "BookAuthor_Book_Id_fk"   foreign key ("BookId")   references public."Book"   ("Id"),
    constraint "BookAuthor_Author_Id_fk" foreign key ("AuthorId") references public."Author" ("Id")
);

comment on column public."BookAuthor"."BookId"   is 'A book can have multiple authors';
comment on column public."BookAuthor"."AuthorId" is 'Multiple authors on same book';
```

---

> **Note on quoted identifiers**: PostgreSQL lowercases unquoted identifiers. The examples above use quoted identifiers (`"Author"`) to preserve PascalCase as defined by the naming conventions. Omit quotes only if lowercase names are acceptable in your project.

---

> 💡 Suggestions
> - `created_at (timestamp)` on `"Book"` — audit timestamp for record creation
> - `deleted_at (timestamp)` on `"Book"` — soft-delete support
> - `subtitle (varchar(500))` on `"Book"` — secondary title
> - `country (varchar(100))` on `"Publisher"` — publisher location
