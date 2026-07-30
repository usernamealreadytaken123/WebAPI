using WebApplication1.Data;
using WebApplication1.Entities;
using WebApplication1.Services;
using WebApplication1.Tests.Data;

namespace WebApplication1.Tests.Services;

public sealed class ValueQueryServiceTests
{
    [Fact]
    public async Task GetLatestAsync_ReturnsTenNewestValues_InDescendingDateOrder()
    {
        await using var dbContext = await CreatePopulatedContextAsync();
        var service = new ValueQueryService(dbContext);

        var values = await service.GetLatestAsync("target.csv");

        Assert.Equal(10, values.Count);
        Assert.Equal(
            Enumerable.Range(2, 10).Reverse(),
            values.Select(item => item.Date.Minute));
        Assert.Equal(
            Enumerable.Range(2, 10).Reverse().Select(minute => minute * 10.0),
            values.Select(item => item.Value));
    }

    [Fact]
    public async Task GetLatestAsync_TrimsFileName()
    {
        await using var dbContext = await CreatePopulatedContextAsync();
        var service = new ValueQueryService(dbContext);

        var values = await service.GetLatestAsync("  target.csv  ");

        Assert.Equal(10, values.Count);
    }

    [Fact]
    public async Task GetLatestAsync_DoesNotReturnValuesFromAnotherFile()
    {
        await using var dbContext = await CreatePopulatedContextAsync();
        var service = new ValueQueryService(dbContext);

        var values = await service.GetLatestAsync("other.csv");

        var value = Assert.Single(values);
        Assert.Equal(999, value.Value);
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsEmptyList_WhenFileDoesNotExist()
    {
        await using var dbContext = await CreatePopulatedContextAsync();
        var service = new ValueQueryService(dbContext);

        var values = await service.GetLatestAsync("unknown.csv");

        Assert.Empty(values);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetLatestAsync_ThrowsArgumentException_WhenFileNameIsMissing(
        string fileName)
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new ValueQueryService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetLatestAsync(fileName));
    }

    [Fact]
    public async Task GetLatestAsync_ThrowsArgumentException_WhenFileNameIsTooLong()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new ValueQueryService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetLatestAsync(new string('a', 256)));
    }

    [Fact]
    public async Task GetLatestAsync_UsesDescendingId_WhenDatesAreEqual()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var date = UtcDate(0);
        var result = new ProcessingResult
        {
            Id = 1,
            FileName = "ties.csv",
            FirstOperationDate = date,
            Values = new List<TimeSeriesValue>
            {
                CreateValue(10, date, 10),
                CreateValue(20, date, 20)
            }
        };
        dbContext.Results.Add(result);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var service = new ValueQueryService(dbContext);

        var values = await service.GetLatestAsync("ties.csv");

        Assert.Equal(new long[] { 20, 10 }, values.Select(item => item.Id));
    }

    private static async Task<AppDbContext> CreatePopulatedContextAsync()
    {
        var dbContext = TestDbContextFactory.Create();
        var target = new ProcessingResult
        {
            Id = 1,
            FileName = "target.csv",
            FirstOperationDate = UtcDate(0)
        };

        var insertionOrder = new[] { 5, 0, 11, 2, 9, 1, 7, 3, 10, 4, 8, 6 };
        foreach (var minute in insertionOrder)
        {
            target.Values.Add(CreateValue(
                id: minute + 1,
                date: UtcDate(minute),
                value: minute * 10));
        }

        var other = new ProcessingResult
        {
            Id = 2,
            FileName = "other.csv",
            FirstOperationDate = UtcDate(59),
            Values = new List<TimeSeriesValue>
            {
                CreateValue(100, UtcDate(59), 999)
            }
        };

        dbContext.Results.AddRange(target, other);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        return dbContext;
    }

    private static TimeSeriesValue CreateValue(
        long id,
        DateTime date,
        double value)
    {
        return new TimeSeriesValue
        {
            Id = id,
            Date = date,
            ExecutionTime = value / 10,
            Value = value
        };
    }

    private static DateTime UtcDate(int minute)
    {
        return new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc)
            .AddMinutes(minute);
    }
}
