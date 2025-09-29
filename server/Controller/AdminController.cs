using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using server.Schema;
using server.Service;


[ApiController]
[Route("/admin")]
public class AdminController : ControllerBase
{
    private readonly Database m_database;

    public class WorldCategoryDTO
    {
        public string WorldId { get; set; }
        public List<WorldCategory> CategoryList { get; set; }
    }

    public AdminController(Database database)
    {
        m_database = database;
    }

    [HttpGet("getcategory/{worldIdListString}")]
    public async Task<ActionResult<List<WorldCategoryDTO>>> GetWorldDataCategoryList(string worldIdListString)
    {
        string[] worldIdList = worldIdListString.Split('^').ToArray();
        List<WorldCategory> result = await m_database.GetWorldDataCategoryList(worldIdList);
        return Ok(result);
    }

    [HttpPost("setcategory/{worldId}/{categoryListString}")]
    public async Task<ActionResult<WorldCategoryDTO>> UpdateWorldDataCategoryList(string worldId, string categoryListString)
    {
        var categoryIdList = categoryListString.Split('^').ToList();
        List<WorldCategory> resultCategoryList;
        try
        {
            await m_database.UpdateWorldDataCategoryList(worldId, categoryIdList);

        }
        catch (Exception e)
        {
            return StatusCode(500, e);
        }

        return Ok(new WorldCategoryDTO()
        {
            WorldId = worldId,
            CategoryList = (await GetWorldDataCategoryList(worldId)).Result,
        });
    }
}
