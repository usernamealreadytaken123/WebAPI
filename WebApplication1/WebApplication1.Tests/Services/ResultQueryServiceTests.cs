using WebApplication1.Contracts.Results;
using WebApplication1.Data;
using WebApplication1.Entities;
using WebApplication1.Services;
using WebApplication1.Tests.Data;

namespace WebApplication1.Tests.Services;

public sealed class ResultQueryServiceTests
{
    [Fact]
    public async Task GetAsync_ReturnsAllResults_OrderedByDescendingId()
    {
        await using var dbContext = await CreatePopulatedContextAsync();
        var service = new ResultQueryService(dbContext);

        var results = await service.GetAsync(new ResultFilterRequest());

        Assert.Equal(new long[] { 3, 2, 1 }, results.Select(item => item.Id));
    }

    [Fact]
    public async Task GetAsync_FiltersByTrimmedFileName()
    {
        await using var dbContext = await CreatePopulatedContextAsync();
        var service = new ResultQueryService(dbContext);

        var results = await service.GetAsync(new ResultFilterRequest
        {
            FileName = "  beta.csv  "
        });

        var result = Assert.Single(results);
        Assert.Equal("beta.csv", result.FileName);
    }

    [Fact]
    public async Task GetAsync_FiltersByFirstOperationDateRange()
    {
        await using var dbContext = await CreatePopulatedContextAsync();
        var service = new ResultQueryService(dbContext);

        var results = await service.GetAsync(new ResultFilterRequest
        {
            FirstOperationDateFrom = UtcDate(2),
            FirstOperationDateTo = UtcDate(3)
        });

        Assert.Equal(
            new[] { "gamma.csv", "beta.csv" },
            results.Select(item => item.FileName));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public async Task GetAsync_ConvertsDateFilterToUtc(DateTimeKind dateTimeKind)
    {
        await using var dbContext = await CreatePopulatedContextAsync();
        var service = new ResultQueryService(dbContext);
        var utcDate = UtcDate(2);
        var filterDate = dateTimeKind == DateTimeKind.Local
            ? utcDate.ToLocalTime()
            : DateTime.SpecifyKind(utcDate, DateTimeKind.Unspecified);

        var results = await service.GetAsync(new ResultFilterRequest
        {
            FirstOperationDateFrom = filterDate,
            FirstOperationDateTo = filterDate
        });

        var result = Assert.Single(results);
        Assert.Equal("beta.csv", result.FileName);
    }

    [Fact]
    public async Task GetAsync_FiltersByAverageValueRange()
    {
        await using var dbContext = await CreatePopulatedContextAsync();
        var service = new ResultQueryService(dbContext);

        var results = await service.GetAsync(new ResultFilterRequest
        {
            AverageValueFrom = 15,
            AverageValueTo = 25
        });

        var result = Assert.Single(results);
        Assert.Equal("beta.csv", result.FileName);
    }

    [Fact]
    public async Task GetAsync_FiltersByAverageExecutionTimeRange()
    {
        await using var dbContext = await CreatePopulatedContextAsync();
        var service = new ResultQueryService(dbContext);

        var results = await service.GetAsync(new ResultFilterRequest
        {
            AverageExecutionTimeFrom = 2,
            AverageExecutionTimeTo = 3
        });

        Assert.Equal(
            new[] { "gamma.csv", "beta.csv" },
            results.Select(item => item.FileName));
    }

    [Fact]
    public async Task GetAsync_ReturnsEmptyList_WhenNothingMatches()
    {
        await using var dbContext = await CreatePopulatedContextAsync();
        var service = new ResultQueryService(dbContext);

        var results = await service.GetAsync(new ResultFilterRequest
        {
            FileName = "unknown.csv"
        });

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetAsync_ThrowsArgumentNullException_WhenFilterIsNull()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new ResultQueryService(dbContext);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.GetAsync(null!));
    }

    [Fact]
    public async Task GetAsync_ThrowsArgumentException_WhenDateRangeIsReversed()
    {
        await AssertInvalidFilterAsync(new ResultFilterRequest
        {
            FirstOperationDateFrom = UtcDate(3),
            FirstOperationDateTo = UtcDate(2)
        });
    }

    [Fact]
    public async Task GetAsync_ThrowsArgumentException_WhenAverageValueRangeIsReversed()
    {
        await AssertInvalidFilterAsync(new ResultFilterRequest
        {
            AverageValueFrom = 20,
            AverageValueTo = 10
        });
    }

    [Fact]
    public async Task GetAsync_ThrowsArgumentException_WhenExecutionTimeRangeIsReversed()
    {
        await AssertInvalidFilterAsync(new ResultFilterRequest
        {
            AverageExecutionTimeFrom = 2,
            AverageExecutionTimeTo = 1
        });
    }

    [Fact]
    public async Task GetAsync_ThrowsArgumentException_WhenNumericBoundaryIsNotFinite()
    {
        await AssertInvalidFilterAsync(new ResultFilterRequest
        {
            AverageValueFrom = double.NaN
        });
    }

    [Fact]
    public async Task GetAsync_ThrowsArgumentException_WhenFileNameIsTooLong()
    {
        await AssertInvalidFilterAsync(new ResultFilterRequest
        {
            FileName = new string('a', 256)
        });
    }

    private static async Task<AppDbContext> CreatePopulatedContextAsync()
    {
        var dbContext = TestDbContextFactory.Create();
        dbContext.Results.AddRange(
            CreateResult(1, "alpha.csv", UtcDate(1), 1, 10),
            CreateResult(2, "beta.csv", UtcDate(2), 2, 20),
            CreateResult(3, "gamma.csv", UtcDate(3), 3, 30));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        return dbContext;
    }

    private static ProcessingResult CreateResult(
        long id,
        string fileName,
        DateTime firstOperationDate,
        double averageExecutionTime,
        double averageValue)
    {
        return new ProcessingResult
        {
            Id = id,
            FileName = fileName,
            TimeDeltaSeconds = 60,
            FirstOperationDate = firstOperationDate,
            AverageExecutionTime = averageExecutionTime,
            AverageValue = averageValue,
            MedianValue = averageValue,
            MaximumValue = averageValue + 1,
            MinimumValue = averageValue - 1
        };
    }

    private static DateTime UtcDate(int day)
    {
        return new DateTime(2024, 1, day, 10, 0, 0, DateTimeKind.Utc);
    }

    private static async Task AssertInvalidFilterAsync(
        ResultFilterRequest filter)
    {
        await using var dbContext = TestDbContextFactory.Create();
        var service = new ResultQueryService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetAsync(filter));
    }
}
