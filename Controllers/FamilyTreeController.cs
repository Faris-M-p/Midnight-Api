using Microsoft.AspNetCore.Mvc;
using MidnightApi.Interfaces;
using MidnightApi.Models;

namespace MidnightApi.Controllers;

[ApiController]
[Route("api/tree")]
public class FamilyTreeController : ControllerBase
{
    private readonly IFamilyTreeRepository _tree;

    public FamilyTreeController(IFamilyTreeRepository tree)
    {
        _tree = tree;
    }

    [HttpGet]
    public async Task<ActionResult<FamilyTreeSnapshot>> GetTree()
    {
        var result = await _tree.GetTreeSnapshotAsync();
        return Ok(result);
    }
}
