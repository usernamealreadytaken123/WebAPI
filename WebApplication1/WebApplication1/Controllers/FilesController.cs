using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/files")]
public sealed class FilesController : ControllerBase
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public IActionResult Upload(IFormFile? file)
    {
        if (file is null)
        {
            return BadRequest("Файл не передан.");
        }

        return Ok(new
        {
            FileName = file.FileName,
            SizeInBytes = file.Length
        });
    }
}
