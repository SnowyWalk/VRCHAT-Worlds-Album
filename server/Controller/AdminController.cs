using Microsoft.AspNetCore.Mvc;
using server.Service;


[ApiController]
[Route("/admin")]
public class AdminController : ControllerBase
{
    private readonly Database m_database;

    public AdminController(Database database)
    {
        m_database = database;
    }

    [HttpPost("setcategory/{worldId}/{categoryIdListString}")]
    public async Task<ActionResult> UpdateWorldDataCategoryList(string worldId, string categoryIdListString)
    {
        var categoryIdList = categoryIdListString.Split('^').Select(int.Parse).ToList();
        await m_database.UpdateWorldDataCategoryList(worldId, categoryIdList);
        return Ok();
    }
}
