using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebApplication1.Controllers;
using WebApplication1.Data;

namespace WebApplication1.Tests.Integration;

internal sealed class WebApiFactory : WebApplicationFactory<FilesController>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            "ConnectionStrings:PostgreSql",
            "Host=localhost;Database=unused;Username=unused;Password=unused");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            services.AddSingleton(connection);
            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
                options.UseSqlite(
                    serviceProvider.GetRequiredService<SqliteConnection>()));
        });
    }

    public async Task<HttpClient> CreateInitializedClientAsync()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        return client;
    }
}
