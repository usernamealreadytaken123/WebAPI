using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/files")]
public sealed class FilesController(
    CsvParser csvParser,
    CsvStatisticsCalculator statisticsCalculator) : ControllerBase
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        IFormFile? file,
        CancellationToken cancellationToken)
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

        try
        {
            using var stream = file.OpenReadStream();
            var records = await csvParser.ParseAsync(stream, cancellationToken);
            var statistics = statisticsCalculator.Calculate(records);

            return Ok(new
            {
                FileName = file.FileName,
                SizeInBytes = file.Length,
                Header = CsvParser.ExpectedHeader,
                DataRowCount = records.Count,
                Records = records,
                Statistics = statistics
            });
        }
        catch (InvalidDataException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}
