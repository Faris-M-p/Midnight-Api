using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MidnightApi.Auth;
using MidnightApi.Exceptions;
using MidnightApi.Interfaces;
using MidnightApi.Models;
using MidnightApi.Services;
using Npgsql;

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

    public FamilyController(
        IFamiliesRepository families,
        IMembersRepository members,
        CommonService commonService)
    {
        _families = families;
        _members = members;
        _commonService = commonService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        _commonService.ValidateModelState(ModelState);

        var family = await _families.GetByIdAsync(new InputGetFamily { Id = User.GetFamilyId() })
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

        var result = await _families.UpdateAsync(new InputUpdateFamily
        {
            Id = User.GetFamilyId(),
            FamilyName = request.FamilyName.Trim(),
            Description = request.Description,
            UpdatedBy = User.GetUsername()
        });

        return _commonService.ToActionResult(result, HttpContext.TraceIdentifier);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        _commonService.ValidateModelState(ModelState);

        OutputDashboard? data;
        try
        {
            data = await _members.GetDashboardAsync(new InputMemberDashboard
            {
                FamilyId = User.GetFamilyId()
            });
        }
        catch (PostgresException ex) when (ex.MessageText.Contains("Family not found", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotFoundException("Family not found.");
        }

        return Ok(new ApiResponse<OutputDashboard>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Success.",
            Data = data ?? throw new NotFoundException("Family not found."),
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpGet("timeline")]
    public async Task<IActionResult> GetTimeline()
    {
        _commonService.ValidateModelState(ModelState);

        var data = await _members.GetTimelineAsync(new InputMemberTimeline
        {
            FamilyId = User.GetFamilyId()
        });

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
