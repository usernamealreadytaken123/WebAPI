using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Tests.Services;

public sealed class CsvStatisticsCalculatorTests
{
    private readonly CsvStatisticsCalculator _calculator = new();

    [Fact]
    public void Calculate_ReturnsExpectedStatistics_ForUnsortedRecords()
    {
        var records = new[]
        {
            CreateRecord("2026-01-10T10:01:00Z", 3.6, 30.5),
            CreateRecord("2026-01-10T10:00:00Z", 1.2, 10.5),
            CreateRecord("2026-01-10T10:00:30Z", 2.4, 20.5)
        };

        var result = _calculator.Calculate(records);

        Assert.Equal(60, result.TimeDeltaSeconds);
        Assert.Equal(
            DateTime.Parse(
                "2026-01-10T10:00:00Z",
                null,
                System.Globalization.DateTimeStyles.RoundtripKind),
            result.FirstOperationDate);
        Assert.Equal(2.4, result.AverageExecutionTime, 10);
        Assert.Equal(20.5, result.AverageValue, 10);
        Assert.Equal(20.5, result.MedianValue);
        Assert.Equal(30.5, result.MaximumValue);
        Assert.Equal(10.5, result.MinimumValue);
    }

    [Fact]
    public void Calculate_UsesAverageOfMiddleValues_ForEvenRecordCount()
    {
        var records = new[]
        {
            CreateRecord("2026-01-10T10:00:00Z", 1, 40),
            CreateRecord("2026-01-10T10:00:10Z", 2, 10),
            CreateRecord("2026-01-10T10:00:20Z", 3, 30),
            CreateRecord("2026-01-10T10:00:30Z", 4, 20)
        };

        var result = _calculator.Calculate(records);

        Assert.Equal(25, result.MedianValue);
    }

    [Fact]
    public void Calculate_ReturnsZeroDelta_ForSingleRecord()
    {
        var record = CreateRecord("2026-01-10T10:00:00Z", 1.5, 15);

        var result = _calculator.Calculate(new[] { record });

        Assert.Equal(0, result.TimeDeltaSeconds);
        Assert.Equal(record.Date, result.FirstOperationDate);
        Assert.Equal(1.5, result.AverageExecutionTime);
        Assert.Equal(15, result.AverageValue);
        Assert.Equal(15, result.MedianValue);
        Assert.Equal(15, result.MaximumValue);
        Assert.Equal(15, result.MinimumValue);
    }

    [Fact]
    public void Calculate_ThrowsArgumentException_WhenRecordsAreEmpty()
    {
        var action = () => _calculator.Calculate(Array.Empty<CsvRecord>());

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Calculate_ThrowsArgumentNullException_WhenRecordsAreNull()
    {
        var action = () => _calculator.Calculate(null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    private static CsvRecord CreateRecord(
        string date,
        double executionTime,
        double value)
    {
        return new CsvRecord
        {
            Date = DateTime.Parse(
                date,
                null,
                System.Globalization.DateTimeStyles.RoundtripKind),
            ExecutionTime = executionTime,
            Value = value
        };
    }
}
