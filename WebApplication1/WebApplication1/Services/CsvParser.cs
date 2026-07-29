using System.Globalization;
using System.Text;
using WebApplication1.Models;

namespace WebApplication1.Services;

public sealed class CsvParser
{
    public const string ExpectedHeader = "Date;ExecutionTime;Value";

    private const int MaximumDataRowCount = 10_000;

    private static readonly DateTime MinimumDateUtc =
        new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public async Task<IReadOnlyList<CsvRecord>> ParseAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 1024,
            leaveOpen: true);

        var header = await reader.ReadLineAsync(cancellationToken);

        if (!string.Equals(header, ExpectedHeader, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Некорректный заголовок CSV. Ожидается: {ExpectedHeader}.");
        }

        var records = new List<CsvRecord>();

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            var dataRowCount = records.Count + 1;

            if (dataRowCount > MaximumDataRowCount)
            {
                throw new InvalidDataException(
                    "CSV может содержать не более 10 000 строк данных.");
            }

            var csvLineNumber = dataRowCount + 1;
            var columns = line.Split(';');

            if (columns.Length != 3)
            {
                throw new InvalidDataException(
                    $"Строка {csvLineNumber}: ожидается 3 значения, разделённых символом ';'.");
            }

            if (!DateTime.TryParse(
                    columns[0],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var date))
            {
                throw new InvalidDataException(
                    $"Строка {csvLineNumber}: значение Date имеет неверный формат.");
            }

            var dateUtc = date.ToUniversalTime();

            if (dateUtc < MinimumDateUtc || dateUtc > DateTime.UtcNow)
            {
                throw new InvalidDataException(
                    $"Строка {csvLineNumber}: Date должна быть не раньше 01.01.2000 и не позже текущего момента.");
            }

            if (!double.TryParse(
                    columns[1],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var executionTime))
            {
                throw new InvalidDataException(
                    $"Строка {csvLineNumber}: значение ExecutionTime имеет неверный формат.");
            }

            if (!double.IsFinite(executionTime) || executionTime < 0)
            {
                throw new InvalidDataException(
                    $"Строка {csvLineNumber}: значение ExecutionTime должно быть неотрицательным конечным числом.");
            }

            if (!double.TryParse(
                    columns[2],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var value))
            {
                throw new InvalidDataException(
                    $"Строка {csvLineNumber}: значение Value имеет неверный формат.");
            }

            if (!double.IsFinite(value) || value < 0)
            {
                throw new InvalidDataException(
                    $"Строка {csvLineNumber}: значение Value должно быть неотрицательным конечным числом.");
            }

            records.Add(new CsvRecord
            {
                Date = dateUtc,
                ExecutionTime = executionTime,
                Value = value
            });
        }

        if (records.Count == 0)
        {
            throw new InvalidDataException(
                "CSV должен содержать хотя бы одну строку данных.");
        }

        return records;
    }
}
