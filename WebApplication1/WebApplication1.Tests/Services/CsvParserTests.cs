using System.Text;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Tests.Services;

public sealed class CsvParserTests
{
    private const string Header = "Date;ExecutionTime;Value";
    private const string ValidRow = "2024-01-10T10:00:00Z;1.2;10.5";

    private readonly CsvParser _parser = new();

    [Fact]
    public async Task ParseAsync_ReturnsRecords_ForValidCsv()
    {
        var csv = BuildCsv(
            "2024-01-10T10:00:00Z;1.2;10.5",
            "2024-01-10T10:00:30Z;2.4;20.5");

        var records = await ParseAsync(csv);

        Assert.Collection(
            records,
            first => AssertRecord(
                first,
                new DateTime(2024, 1, 10, 10, 0, 0, DateTimeKind.Utc),
                1.2,
                10.5),
            second => AssertRecord(
                second,
                new DateTime(2024, 1, 10, 10, 0, 30, DateTimeKind.Utc),
                2.4,
                20.5));
    }

    [Fact]
    public async Task ParseAsync_LeavesInputStreamOpen()
    {
        await using var stream = CreateStream(BuildCsv(ValidRow));

        await _parser.ParseAsync(stream);

        Assert.True(stream.CanRead);
    }

    [Fact]
    public async Task ParseAsync_ThrowsArgumentNullException_WhenStreamIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _parser.ParseAsync(null!));
    }

    [Fact]
    public async Task ParseAsync_ThrowsInvalidDataException_WhenHeaderIsWrong()
    {
        var csv = $"Timestamp;Duration;Measurement\n{ValidRow}";

        await AssertInvalidAsync(csv);
    }

    [Fact]
    public async Task ParseAsync_ThrowsInvalidDataException_WhenDataRowsAreMissing()
    {
        await AssertInvalidAsync(Header);
    }

    [Theory]
    [InlineData("2024-01-10T10:00:00Z;1.2")]
    [InlineData("2024-01-10T10:00:00Z;1.2;10.5;extra")]
    public async Task ParseAsync_ThrowsInvalidDataException_WhenColumnCountIsWrong(
        string row)
    {
        await AssertInvalidAsync(BuildCsv(row));
    }

    [Fact]
    public async Task ParseAsync_ThrowsInvalidDataException_WhenDateFormatIsWrong()
    {
        await AssertInvalidAsync(BuildCsv("wrong-date;1.2;10.5"));
    }

    [Theory]
    [InlineData("1999-12-31T23:59:59Z")]
    [InlineData("2099-01-01T00:00:00Z")]
    public async Task ParseAsync_ThrowsInvalidDataException_WhenDateIsOutsideRange(
        string date)
    {
        await AssertInvalidAsync(BuildCsv($"{date};1.2;10.5"));
    }

    [Fact]
    public async Task ParseAsync_ThrowsInvalidDataException_WhenExecutionTimeFormatIsWrong()
    {
        await AssertInvalidAsync(
            BuildCsv("2024-01-10T10:00:00Z;wrong;10.5"));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    public async Task ParseAsync_ThrowsInvalidDataException_WhenExecutionTimeIsInvalid(
        string executionTime)
    {
        await AssertInvalidAsync(
            BuildCsv($"2024-01-10T10:00:00Z;{executionTime};10.5"));
    }

    [Fact]
    public async Task ParseAsync_ThrowsInvalidDataException_WhenValueFormatIsWrong()
    {
        await AssertInvalidAsync(
            BuildCsv("2024-01-10T10:00:00Z;1.2;wrong"));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    public async Task ParseAsync_ThrowsInvalidDataException_WhenValueIsInvalid(
        string value)
    {
        await AssertInvalidAsync(
            BuildCsv($"2024-01-10T10:00:00Z;1.2;{value}"));
    }

    [Fact]
    public async Task ParseAsync_AcceptsMaximumNumberOfDataRows()
    {
        var csv = BuildCsvWithRowCount(10_000);

        var records = await ParseAsync(csv);

        Assert.Equal(10_000, records.Count);
    }

    [Fact]
    public async Task ParseAsync_ThrowsInvalidDataException_WhenRowLimitIsExceeded()
    {
        var csv = BuildCsvWithRowCount(10_001);

        await AssertInvalidAsync(csv);
    }

    private async Task<IReadOnlyList<CsvRecord>> ParseAsync(string csv)
    {
        await using var stream = CreateStream(csv);
        return await _parser.ParseAsync(stream);
    }

    private async Task AssertInvalidAsync(string csv)
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => ParseAsync(csv));
    }

    private static MemoryStream CreateStream(string csv)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(csv));
    }

    private static string BuildCsv(params string[] rows)
    {
        return string.Join('\n', new[] { Header }.Concat(rows));
    }

    private static string BuildCsvWithRowCount(int rowCount)
    {
        var builder = new StringBuilder(Header);

        for (var index = 0; index < rowCount; index++)
        {
            builder.Append('\n');
            builder.Append(ValidRow);
        }

        return builder.ToString();
    }

    private static void AssertRecord(
        CsvRecord record,
        DateTime expectedDate,
        double expectedExecutionTime,
        double expectedValue)
    {
        Assert.Equal(expectedDate, record.Date);
        Assert.Equal(expectedExecutionTime, record.ExecutionTime);
        Assert.Equal(expectedValue, record.Value);
    }
}
