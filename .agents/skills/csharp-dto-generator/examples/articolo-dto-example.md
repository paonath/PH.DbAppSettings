# Example: Articolo → ArticoloDto

This walkthrough shows the complete transformation of the `Articolo` EF Core entity
into a pair of DTO records.

---

## Input: `Articolo` entity (`YourApp.Dal/Models/Articolo.cs`)

```csharp
[Table("Articoli")]
public class Articolo : Entity<string>
{
    [Key]
    public override required string Id { get; set; }           // GROUP A — Primary Key

    public required string SerialNumber { get; set; }          // GROUP B — scalar
    public required string Categoria { get; set; }             // GROUP B — scalar
    public required DateTime DataCreazione { get; set; }       // GROUP B — scalar
    public required string Stagione { get; set; }              // GROUP B — scalar
    public required string CodArt { get; set; }                // GROUP B — scalar
    public required string Variante { get; set; }              // GROUP B — scalar
    public required string Abbinamento { get; set; }           // GROUP B — scalar
    public string? PathImg { get; set; }                       // GROUP B — nullable scalar
    public string? Note { get; set; }                          // GROUP B — nullable scalar

    public string? UserIdLockedBy { get; set; }                // excluded — FK for navigation
    public string? IdVersioneRiferimento { get; set; }         // excluded — internal tracking

    public virtual User? UserLocked { get; set; }              // GROUP C — navigation, excluded
    public virtual ICollection<Versione> Versioni { get; set; }                          // GROUP C — collection, excluded
    public virtual ICollection<ArticoloAnagraficaMacrocomponente> AnagraficaMacrocomponenti { get; set; }  // GROUP C
    public virtual ICollection<ArticoloAnagraficaComponente> AnagraficaComponenti { get; set; }           // GROUP C
}
```

### Property classification decisions

| Property | Group | Reason |
|---|---|---|
| `Id` | A — PK | `[Key]` + overrides `Entity<string>.Id` |
| `SerialNumber` … `Note` | B — include | Non-virtual scalar types |
| `UserIdLockedBy` | excluded | FK backing field for `UserLocked` navigation |
| `IdVersioneRiferimento` | excluded | Internal audit / cloning reference; not part of the public API |
| `UserLocked` … `AnagraficaComponenti` | C — exclude | `virtual` navigation / collection properties |

> **Note**: FK backing fields (`UserIdLockedBy`) may be included or excluded based on
> whether the consumer needs to supply or read them. In this project they are excluded
> because locking and versioning are managed by separate business logic, not by the caller.

---

## Output: `ArticoloDto.cs` (`YourApp.Models/`)

```csharp
namespace YourApp.Models.Dtos;

/// <summary>
/// DTO for the Articolo entity.
/// </summary>
public record ArticoloDto : ArticoloCreateDto
{
    /// <summary>
    /// Gets the primary key of the Articolo entity.
    /// </summary>
    /// <remarks>This is the primary key.</remarks>
    public string Id { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArticoloDto"/> record.
    /// </summary>
    public ArticoloDto() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArticoloDto"/> record with all properties.
    /// </summary>
    /// <param name="id">The primary key of the Articolo entity.</param>
    /// <param name="serialNumber">The serial number of the Articolo.</param>
    /// <param name="categoria">The category of the Articolo.</param>
    /// <param name="dataCreazione">The creation date of the Articolo.</param>
    /// <param name="stagione">The season of the Articolo.</param>
    /// <param name="codArt">The article code.</param>
    /// <param name="variante">The variant of the Articolo.</param>
    /// <param name="abbinamento">The combination of the Articolo.</param>
    /// <param name="pathImg">The image path of the Articolo.</param>
    /// <param name="note">The notes for the Articolo.</param>
    public ArticoloDto(
        string id,
        string serialNumber,
        string categoria,
        DateTime dataCreazione,
        string stagione,
        string codArt,
        string variante,
        string abbinamento,
        string? pathImg,
        string? note
    ) : base(serialNumber, categoria, dataCreazione, stagione, codArt, variante, abbinamento, pathImg, note)
    {
        Id = id;
    }

    /// <summary>
    /// Deconstructs the ArticoloDto into its properties.
    /// </summary>
    /// <param name="id">The primary key of the Articolo entity.</param>
    /// <param name="serialNumber">The serial number of the Articolo.</param>
    /// <param name="categoria">The category of the Articolo.</param>
    /// <param name="dataCreazione">The creation date of the Articolo.</param>
    /// <param name="stagione">The season of the Articolo.</param>
    /// <param name="codArt">The article code.</param>
    /// <param name="variante">The variant of the Articolo.</param>
    /// <param name="abbinamento">The combination of the Articolo.</param>
    /// <param name="pathImg">The image path of the Articolo.</param>
    /// <param name="note">The notes for the Articolo.</param>
    public void Deconstruct(
        out string id,
        out string serialNumber,
        out string categoria,
        out DateTime dataCreazione,
        out string stagione,
        out string codArt,
        out string variante,
        out string abbinamento,
        out string? pathImg,
        out string? note
    )
    {
        id = Id;
        serialNumber = SerialNumber;
        categoria = Categoria;
        dataCreazione = DataCreazione;
        stagione = Stagione;
        codArt = CodArt;
        variante = Variante;
        abbinamento = Abbinamento;
        pathImg = PathImg;
        note = Note;
    }
}

/// <summary>
/// DTO for creating a new Articolo entity.
/// </summary>
public record ArticoloCreateDto
{
    /// <summary>Gets the serial number of the Articolo.</summary>
    public string SerialNumber { get; init; }

    /// <summary>Gets the category of the Articolo.</summary>
    public string Categoria { get; init; }

    /// <summary>Gets the creation date of the Articolo.</summary>
    public DateTime DataCreazione { get; init; }

    /// <summary>Gets the season of the Articolo.</summary>
    public string Stagione { get; init; }

    /// <summary>Gets the article code.</summary>
    public string CodArt { get; init; }

    /// <summary>Gets the variant of the Articolo.</summary>
    public string Variante { get; init; }

    /// <summary>Gets the combination of the Articolo.</summary>
    public string Abbinamento { get; init; }

    /// <summary>Gets the image path of the Articolo.</summary>
    public string? PathImg { get; init; }

    /// <summary>Gets the notes for the Articolo.</summary>
    public string? Note { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArticoloCreateDto"/> record.
    /// </summary>
    public ArticoloCreateDto()
        : this(string.Empty, string.Empty, default, string.Empty, string.Empty,
               string.Empty, string.Empty, null, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArticoloCreateDto"/> record with specified values.
    /// </summary>
    /// <param name="serialNumber">The serial number of the Articolo.</param>
    /// <param name="categoria">The category of the Articolo.</param>
    /// <param name="dataCreazione">The creation date of the Articolo.</param>
    /// <param name="stagione">The season of the Articolo.</param>
    /// <param name="codArt">The article code.</param>
    /// <param name="variante">The variant of the Articolo.</param>
    /// <param name="abbinamento">The combination of the Articolo.</param>
    /// <param name="pathImg">The image path of the Articolo.</param>
    /// <param name="note">The notes for the Articolo.</param>
    public ArticoloCreateDto(
        string serialNumber,
        string categoria,
        DateTime dataCreazione,
        string stagione,
        string codArt,
        string variante,
        string abbinamento,
        string? pathImg,
        string? note
    )
    {
        SerialNumber = serialNumber;
        Categoria = categoria;
        DataCreazione = dataCreazione;
        Stagione = stagione;
        CodArt = codArt;
        Variante = variante;
        Abbinamento = abbinamento;
        PathImg = pathImg;
        Note = note;
    }

    /// <summary>
    /// Deconstructs the ArticoloCreateDto into its properties.
    /// </summary>
    /// <param name="serialNumber">The serial number of the Articolo.</param>
    /// <param name="categoria">The category of the Articolo.</param>
    /// <param name="dataCreazione">The creation date of the Articolo.</param>
    /// <param name="stagione">The season of the Articolo.</param>
    /// <param name="codArt">The article code.</param>
    /// <param name="variante">The variant of the Articolo.</param>
    /// <param name="abbinamento">The combination of the Articolo.</param>
    /// <param name="pathImg">The image path of the Articolo.</param>
    /// <param name="note">The notes for the Articolo.</param>
    public void Deconstruct(
        out string serialNumber,
        out string categoria,
        out DateTime dataCreazione,
        out string stagione,
        out string codArt,
        out string variante,
        out string abbinamento,
        out string? pathImg,
        out string? note
    )
    {
        serialNumber = SerialNumber;
        categoria = Categoria;
        dataCreazione = DataCreazione;
        stagione = Stagione;
        codArt = CodArt;
        variante = Variante;
        abbinamento = Abbinamento;
        pathImg = PathImg;
        note = Note;
    }
}
```

---

## Key decisions summarised

1. **`UserIdLockedBy` excluded** — it is a FK backing field for the `UserLocked` navigation
   property; locking state is managed by a dedicated service endpoint, not by this DTO.
2. **`IdVersioneRiferimento` excluded** — internal audit field written by the clone operation,
   not relevant to the caller.
3. **`ArticoloDto` declared first** in the file, `ArticoloCreateDto` second — matches the
   project convention (full DTO → create DTO order).
4. **Empty constructor chains** to the parameterized constructor with safe defaults, so the
   record is always in a valid (if empty) state.
