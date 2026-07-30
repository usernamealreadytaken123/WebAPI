using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WebApplication1.Data;

namespace WebApplication1.Tests.Data;

internal sealed class SqliteTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    private SqliteTestDatabase(
        SqliteConnection connection,
        DbContextOptions<AppDbContext> options)
    {
        _connection = connection;
        _options = options;
    }

    public static async Task<SqliteTestDatabase> CreateAsync(
        params IInterceptor[] interceptors)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection);

        if (interceptors.Length > 0)
        {
            optionsBuilder.AddInterceptors(interceptors);
        }

        var database = new SqliteTestDatabase(
            connection,
            optionsBuilder.Options);

        await using var dbContext = database.CreateContext();
        await dbContext.Database.EnsureCreatedAsync();

        return database;
    }

    public AppDbContext CreateContext()
    {
        return new AppDbContext(_options);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
