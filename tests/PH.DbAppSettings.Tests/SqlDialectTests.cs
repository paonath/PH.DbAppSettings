using PH.DbAppSettings.Storage;
using PH.DbAppSettings.Storage.Dialects;

namespace PH.DbAppSettings.Tests;

public class SqlDialectTests
{
    [Fact]
    public void SqlServerDialect_EscapesIdentifiersWithSquareBrackets()
    {
        // Arrange
        ISqlDialect dialect = new SqlServerDialect();

        // Act
        var escaped = dialect.EscapeIdentifier("Key");

        // Assert
        Assert.Equal("[Key]", escaped);
    }

    [Fact]
    public void PostgreSqlDialect_EscapesIdentifiersWithDoubleQuotes()
    {
        // Arrange
        ISqlDialect dialect = new PostgreSqlDialect();

        // Act
        var escaped = dialect.EscapeIdentifier("Key");

        // Assert
        Assert.Equal("\"Key\"", escaped);
    }

    [Fact]
    public void SqliteDialect_EscapesIdentifiersWithSquareBrackets()
    {
        // Arrange
        ISqlDialect dialect = new SqliteDialect();

        // Act
        var escaped = dialect.EscapeIdentifier("Key");

        // Assert
        Assert.Equal("[Key]", escaped);
    }

    [Fact]
    public void MySqlDialect_EscapesIdentifiersWithBackticks()
    {
        // Arrange
        ISqlDialect dialect = new MySqlDialect();

        // Act
        var escaped = dialect.EscapeIdentifier("Key");

        // Assert
        Assert.Equal("`Key`", escaped);
    }

    [Theory]
    [InlineData(typeof(SqlServerDialect), "SqlServer")]
    [InlineData(typeof(PostgreSqlDialect), "PostgreSql")]
    [InlineData(typeof(SqliteDialect), "Sqlite")]
    [InlineData(typeof(MySqlDialect), "MySql")]
    public void Dialects_ProvideValidSqlQueries(Type dialectType, string expectedName)
    {
        // Arrange
        var dialect = (ISqlDialect)Activator.CreateInstance(dialectType)!;

        // Act
        var selectAll = dialect.GetSelectAllSql("dbo", "AppSettings");
        var selectByKey = dialect.GetSelectByKeySql("dbo", "AppSettings");
        var upsert = dialect.GetUpsertSql("dbo", "AppSettings");
        var delete = dialect.GetDeleteSql("dbo", "AppSettings");
        var maxUpdated = dialect.GetMaxUpdatedAtSql("dbo", "AppSettings");
        var isEmpty = dialect.GetIsEmptySql("dbo", "AppSettings");
        var createTable = dialect.GetCreateTableSql("dbo", "AppSettings");

        // Assert
        Assert.Equal(expectedName, dialect.DialectName);
        Assert.False(string.IsNullOrWhiteSpace(selectAll));
        Assert.False(string.IsNullOrWhiteSpace(selectByKey));
        Assert.False(string.IsNullOrWhiteSpace(upsert));
        Assert.False(string.IsNullOrWhiteSpace(delete));
        Assert.False(string.IsNullOrWhiteSpace(maxUpdated));
        Assert.False(string.IsNullOrWhiteSpace(isEmpty));
        Assert.False(string.IsNullOrWhiteSpace(createTable));

        Assert.Contains("@Environment", selectAll);
        Assert.Contains("@Key", selectByKey);
        Assert.Contains("@Value", upsert);
        Assert.Contains("@Key", delete);
    }

    [Fact]
    public void SqliteDialect_UpsertSql_ContainsOnConflictClause()
    {
        // Arrange
        ISqlDialect dialect = new SqliteDialect();

        // Act
        var sql = dialect.GetUpsertSql("", "AppSettings");

        // Assert
        Assert.Contains("ON CONFLICT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DO UPDATE SET", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PostgreSqlDialect_UpsertSql_ContainsOnConflictClause()
    {
        // Arrange
        ISqlDialect dialect = new PostgreSqlDialect();

        // Act
        var sql = dialect.GetUpsertSql("public", "AppSettings");

        // Assert
        Assert.Contains("ON CONFLICT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DO UPDATE SET", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MySqlDialect_UpsertSql_ContainsOnDuplicateKeyUpdate()
    {
        // Arrange
        ISqlDialect dialect = new MySqlDialect();

        // Act
        var sql = dialect.GetUpsertSql("", "AppSettings");

        // Assert
        Assert.Contains("ON DUPLICATE KEY UPDATE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SqlServerDialect_UpsertSql_ContainsMergeOrUpdateInsert()
    {
        // Arrange
        ISqlDialect dialect = new SqlServerDialect();

        // Act
        var sql = dialect.GetUpsertSql("dbo", "AppSettings");

        // Assert
        Assert.Contains("MERGE", sql, StringComparison.OrdinalIgnoreCase);
    }
}
