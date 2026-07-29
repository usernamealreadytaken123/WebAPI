namespace WebApplication1.Contracts.Results;

public sealed class ResultResponse
{
    public long Id { get; init; }

    public string FileName { get; init; } = string.Empty;

    public double TimeDeltaSeconds { get; init; }

    public DateTime FirstOperationDate { get; init; }

    public double AverageExecutionTime { get; init; }

    public double AverageValue { get; init; }

    public double MedianValue { get; init; }

    public double MaximumValue { get; init; }

    public double MinimumValue { get; init; }
}
