using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace PH.DbAppSettings.Tests.Helpers;

public static class DbContextHelper
{
    public static TestAppSettingsDbContext CreateInMemoryContext(SqliteConnection? connection = null)
    {
        var conn = connection ?? new SqliteConnection("Data Source=:memory:");
        if (conn.State != System.Data.ConnectionState.Open)
        {
            conn.Open();
        }

        var options = new DbContextOptionsBuilder<TestAppSettingsDbContext>()
            .UseSqlite(conn)
            .Options;

        var context = new TestAppSettingsDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static (TestAppSettingsDbContext context, SqliteConnection connection) CreateSharedInMemoryContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TestAppSettingsDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new TestAppSettingsDbContext(options);
        context.Database.EnsureCreated();
        return (context, connection);
    }
}
