using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MidnightApi.Auth;
using MidnightApi.Exceptions;
using MidnightApi.Interfaces;
using MidnightApi.Models.Api;
using MidnightApi.Services;

namespace MidnightApi.Controllers;

[ApiController]
[Route("api/family")]
[Tags("Family")]
[Authorize]
public class FamilyController : ControllerBase
{
    private readonly IFamiliesRepository _families;
    private readonly IMembersRepository _members;
    private readonly CommonService _commonService;

    public FamilyController(IFamiliesRepository families, IMembersRepository members, CommonService commonService)
    {
        _families = families;
        _members = members;
        _commonService = commonService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        _commonService.ValidateModelState(ModelState);

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
        _commonService.ValidateModelState(ModelState);

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
        _commonService.ValidateModelState(ModelState);

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
        _commonService.ValidateModelState(ModelState);

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
