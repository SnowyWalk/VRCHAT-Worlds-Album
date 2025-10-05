using Microsoft.AspNetCore.Mvc;
using server.Core;
using server.Schema;
using server.Service;
using server.Util;
using System.Buffers.Text;
using System.Text.Json;
namespace server.Controllers;

[ApiController]
[Route("/api")]
public class MainController : ControllerBase
{
    private readonly Database m_database;
    private readonly VRCClient m_vrcClient;
    private readonly WorldPreprocessor m_worldPreprocessor;
    
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

    [HttpGet("worlddatalist")]
    public async Task<ActionResult<List<WorldMetadata>>> GetPage([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        Log.Debug($"[GetPage] GET {page}, {pageSize}");
        m_worldPreprocessor.Scan(); // 일부러 await 안 한다.
        List<WorldData> worldDataList = await m_database.GetWorldDataListByPage(page, pageSize);
        return Ok(worldDataList);
    }
    
    [HttpGet("worlddatalist/{cursor}")]
    public async Task<ActionResult<List<WorldMetadata>>> GetPage([FromRoute] string cursor, [FromQuery] int pageSize = 10)
    {
        m_worldPreprocessor.Scan(); // 일부러 await 안 한다.

        (DateTime dateTime, string worldId) = CursorUtil.DecodeCursor(cursor);
        
        pageSize = Math.Clamp(1, pageSize, 100);
        List<WorldData> worldDataList = await m_database.GetWorldDataListAfterCursor(dateTime, worldId, pageSize);
        return Ok(worldDataList);
    }


}