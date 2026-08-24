using System.Data;
using System.Data.Common;
using Dapper;

namespace PH.DbAppSettings.Storage;

public sealed class DapperStorageEngine : IDbAppSettingsStorageEngine
{
    private readonly Func<DbConnection> _connectionFactory;
    private readonly ISqlDialect _dialect;
    private readonly string _schema;
    private readonly string _table;

    public DapperStorageEngine(
        Func<DbConnection> connectionFactory,
        ISqlDialect dialect,
        string schema = "dbo",
        string table = "AppSettings")
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
        _schema = schema;
        _table = string.IsNullOrWhiteSpace(table) ? "AppSettings" : table;
    }

    public async Task EnsureSchemaCreatedAsync(CancellationToken ct = default)
    {
        await using var connection = _connectionFactory();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        var sql = _dialect.GetCreateTableSql(_schema, _table);
        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<AppSettingRecord>> GetAllAsync(string environment, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        var sql = _dialect.GetSelectAllSql(_schema, _table);
        var results = await connection.QueryAsync<AppSettingRecord>(
            new CommandDefinition(sql, new { Environment = environment }, cancellationToken: ct));

        return results.AsList();
    }

    public async Task<AppSettingRecord?> GetByKeyAsync(string key, string environment, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        var sql = _dialect.GetSelectByKeySql(_schema, _table);
        return await connection.QuerySingleOrDefaultAsync<AppSettingRecord>(
            new CommandDefinition(sql, new { Key = key, Environment = environment }, cancellationToken: ct));
    }

    public async Task UpsertAsync(AppSettingRecord entry, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        var sql = _dialect.GetUpsertSql(_schema, _table);
        var parameters = new
        {
            entry.Key,
            entry.Environment,
            entry.Value,
            entry.IsEncrypted,
            UpdatedAt = (entry.UpdatedAt ?? DateTimeOffset.UtcNow).ToString("O")
        };

        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: ct));
    }

    public async Task UpsertBatchAsync(IEnumerable<AppSettingRecord> entries, CancellationToken ct = default)
    {
        var entryList = entries.AsList();
        if (entryList.Count == 0) return;

        await using var connection = _connectionFactory();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        await using var transaction = await connection.BeginTransactionAsync(ct);
        var sql = _dialect.GetUpsertSql(_schema, _table);

        foreach (var entry in entryList)
        {
            var parameters = new
            {
                entry.Key,
                entry.Environment,
                entry.Value,
                entry.IsEncrypted,
                UpdatedAt = (entry.UpdatedAt ?? DateTimeOffset.UtcNow).ToString("O")
            };

            await connection.ExecuteAsync(new CommandDefinition(sql, parameters, transaction: transaction, cancellationToken: ct));
        }

        await transaction.CommitAsync(ct);
    }

    public async Task<bool> DeleteAsync(string key, string environment, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        var sql = _dialect.GetDeleteSql(_schema, _table);
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Key = key, Environment = environment }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<bool> IsEmptyAsync(string environment, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        var sql = _dialect.GetIsEmptySql(_schema, _table);
        var result = await connection.ExecuteScalarAsync<int?>(
            new CommandDefinition(sql, new { Environment = environment }, cancellationToken: ct));

        return result is null;
    }

    public async Task<DateTimeOffset?> GetLastModifiedTimestampAsync(string environment, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        var sql = _dialect.GetMaxUpdatedAtSql(_schema, _table);
        var result = await connection.ExecuteScalarAsync<object?>(
            new CommandDefinition(sql, new { Environment = environment }, cancellationToken: ct));

        if (result is null || result is DBNull)
        {
            return null;
        }

        if (result is DateTimeOffset dto)
        {
            return dto;
        }

        if (result is DateTime dt)
        {
            return new DateTimeOffset(dt, TimeSpan.Zero);
        }

        if (DateTimeOffset.TryParse(result.ToString(), out var parsedDto))
        {
            return parsedDto;
        }

        return null;
    }

    static DapperStorageEngine()
    {
        SqlMapper.AddTypeHandler(new DateTimeOffsetTypeHandler());
        SqlMapper.AddTypeHandler(new NullableDateTimeOffsetTypeHandler());
    }

    private sealed class DateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
        {
            parameter.Value = value.ToString("O");
        }

        public override DateTimeOffset Parse(object value)
        {
            if (value is DateTimeOffset dto) return dto;
            if (value is DateTime dt) return new DateTimeOffset(dt, TimeSpan.Zero);
            if (value is string str && DateTimeOffset.TryParse(str, out var parsed)) return parsed;
            if (value is not null && DateTimeOffset.TryParse(value.ToString(), out var fallbackParsed)) return fallbackParsed;
            return default;
        }
    }

    private sealed class NullableDateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset?>
    {
        public override void SetValue(IDbDataParameter parameter, DateTimeOffset? value)
        {
            if (value.HasValue)
                parameter.Value = value.Value.ToString("O");
            else
                parameter.Value = DBNull.Value;
        }

        public override DateTimeOffset? Parse(object value)
        {
            if (value is null || value is DBNull) return null;
            if (value is DateTimeOffset dto) return dto;
            if (value is DateTime dt) return new DateTimeOffset(dt, TimeSpan.Zero);
            if (value is string str && DateTimeOffset.TryParse(str, out var parsed)) return parsed;
            if (DateTimeOffset.TryParse(value.ToString(), out var fallbackParsed)) return fallbackParsed;
            return null;
        }
    }
}
