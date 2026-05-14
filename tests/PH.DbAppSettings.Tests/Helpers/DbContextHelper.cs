using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PH.DbAppSettings.Data;

namespace PH.DbAppSettings.Tests.Helpers;

public static class DbContextHelper
{
    public static AppSettingsDbContext CreateInMemoryContext(SqliteConnection? connection = null)
    {
        var conn = connection ?? new SqliteConnection("Data Source=:memory:");
        conn.Open();

        var options = new DbContextOptionsBuilder<AppSettingsDbContext>()
            .UseSqlite(conn)
            .Options;

        var context = new AppSettingsDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static (AppSettingsDbContext context, SqliteConnection connection) CreateSharedInMemoryContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppSettingsDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppSettingsDbContext(options);
        context.Database.EnsureCreated();
        return (context, connection);
    }
}
