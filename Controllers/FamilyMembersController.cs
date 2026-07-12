using Microsoft.AspNetCore.Mvc;
using MidnightApi.Interfaces;
using MidnightApi.Models;

namespace MidnightApi.Controllers;

[ApiController]
[Route("api/members")]
public class FamilyMembersController : ControllerBase
{
    private readonly IFamilyMemberRepository _members;

    public FamilyMembersController(IFamilyMemberRepository members)
    {
        _members = members;
    }

    [HttpGet]
    public async Task<ActionResult<List<FamilyMember>>> GetAll()
    {
        var result = await _members.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FamilyMember>> GetById(string id)
    {
        var result = await _members.GetByIdAsync(id);
        if (result is null)
        {
            return NotFound(new { message = "Member not found." });
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<FamilyMember>> Create([FromBody] FamilyMember member)
    {
        var existing = await _members.GetByIdAsync(member.Id);
        if (existing is not null)
        {
            return Conflict(new { message = "Member id already exists." });
        }

        var created = await _members.CreateAsync(member);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<FamilyMember>> Update(string id, [FromBody] FamilyMember member)
    {
        member.Id = id;
        var updated = await _members.UpdateAsync(id, member);
        if (updated is null)
        {
            return NotFound(new { message = "Member not found." });
        }

        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var deleted = await _members.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound(new { message = "Member not found." });
        }

        return NoContent();
    }
}
