using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MidnightApi.Auth;
using MidnightApi.Exceptions;
using MidnightApi.Interfaces;

namespace MidnightApi.Controllers;

using MidnightApi.Models.Api;

[ApiController]
[Route("api/family")]
[Tags("Family")]
[Authorize]
public class FamilyController : ControllerBase
{
    private readonly IFamiliesRepository _families;
    private readonly IMembersRepository _members;

    public FamilyController(IFamiliesRepository families, IMembersRepository members)
    {
        _families = families;
        _members = members;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
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

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] InputUpdateFamilyView request)
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

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var data = await _members.GetDashboardAsync(User.GetFamilyId());

        return Ok(new ApiResponse<OutputDashboard>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Success.",
            Data = data,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpGet("timeline")]
    public async Task<IActionResult> GetTimeline()
    {
        var data = await _members.GetTimelineAsync(User.GetFamilyId());

        return Ok(new ApiResponse<List<OutputTimelineItem>>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Success.",
            Data = data,
            TraceId = HttpContext.TraceIdentifier
        });
    }
}
