using Microsoft.EntityFrameworkCore;
using WebApplication1.Contracts.Values;
using WebApplication1.Data;

namespace WebApplication1.Services;

public sealed class ValueQueryService(AppDbContext dbContext)
{
    public async Task<IReadOnlyList<ValueResponse>> GetLatestAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Необходимо указать имя файла.");
        }

        fileName = fileName.Trim();

        if (fileName.Length > 255)
        {
            throw new ArgumentException(
                "Имя файла не может быть длиннее 255 символов.");
        }

        return await dbContext.Values
            .AsNoTracking()
            .Where(item => item.ProcessingResult.FileName == fileName)
            .OrderByDescending(item => item.Date)
            .ThenByDescending(item => item.Id)
            .Take(10)
            .Select(item => new ValueResponse
            {
                Id = item.Id,
                Date = item.Date,
                ExecutionTime = item.ExecutionTime,
                Value = item.Value
            })
            .ToListAsync(cancellationToken);
    }
}
