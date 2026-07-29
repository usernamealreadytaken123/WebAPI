using WebApplication1.Models;

namespace WebApplication1.Services;

public sealed class CsvStatisticsCalculator
{
    public CsvStatistics Calculate(IReadOnlyList<CsvRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        if (records.Count == 0)
        {
            throw new ArgumentException(
                "Для расчёта статистики требуется хотя бы одна запись.",
                nameof(records));
        }

        var minimumDate = records[0].Date;
        var maximumDate = records[0].Date;
        var averageExecutionTime = 0.0;
        var averageValue = 0.0;
        var values = new double[records.Count];

        for (var index = 0; index < records.Count; index++)
        {
            var record = records[index];

            if (record.Date < minimumDate)
            {
                minimumDate = record.Date;
            }

            if (record.Date > maximumDate)
            {
                maximumDate = record.Date;
            }

            var processedRecordCount = index + 1;

            averageExecutionTime +=
                (record.ExecutionTime - averageExecutionTime) / processedRecordCount;

            averageValue +=
                (record.Value - averageValue) / processedRecordCount;

            values[index] = record.Value;
        }

        Array.Sort(values);

        var middleIndex = values.Length / 2;
        var medianValue = values.Length % 2 == 1
            ? values[middleIndex]
            : values[middleIndex - 1] +
              (values[middleIndex] - values[middleIndex - 1]) / 2;

        return new CsvStatistics
        {
            TimeDeltaSeconds = (maximumDate - minimumDate).TotalSeconds,
            FirstOperationDate = minimumDate,
            AverageExecutionTime = averageExecutionTime,
            AverageValue = averageValue,
            MedianValue = medianValue,
            MaximumValue = values[^1],
            MinimumValue = values[0]
        };
    }
}
