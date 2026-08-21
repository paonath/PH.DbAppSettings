using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PH.DbAppSettings.Data;

public sealed class AppSettingEntryConfiguration : IEntityTypeConfiguration<AppSettingEntry>
{
    private readonly string _tableName;
    private readonly string _schemaName;

    public AppSettingEntryConfiguration(string tableName = "AppSettings", string schemaName = "dbo")
    {
        _tableName = tableName;
        _schemaName = schemaName;
    }

    public void Configure(EntityTypeBuilder<AppSettingEntry> builder)
    {
        builder.ToTable(_tableName, _schemaName);

        builder.HasKey(e => new { e.Key, e.Environment });

        builder.Property(e => e.Key)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(e => e.Environment)
            .HasMaxLength(64)
            .IsRequired()
            .HasDefaultValue("Production");

        builder.Property(e => e.Value)
            .HasMaxLength(4000)
            .IsRequired(false);

        builder.Property(e => e.IsEncrypted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.UpdatedAt)
            .HasConversion(
                v => v.HasValue ? v.Value.ToString("O") : null,
                v => v != null ? DateTimeOffset.Parse(v) : null)
            .IsRequired(false);
    }
}
