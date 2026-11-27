using CoworkingAPI.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CoworkingAPI.Tests.Infrastructure;

/// <summary>
/// In-memory SQLite database shared by every context created from it.
/// SQLite (instead of the EF InMemory provider) is used on purpose so that relational
/// behaviour is exercised: query filters, unique filtered indexes, foreign keys,
/// transactions and ExecuteUpdate.
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection connection;

    public DbContextOptions<AppDbContext> Options { get; }

    public SqliteTestDatabase()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        Options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public AppDbContext CreateContext() => new TestAppDbContext(Options);

    public void Dispose() => connection.Dispose();
}

/// <summary>
/// SQLite cannot natively compare/order DateTimeOffset columns, the production database (SQL Server) can.
/// Storing them as binary keeps the production model intact while allowing the same LINQ to run in tests.
/// </summary>
public class TestAppDbContext : AppDbContext
{
    public TestAppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }
}
