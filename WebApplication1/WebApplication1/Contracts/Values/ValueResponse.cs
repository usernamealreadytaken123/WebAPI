namespace WebApplication1.Contracts.Values;

public sealed class ValueResponse
{
    public long Id { get; init; }

    public DateTime Date { get; init; }

    public double ExecutionTime { get; init; }

    public double Value { get; init; }
}
