namespace WebApplication1.Entities;

public sealed class ProcessingResult
{
    public long Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public double TimeDeltaSeconds { get; set; }

    public DateTime FirstOperationDate { get; set; }

    public double AverageExecutionTime { get; set; }

    public double AverageValue { get; set; }

    public double MedianValue { get; set; }

    public double MaximumValue { get; set; }

    public double MinimumValue { get; set; }

    public ICollection<TimeSeriesValue> Values { get; set; } =
        new List<TimeSeriesValue>();
}
