using Microsoft.AspNetCore.Mvc;
using WebApplication1.Contracts.Values;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/values")]
public sealed class ValuesController(ValueQueryService valueQueryService)
    : ControllerBase
{
    [HttpGet("latest")]
    [ProducesResponseType<IReadOnlyList<ValueResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ValueResponse>>> GetLatest(
        [FromQuery] string? fileName,
        CancellationToken cancellationToken)
    {
        try
        {
            var values = await valueQueryService.GetLatestAsync(
                fileName ?? string.Empty,
                cancellationToken);

            return Ok(values);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}
