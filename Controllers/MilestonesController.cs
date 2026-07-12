using Microsoft.AspNetCore.Mvc;
using MidnightApi.Interfaces;
using MidnightApi.Models;

namespace MidnightApi.Controllers;

[ApiController]
[Route("api/milestones")]
public class MilestonesController : ControllerBase
{
    private readonly IMilestoneRepository _milestones;
    private readonly IFamilyMemberRepository _members;

    public MilestonesController(IMilestoneRepository milestones, IFamilyMemberRepository members)
    {
        _milestones = milestones;
        _members = members;
    }

    [HttpGet]
    public async Task<ActionResult<List<Milestone>>> GetAll()
    {
        var result = await _milestones.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Milestone>> GetById(string id)
    {
        var result = await _milestones.GetByIdAsync(id);
        if (result is null)
        {
            return NotFound(new { message = "Milestone not found." });
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Milestone>> Create([FromBody] Milestone milestone)
    {
        if (!await IsValidMemberReference(milestone.MemberId))
        {
            return BadRequest(new { message = "Invalid member id for milestone." });
        }

        var existing = await _milestones.GetByIdAsync(milestone.Id);
        if (existing is not null)
        {
            return Conflict(new { message = "Milestone id already exists." });
        }

        var created = await _milestones.CreateAsync(milestone);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Milestone>> Update(string id, [FromBody] Milestone milestone)
    {
        milestone.Id = id;
        if (!await IsValidMemberReference(milestone.MemberId))
        {
            return BadRequest(new { message = "Invalid member id for milestone." });
        }

        var updated = await _milestones.UpdateAsync(id, milestone);
        if (updated is null)
        {
            return NotFound(new { message = "Milestone not found." });
        }

        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        var deleted = await _milestones.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound(new { message = "Milestone not found." });
        }

        return NoContent();
    }

    private async Task<bool> IsValidMemberReference(string memberId)
    {
        var member = await _members.GetByIdAsync(memberId);
        return member is not null;
    }
}
