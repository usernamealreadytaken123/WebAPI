using Microsoft.AspNetCore.Mvc;
using WebApplication1.Contracts.Results;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/results")]
public sealed class ResultsController(ResultQueryService resultQueryService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ResultResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ResultResponse>>> Get(
        [FromQuery] ResultFilterRequest filter,
        CancellationToken cancellationToken)
    {
        try
        {
            var results = await resultQueryService.GetAsync(
                filter,
                cancellationToken);

            return Ok(results);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }
}
