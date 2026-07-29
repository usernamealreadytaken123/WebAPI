using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Entities;
using WebApplication1.Models;

namespace WebApplication1.Services;

public sealed class CsvStorageService(AppDbContext dbContext)
{
    public async Task<long> SaveAsync(
        string fileName,
        IReadOnlyList<CsvRecord> records,
        CsvStatistics statistics,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(statistics);

        if (records.Count == 0)
        {
            throw new ArgumentException(
                "Для сохранения требуется хотя бы одна запись.",
                nameof(records));
        }

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var existingResult = await dbContext.Results
                .SingleOrDefaultAsync(
                    item => item.FileName == fileName,
                    cancellationToken);

            if (existingResult is not null)
            {
                dbContext.Results.Remove(existingResult);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var result = new ProcessingResult
            {
                FileName = fileName,
                TimeDeltaSeconds = statistics.TimeDeltaSeconds,
                FirstOperationDate = statistics.FirstOperationDate,
                AverageExecutionTime = statistics.AverageExecutionTime,
                AverageValue = statistics.AverageValue,
                MedianValue = statistics.MedianValue,
                MaximumValue = statistics.MaximumValue,
                MinimumValue = statistics.MinimumValue,
                Values = records.Select(record => new TimeSeriesValue
                {
                    Date = record.Date,
                    ExecutionTime = record.ExecutionTime,
                    Value = record.Value
                }).ToList()
            };

            dbContext.Results.Add(result);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return result.Id;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
