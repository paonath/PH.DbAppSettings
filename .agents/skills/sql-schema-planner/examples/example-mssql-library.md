# Example: Library Schema — SQL Server 2016

**Server**: SQL Server 2016 Express
**PK strategy**: ASSIGNED — all `Id` values are set by application code (`varchar(128)`, GUID-compatible)
**Output format**: DDL script with `GO` batch separators

---

## What this example demonstrates

- `Id varchar(128) not null` as first column with named PK constraint (`{Table}_pk`)
- PK is application-assigned — no `DEFAULT NEWID()`, no sequences
- FK column naming: `PublisherId`, `CategoryId`, `BookId`, `AuthorId`
- FK constraint naming: `{Table}_{RefTable}_Id_fk`
- Index naming: `IX_{Table}_{Column}`
- `IsActive bit default 1 not null` — boolean with explicit default
- Junction table pattern (`BookAuthor`) with `sp_addextendedproperty` for FK column descriptions
- `GO` batch separator after every statement

---

## DDL Script

```sql
create database Library collate SQL_Latin1_General_CP1_CI_AS
go

use Library
go


create table dbo.Author
(
    Id        varchar(128) not null -- PK is application-assigned (varchar 128, GUID-compatible)
        constraint Author_pk
            primary key,
    FirstName varchar(255) not null,
    LastName  varchar(255) not null,
    Biography varchar(2000),        -- optional: may be null
    BirthDate date
)
go

create table dbo.Category
(
    Id          varchar(128)  not null
        constraint Category_pk
            primary key,
    Name        varchar(255)  not null,
    Description varchar(1000),
    IsActive    bit default 1 not null
)
go

create table dbo.Publisher
(
    Id      varchar(128) not null
        constraint Publisher_pk
            primary key,
    Name    varchar(255) not null,
    Address varchar(500),
    Phone   varchar(50),
    Email   varchar(255)
)
go

create table dbo.Book
(
    Id          varchar(128) not null
        constraint Book_pk
            primary key,
    Title       varchar(500) not null,
    ISBN        varchar(20),
    PublisherId varchar(128)
        constraint Book_Publisher_Id_fk
            references dbo.Publisher,
    CategoryId  varchar(128)
        constraint Book_Category_Id_fk
            references dbo.Category,
    PublishYear int,
    Pages       int,
    Language    varchar(50),
    Summary     varchar(2000)
)
go

create index IX_Book_ISBN
    on dbo.Book (ISBN)
go

create index IX_Book_Title
    on dbo.Book (Title)
go

create index IX_Book_CategoryId
    on dbo.Book (CategoryId)
go

create index IX_Book_PublisherId
    on dbo.Book (PublisherId)
go

-- Junction table: many-to-many between Book and Author
create table dbo.BookAuthor
(
    Id       varchar(128) not null
        constraint BookAuthor_pk
            primary key,
    BookId   varchar(128) not null
        constraint BookAuthor_Book_Id_fk
            references dbo.Book,
    AuthorId varchar(128) not null
        constraint BookAuthor_Author_Id_fk
            references dbo.Author
)
go

exec sp_addextendedproperty 'MS_Description', 'A book can have multiple authors', 'SCHEMA', 'dbo', 'TABLE',
     'BookAuthor', 'COLUMN', 'BookId'
go

exec sp_addextendedproperty 'MS_Description', 'Multiple authors on same book', 'SCHEMA', 'dbo', 'TABLE', 'BookAuthor',
     'COLUMN', 'AuthorId'
go
```

---

> 💡 Suggestions
> - `CreatedAt (datetime2)` on `Book` — audit timestamp for record creation
> - `DeletedAt (datetime2)` on `Book` — soft-delete support
> - `Subtitle (varchar(500))` on `Book` — secondary title
> - `Country (varchar(100))` on `Publisher` — publisher location
