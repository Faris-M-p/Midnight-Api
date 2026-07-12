using Microsoft.AspNetCore.Mvc;
using MidnightApi.Interfaces;

namespace MidnightApi.Controllers;

using MidnightApi.Models.Api;

[ApiController]
[Route("api/families")]
public class FamilyController : ControllerBase
{
    private readonly IFamiliesRepository _families;

    public FamilyController(IFamiliesRepository families)
    {
        _families = families;
    }

    [HttpGet]
    public async Task<ActionResult<List<OutputGetFamily>>> GetAll()
    {
        return Ok(await _families.GetAllAsync());
    }

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
