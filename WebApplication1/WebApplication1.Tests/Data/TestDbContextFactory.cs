using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Tests.Data;

internal static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"Tests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }
}
