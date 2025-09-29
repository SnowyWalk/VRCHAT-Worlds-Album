﻿using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using server.Schema;
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

    [HttpGet("getcategory/{worldIdListString}")]
    public async Task<ActionResult<Dictionary<string,List<WorldCategory>>>> GetWorldDataCategoryList(string worldIdListString)
    {
        string[] worldIdList = worldIdListString.Split('^').ToArray();
        Dictionary<string,List<WorldCategory>> result = await m_database.GetWorldDataCategoryList(worldIdList);
        return Ok(result);
    }

    [HttpPost("setcategory/{worldId}/{categoryListString}")]
    public async Task<ActionResult<Dictionary<string, List<WorldCategory>>>> UpdateWorldDataCategoryList(string worldId, string categoryListString)
    {
        var categoryIdList = categoryListString.Split('^').ToList();
        try
        {
            await m_database.UpdateWorldDataCategoryList(worldId, categoryIdList);
        }
        catch (Exception e)
        {
            return StatusCode(500, e);
        }
        List<WorldCategory> finalState = await m_database.GetWorldDataCategoryList(worldId);
        Dictionary<string, List<WorldCategory>> result = new();
        result[worldId] = finalState;

        return Ok(result);
    }
}
