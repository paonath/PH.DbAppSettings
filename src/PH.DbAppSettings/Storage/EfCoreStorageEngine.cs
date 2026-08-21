using Microsoft.EntityFrameworkCore;
using PH.DbAppSettings.Data;

namespace PH.DbAppSettings.Storage;

public sealed class EfCoreStorageEngine : IDbAppSettingsStorageEngine
{
    private readonly Func<AppSettingsDbContext> _contextFactory;
    private readonly bool _ownsContext;

    public EfCoreStorageEngine(Func<AppSettingsDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _ownsContext = true;
    }

    public EfCoreStorageEngine(AppSettingsDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _contextFactory = () => dbContext;
        _ownsContext = false;
    }

    public async Task EnsureSchemaCreatedAsync(CancellationToken ct = default)
    {
        var context = _contextFactory();
        try
        {
            await context.Database.EnsureCreatedAsync(ct);
        }
        finally
        {
            if (_ownsContext)
            {
                await context.DisposeAsync();
            }
        }
    }

    public async Task<IReadOnlyList<AppSettingRecord>> GetAllAsync(string environment, CancellationToken ct = default)
    {
        var context = _contextFactory();
        try
        {
            var entries = await context.AppSettings
                .AsNoTracking()
                .Where(e => e.Environment == environment)
                .ToListAsync(ct);

            return entries.Select(e => new AppSettingRecord
            {
                Key = e.Key,
                Environment = e.Environment,
                Value = e.Value,
                IsEncrypted = e.IsEncrypted,
                UpdatedAt = e.UpdatedAt
            }).ToList();
        }
        finally
        {
            if (_ownsContext)
            {
                await context.DisposeAsync();
            }
        }
    }

    public async Task<AppSettingRecord?> GetByKeyAsync(string key, string environment, CancellationToken ct = default)
    {
        var context = _contextFactory();
        try
        {
            var entry = await context.AppSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Key == key && e.Environment == environment, ct);

            if (entry is null) return null;

            return new AppSettingRecord
            {
                Key = entry.Key,
                Environment = entry.Environment,
                Value = entry.Value,
                IsEncrypted = entry.IsEncrypted,
                UpdatedAt = entry.UpdatedAt
            };
        }
        finally
        {
            if (_ownsContext)
            {
                await context.DisposeAsync();
            }
        }
    }

    public async Task UpsertAsync(AppSettingRecord entry, CancellationToken ct = default)
    {
        var context = _contextFactory();
        try
        {
            var existing = await context.AppSettings
                .FirstOrDefaultAsync(e => e.Key == entry.Key && e.Environment == entry.Environment, ct);

            var timestamp = entry.UpdatedAt ?? DateTimeOffset.UtcNow;

            if (existing is not null)
            {
                existing.Value = entry.Value;
                existing.IsEncrypted = entry.IsEncrypted;
                existing.UpdatedAt = timestamp;
            }
            else
            {
                context.AppSettings.Add(new AppSettingEntry
                {
                    Key = entry.Key,
                    Environment = entry.Environment,
                    Value = entry.Value,
                    IsEncrypted = entry.IsEncrypted,
                    UpdatedAt = timestamp
                });
            }

            await context.SaveChangesAsync(ct);
        }
        finally
        {
            if (_ownsContext)
            {
                await context.DisposeAsync();
            }
        }
    }

    public async Task UpsertBatchAsync(IEnumerable<AppSettingRecord> entries, CancellationToken ct = default)
    {
        var entryList = entries.ToList();
        if (entryList.Count == 0) return;

        var context = _contextFactory();
        try
        {
            foreach (var entry in entryList)
            {
                var existing = await context.AppSettings
                    .FirstOrDefaultAsync(e => e.Key == entry.Key && e.Environment == entry.Environment, ct);

                var timestamp = entry.UpdatedAt ?? DateTimeOffset.UtcNow;

                if (existing is not null)
                {
                    existing.Value = entry.Value;
                    existing.IsEncrypted = entry.IsEncrypted;
                    existing.UpdatedAt = timestamp;
                }
                else
                {
                    context.AppSettings.Add(new AppSettingEntry
                    {
                        Key = entry.Key,
                        Environment = entry.Environment,
                        Value = entry.Value,
                        IsEncrypted = entry.IsEncrypted,
                        UpdatedAt = timestamp
                    });
                }
            }

            await context.SaveChangesAsync(ct);
        }
        finally
        {
            if (_ownsContext)
            {
                await context.DisposeAsync();
            }
        }
    }

    public async Task<bool> DeleteAsync(string key, string environment, CancellationToken ct = default)
    {
        var context = _contextFactory();
        try
        {
            var existing = await context.AppSettings
                .FirstOrDefaultAsync(e => e.Key == key && e.Environment == environment, ct);

            if (existing is not null)
            {
                context.AppSettings.Remove(existing);
                await context.SaveChangesAsync(ct);
                return true;
            }

            return false;
        }
        finally
        {
            if (_ownsContext)
            {
                await context.DisposeAsync();
            }
        }
    }

    public async Task<bool> IsEmptyAsync(string environment, CancellationToken ct = default)
    {
        var context = _contextFactory();
        try
        {
            return !await context.AppSettings.AnyAsync(e => e.Environment == environment, ct);
        }
        finally
        {
            if (_ownsContext)
            {
                await context.DisposeAsync();
            }
        }
    }

    public async Task<DateTimeOffset?> GetLastModifiedTimestampAsync(string environment, CancellationToken ct = default)
    {
        var context = _contextFactory();
        try
        {
            return await context.AppSettings
                .AsNoTracking()
                .Where(e => e.Environment == environment && e.UpdatedAt != null)
                .OrderByDescending(e => e.UpdatedAt)
                .Select(e => e.UpdatedAt)
                .FirstOrDefaultAsync(ct);
        }
        finally
        {
            if (_ownsContext)
            {
                await context.DisposeAsync();
            }
        }
    }
}
