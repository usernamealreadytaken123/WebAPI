namespace WebApplication1.Entities;

public sealed class TimeSeriesValue
{
    public long Id { get; set; }

    public long ProcessingResultId { get; set; }

    public DateTime Date { get; set; }

    public double ExecutionTime { get; set; }

    public double Value { get; set; }

    public ProcessingResult ProcessingResult { get; set; } = null!;
}
