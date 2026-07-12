using Microsoft.AspNetCore.Mvc;
using MidnightApi.Interfaces;

namespace MidnightApi.Controllers;

using MidnightApi.Models.Api;

/// <summary>Manage family groups.</summary>
[ApiController]
[Route("api/families")]
[Tags("Families")]
public class FamilyController : ControllerBase
{
    private readonly IFamiliesRepository _families;

    public FamilyController(IFamiliesRepository families)
    {
        _families = families;
    }

    /// <summary>Get all families.</summary>
    [HttpGet]
    public async Task<ActionResult<List<OutputGetFamily>>> GetAll()
    {
        return Ok(await _families.GetAllAsync());
    }

    /// <summary>Get a family by id.</summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<OutputGetFamily>> GetById(long id)
    {
        var family = await _families.GetByIdAsync(id);
        if (family is null)
        {
            return NotFound(new { message = "Family not found." });
        }

        return Ok(family);
    }

    /// <summary>Create a new family.</summary>
    [HttpPost]
    public async Task<ActionResult<OutputGetFamily>> Create([FromBody] InputCreateFamilyView view)
    {
        var input = MapToCreateInput(view);
        if (await _families.ExistsByCodeAsync(input.FamilyCode))
        {
            return Conflict(new { message = "Family code already exists." });
        }

        var created = await _families.CreateAsync(input, "system");
        return CreatedAtAction(nameof(GetById), new { id = created.ID_Families }, created);
    }

    /// <summary>Update an existing family.</summary>
    [HttpPut("{id:long}")]
    public async Task<ActionResult<OutputGetFamily>> Update(long id, [FromBody] InputUpdateFamilyView view)
    {
        var updated = await _families.UpdateAsync(id, MapToUpdateInput(view), "system");
        if (updated is null)
        {
            return NotFound(new { message = "Family not found." });
        }

        return Ok(updated);
    }

    /// <summary>Soft-delete a family.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        if (!await _families.SoftDeleteAsync(id, "system"))
        {
            return NotFound(new { message = "Family not found." });
        }

        return NoContent();
    }

    private static InputCreateFamily MapToCreateInput(InputCreateFamilyView view) => new()
    {
        FamilyCode = view.FamilyCode,
        FamilyName = view.FamilyName,
        Description = view.Description
    };

    private static InputUpdateFamily MapToUpdateInput(InputUpdateFamilyView view) => new()
    {
        FamilyName = view.FamilyName,
        Description = view.Description
    };
}
