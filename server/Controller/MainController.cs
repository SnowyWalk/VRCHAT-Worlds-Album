using Microsoft.AspNetCore.Mvc;
using server.Schema;
using server.Service;
using System.Text.Json;
namespace server.Controllers;

[ApiController]
[Route("/api")]
public class MainController : ControllerBase
{
    private Database m_database;
    private VRCClient m_vrcClient;
    private WorldPreprocessor m_worldPreprocessor;
    
    public MainController(Database database, VRCClient vrcClient, WorldPreprocessor worldPreprocessor)
    {
        m_database = database;
        m_vrcClient = vrcClient;
        m_worldPreprocessor = worldPreprocessor;
    }

    [HttpGet("worldmetadata/{worldId}")]
    public async Task<ActionResult<string>> GetWorldMetadata([FromRoute] string worldId)
    {
        WorldMetadata? worldMetadata = await m_vrcClient.FetchVRCWorldMetadata(worldId);
        if (worldMetadata == null)
            return Problem("아무튼 실패함");
        return Ok(JsonSerializer.Serialize(worldMetadata));
    }

    [HttpGet("page/{page:int}")]
    public ActionResult<List<WorldMetadata>> GetPage([FromRoute] int page = 0, [FromQuery] int pageCount = 10)
    {
        m_worldPreprocessor.Scan(null);
        return Ok(m_database.GetWorldDataListByPaging(page, pageCount));
    }
}