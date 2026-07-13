using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MidnightApi.Auth;
using MidnightApi.Exceptions;
using MidnightApi.Interfaces;

namespace MidnightApi.Controllers;

using MidnightApi.Models.Api;

/// <summary>Family profile for the authenticated family only.</summary>
[ApiController]
[Route("api/families")]
[Tags("Families")]
[Authorize]
public class FamilyController : ControllerBase
{
    private readonly IFamiliesRepository _families;

    public FamilyController(IFamiliesRepository families)
    {
        _families = families;
    }

    /// <summary>Get the family belonging to the authenticated account.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyFamily()
    {
        var family = await _families.GetByIdAsync(User.GetFamilyId())
            ?? throw new NotFoundException("Family not found.");

        return Ok(new ApiResponse<OutputGetFamily>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Success.",
            Data = family,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    /// <summary>Update the authenticated family's profile.</summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyFamily([FromBody] InputUpdateFamilyView request)
    {
        var updated = await _families.UpdateAsync(User.GetFamilyId(), new InputUpdateFamily
        {
            FamilyName = request.FamilyName.Trim(),
            Description = request.Description
        }, User.GetUsername())
            ?? throw new NotFoundException("Family not found.");

        return Ok(new ApiResponse<OutputGetFamily>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Family updated successfully.",
            Data = updated,
            TraceId = HttpContext.TraceIdentifier
        });
    }
}
