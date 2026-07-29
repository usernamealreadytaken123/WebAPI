using System.Globalization;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/files")]
public sealed class FilesController : ControllerBase
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("Файл не передан или пуст.");
        }

        var extension = Path.GetExtension(file.FileName);

        if (!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Допускаются только CSV-файлы.");
        }

        using var reader = new StreamReader(file.OpenReadStream());

        var header = await reader.ReadLineAsync();
        const string expectedHeader = "Date;ExecutionTime;Value";

        if (!string.Equals(header, expectedHeader, StringComparison.Ordinal))
        {
            return BadRequest($"Некорректный заголовок CSV. Ожидается: {expectedHeader}.");
        }

        var dataRowCount = 0;

        while (await reader.ReadLineAsync() is { } line)
        {
            dataRowCount++;

            var csvLineNumber = dataRowCount + 1;
            var columns = line.Split(';');

            if (columns.Length != 3)
            {
                return BadRequest(
                    $"Строка {csvLineNumber}: ожидается 3 значения, разделённых символом ';'.");
            }

            if (!DateTime.TryParse(
                    columns[0],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out _))
            {
                return BadRequest(
                    $"Строка {csvLineNumber}: значение Date имеет неверный формат.");
            }

            if (!double.TryParse(
                    columns[1],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out _))
            {
                return BadRequest(
                    $"Строка {csvLineNumber}: значение ExecutionTime имеет неверный формат.");
            }

            if (!double.TryParse(
                    columns[2],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out _))
            {
                return BadRequest(
                    $"Строка {csvLineNumber}: значение Value имеет неверный формат.");
            }
        }

        return Ok(new
        {
            FileName = file.FileName,
            SizeInBytes = file.Length,
            Header = header,
            DataRowCount = dataRowCount
        });
    }
}
