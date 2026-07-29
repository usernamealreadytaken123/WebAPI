using Microsoft.EntityFrameworkCore;
using WebApplication1.Contracts.Results;
using WebApplication1.Data;

namespace WebApplication1.Services;

public sealed class ResultQueryService(AppDbContext dbContext)
{
    public async Task<IReadOnlyList<ResultResponse>> GetAsync(
        ResultFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        ValidateFilter(filter);

        var query = dbContext.Results.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.FileName))
        {
            var fileName = filter.FileName.Trim();
            query = query.Where(item => item.FileName == fileName);
        }

        if (filter.FirstOperationDateFrom is { } dateFrom)
        {
            var dateFromUtc = ToUtc(dateFrom);
            query = query.Where(item => item.FirstOperationDate >= dateFromUtc);
        }

        if (filter.FirstOperationDateTo is { } dateTo)
        {
            var dateToUtc = ToUtc(dateTo);
            query = query.Where(item => item.FirstOperationDate <= dateToUtc);
        }

        if (filter.AverageValueFrom is { } averageValueFrom)
        {
            query = query.Where(item => item.AverageValue >= averageValueFrom);
        }

        if (filter.AverageValueTo is { } averageValueTo)
        {
            query = query.Where(item => item.AverageValue <= averageValueTo);
        }

        if (filter.AverageExecutionTimeFrom is { } averageExecutionTimeFrom)
        {
            query = query.Where(
                item => item.AverageExecutionTime >= averageExecutionTimeFrom);
        }

        if (filter.AverageExecutionTimeTo is { } averageExecutionTimeTo)
        {
            query = query.Where(
                item => item.AverageExecutionTime <= averageExecutionTimeTo);
        }

        return await query
            .OrderByDescending(item => item.Id)
            .Select(item => new ResultResponse
            {
                Id = item.Id,
                FileName = item.FileName,
                TimeDeltaSeconds = item.TimeDeltaSeconds,
                FirstOperationDate = item.FirstOperationDate,
                AverageExecutionTime = item.AverageExecutionTime,
                AverageValue = item.AverageValue,
                MedianValue = item.MedianValue,
                MaximumValue = item.MaximumValue,
                MinimumValue = item.MinimumValue
            })
            .ToListAsync(cancellationToken);
    }

    private static void ValidateFilter(ResultFilterRequest filter)
    {
        if (filter.FileName?.Trim().Length > 255)
        {
            throw new ArgumentException(
                "Имя файла в фильтре не может быть длиннее 255 символов.");
        }

        if (filter.FirstOperationDateFrom is { } dateFrom &&
            filter.FirstOperationDateTo is { } dateTo &&
            ToUtc(dateFrom) > ToUtc(dateTo))
        {
            throw new ArgumentException(
                "Начало диапазона FirstOperationDate не может быть позже конца.");
        }

        ValidateNumberRange(
            filter.AverageValueFrom,
            filter.AverageValueTo,
            "AverageValue");

        ValidateNumberRange(
            filter.AverageExecutionTimeFrom,
            filter.AverageExecutionTimeTo,
            "AverageExecutionTime");
    }

    private static void ValidateNumberRange(
        double? from,
        double? to,
        string filterName)
    {
        if ((from is { } fromValue && !double.IsFinite(fromValue)) ||
            (to is { } toValue && !double.IsFinite(toValue)))
        {
            throw new ArgumentException(
                $"Границы диапазона {filterName} должны быть конечными числами.");
        }

        if (from > to)
        {
            throw new ArgumentException(
                $"Начало диапазона {filterName} не может быть больше конца.");
        }
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
