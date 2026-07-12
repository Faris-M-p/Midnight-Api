using Microsoft.AspNetCore.Mvc;
using MidnightApi.Interfaces;
using MidnightApi.Models;

namespace MidnightApi.Controllers;

[ApiController]
[Route("api/unions")]
public class MarriageUnionsController : ControllerBase
{
    private readonly IMarriageUnionRepository _unions;
    private readonly IFamilyMemberRepository _members;

    public MarriageUnionsController(
        IMarriageUnionRepository unions,
        IFamilyMemberRepository members)
    {
        _unions = unions;
        _members = members;
    }

    [HttpGet]
    public async Task<ActionResult<List<MarriageUnion>>> GetAll()
    {
        var result = await _unions.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MarriageUnion>> GetById(string id)
    {
        var result = await _unions.GetByIdAsync(id);
        if (result is null)
        {
            return NotFound(new { message = "Union not found." });
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<MarriageUnion>> Create([FromBody] MarriageUnion union)
    {
        if (!await IsValidUnionReferences(union))
        {
            return BadRequest(new { message = "One or more member ids are invalid." });
        }

        var existing = await _unions.GetByIdAsync(union.Id);
        if (existing is not null)
        {
            return Conflict(new { message = "Union id already exists." });
        }

        var created = await _unions.CreateAsync(union);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<MarriageUnion>> Update(string id, [FromBody] MarriageUnion union)
    {
        union.Id = id;
        if (!await IsValidUnionReferences(union))
        {
            return BadRequest(new { message = "One or more member ids are invalid." });
        }

        var updated = await _unions.UpdateAsync(id, union);
        if (updated is null)
        {
            return NotFound(new { message = "Union not found." });
        }

        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var deleted = await _unions.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound(new { message = "Union not found." });
        }

        return NoContent();
    }

    private async Task<bool> IsValidUnionReferences(MarriageUnion union)
    {
        if (union.Spouse1Id == union.Spouse2Id)
        {
            return false;
        }

        var spouse1 = await _members.GetByIdAsync(union.Spouse1Id);
        var spouse2 = await _members.GetByIdAsync(union.Spouse2Id);
        if (spouse1 is null || spouse2 is null)
        {
            return false;
        }

        foreach (var childId in union.ChildrenIds)
        {
            var child = await _members.GetByIdAsync(childId);
            if (child is null)
            {
                return false;
            }
        }

        return true;
    }
}
