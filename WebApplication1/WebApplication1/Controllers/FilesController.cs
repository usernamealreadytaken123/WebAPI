using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/files")]
public sealed class FilesController(
    CsvParser csvParser,
    CsvStatisticsCalculator statisticsCalculator,
    CsvStorageService storageService) : ControllerBase
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

        var fileName = Path.GetFileName(file.FileName);

        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255)
        {
            return BadRequest("Имя файла должно содержать от 1 до 255 символов.");
        }

        var extension = Path.GetExtension(fileName);

        if (!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Допускаются только CSV-файлы.");
        }

        try
        {
            using var stream = file.OpenReadStream();
            var records = await csvParser.ParseAsync(stream, cancellationToken);
            var statistics = statisticsCalculator.Calculate(records);
            var resultId = await storageService.SaveAsync(
                fileName,
                records,
                statistics,
                cancellationToken);

            return Ok(new
            {
                ResultId = resultId,
                FileName = fileName,
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
