namespace WebApplication1.Models;

public sealed class CsvStatistics
{
    public double TimeDeltaSeconds { get; init; }

    public DateTime FirstOperationDate { get; init; }

    public double AverageExecutionTime { get; init; }

    public double AverageValue { get; init; }

    public double MedianValue { get; init; }

    public double MaximumValue { get; init; }

    public double MinimumValue { get; init; }
}
