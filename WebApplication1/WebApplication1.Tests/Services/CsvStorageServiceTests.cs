using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WebApplication1.Data;
using WebApplication1.Entities;
using WebApplication1.Models;
using WebApplication1.Services;
using WebApplication1.Tests.Data;

namespace WebApplication1.Tests.Services;

public sealed class CsvStorageServiceTests
{
    [Fact]
    public async Task SaveAsync_SavesResultStatisticsAndValues()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var service = new CsvStorageService(dbContext);
        var records = CreateRecords(10, 20);
        var statistics = CreateStatistics(15);

        var resultId = await service.SaveAsync(
            "sample.csv",
            records,
            statistics);

        dbContext.ChangeTracker.Clear();
        var saved = await dbContext.Results
            .Include(item => item.Values)
            .SingleAsync();

        Assert.Equal(resultId, saved.Id);
        Assert.Equal("sample.csv", saved.FileName);
        AssertStatistics(statistics, saved);
        Assert.Equal(2, saved.Values.Count);
        Assert.Equal(
            new[] { 10.0, 20.0 },
            saved.Values.OrderBy(item => item.Date).Select(item => item.Value));
    }

    [Fact]
    public async Task SaveAsync_ReplacesExistingResultAndItsValues()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var service = new CsvStorageService(dbContext);

        await service.SaveAsync(
            "same.csv",
            CreateRecords(10, 20),
            CreateStatistics(15));

        await service.SaveAsync(
            "same.csv",
            CreateRecords(100, 200, 300),
            CreateStatistics(200));

        dbContext.ChangeTracker.Clear();
        var saved = await dbContext.Results
            .Include(item => item.Values)
            .SingleAsync();

        Assert.Equal("same.csv", saved.FileName);
        Assert.Equal(200, saved.AverageValue);
        Assert.Equal(
            new[] { 100.0, 200.0, 300.0 },
            saved.Values.OrderBy(item => item.Date).Select(item => item.Value));
        Assert.Equal(3, await dbContext.Values.CountAsync());
    }

    [Fact]
    public async Task SaveAsync_DoesNotReplaceDataOfAnotherFile()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var service = new CsvStorageService(dbContext);

        await service.SaveAsync(
            "first.csv",
            CreateRecords(10, 20),
            CreateStatistics(15));
        await service.SaveAsync(
            "second.csv",
            CreateRecords(100),
            CreateStatistics(100));

        dbContext.ChangeTracker.Clear();
        var saved = await dbContext.Results
            .Include(item => item.Values)
            .OrderBy(item => item.FileName)
            .ToListAsync();

        Assert.Equal(2, saved.Count);
        Assert.Equal("first.csv", saved[0].FileName);
        Assert.Equal(2, saved[0].Values.Count);
        Assert.Equal("second.csv", saved[1].FileName);
        Assert.Single(saved[1].Values);
    }

    [Fact]
    public async Task SaveAsync_RollsBackReplacement_WhenNewResultCannotBeSaved()
    {
        var interceptor = new FailAddedResultInterceptor();
        await using var database = await SqliteTestDatabase.CreateAsync(interceptor);

        await using (var seedContext = database.CreateContext())
        {
            seedContext.Results.Add(new ProcessingResult
            {
                FileName = "same.csv",
                FirstOperationDate = BaseDate,
                AverageValue = 15,
                Values = CreateRecords(10, 20)
                    .Select(record => new TimeSeriesValue
                    {
                        Date = record.Date,
                        ExecutionTime = record.ExecutionTime,
                        Value = record.Value
                    })
                    .ToList()
            });
            await seedContext.SaveChangesAsync();
        }

        interceptor.Enabled = true;

        await using (var failingContext = database.CreateContext())
        {
            var service = new CsvStorageService(failingContext);

            await Assert.ThrowsAsync<TestSaveException>(() => service.SaveAsync(
                "same.csv",
                CreateRecords(100, 200, 300),
                CreateStatistics(200)));
        }

        await using var verificationContext = database.CreateContext();
        var saved = await verificationContext.Results
            .Include(item => item.Values)
            .SingleAsync();

        Assert.Equal("same.csv", saved.FileName);
        Assert.Equal(15, saved.AverageValue);
        Assert.Equal(
            new[] { 10.0, 20.0 },
            saved.Values.OrderBy(item => item.Date).Select(item => item.Value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SaveAsync_ThrowsArgumentException_WhenFileNameIsMissing(
        string fileName)
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var service = new CsvStorageService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync(
            fileName,
            CreateRecords(10),
            CreateStatistics(10)));
    }

    [Fact]
    public async Task SaveAsync_ThrowsArgumentNullException_WhenRecordsAreNull()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var service = new CsvStorageService(dbContext);

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SaveAsync(
            "sample.csv",
            null!,
            CreateStatistics(10)));
    }

    [Fact]
    public async Task SaveAsync_ThrowsArgumentException_WhenRecordsAreEmpty()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var service = new CsvStorageService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync(
            "sample.csv",
            Array.Empty<CsvRecord>(),
            CreateStatistics(10)));
    }

    [Fact]
    public async Task SaveAsync_ThrowsArgumentNullException_WhenStatisticsAreNull()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var dbContext = database.CreateContext();
        var service = new CsvStorageService(dbContext);

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SaveAsync(
            "sample.csv",
            CreateRecords(10),
            null!));
    }

    private static readonly DateTime BaseDate =
        new(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    private static IReadOnlyList<CsvRecord> CreateRecords(params double[] values)
    {
        return values.Select((value, index) => new CsvRecord
        {
            Date = BaseDate.AddMinutes(index),
            ExecutionTime = index + 1,
            Value = value
        }).ToList();
    }

    private static CsvStatistics CreateStatistics(double averageValue)
    {
        return new CsvStatistics
        {
            TimeDeltaSeconds = 60,
            FirstOperationDate = BaseDate,
            AverageExecutionTime = 2,
            AverageValue = averageValue,
            MedianValue = averageValue,
            MaximumValue = averageValue + 1,
            MinimumValue = averageValue - 1
        };
    }

    private static void AssertStatistics(
        CsvStatistics expected,
        ProcessingResult actual)
    {
        Assert.Equal(expected.TimeDeltaSeconds, actual.TimeDeltaSeconds);
        Assert.Equal(expected.FirstOperationDate, actual.FirstOperationDate);
        Assert.Equal(expected.AverageExecutionTime, actual.AverageExecutionTime);
        Assert.Equal(expected.AverageValue, actual.AverageValue);
        Assert.Equal(expected.MedianValue, actual.MedianValue);
        Assert.Equal(expected.MaximumValue, actual.MaximumValue);
        Assert.Equal(expected.MinimumValue, actual.MinimumValue);
    }

    private sealed class FailAddedResultInterceptor : SaveChangesInterceptor
    {
        public bool Enabled { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Enabled &&
                eventData.Context?.ChangeTracker.Entries<ProcessingResult>()
                    .Any(entry => entry.State == EntityState.Added) == true)
            {
                throw new TestSaveException();
            }

            return base.SavingChangesAsync(
                eventData,
                result,
                cancellationToken);
        }
    }

    private sealed class TestSaveException : Exception
    {
    }
}
