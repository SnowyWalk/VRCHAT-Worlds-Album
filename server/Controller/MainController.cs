using Microsoft.AspNetCore.Mvc;
using server.Schema;
using server.Service;
using System.Text.Json;
namespace server.Controllers;

[ApiController]
[Route("/api")]
public class MainController : ControllerBase
{
    [HttpGet("worldmetadata/{worldId}")]
    public async Task<ActionResult<string>> GetWorldMetadata([FromRoute] string worldId)
    {
        WorldMetadata? worldMetadata = await VRCClient.FetchVRCWorldMetadata(worldId);
        if (worldMetadata == null)
            return Problem("아무튼 실패함");
        return Ok(JsonSerializer.Serialize(worldMetadata));
    }

    [HttpGet("page/{page:int}")]
    public ActionResult<List<WorldMetadata>> GetPage([FromRoute] int page = 0, [FromQuery] int pageCount = 10)
    {
        return Ok(Database.Instance.GetWorldDataListByPaging(page, pageCount));
    }
}