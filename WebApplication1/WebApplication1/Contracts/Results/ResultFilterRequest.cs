namespace WebApplication1.Contracts.Results;

public sealed class ResultFilterRequest
{
    public string? FileName { get; init; }

    public DateTime? FirstOperationDateFrom { get; init; }

    public DateTime? FirstOperationDateTo { get; init; }

    public double? AverageValueFrom { get; init; }

    public double? AverageValueTo { get; init; }

    public double? AverageExecutionTimeFrom { get; init; }

    public double? AverageExecutionTimeTo { get; init; }
}
