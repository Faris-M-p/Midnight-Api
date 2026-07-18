using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MidnightApi.Auth;
using MidnightApi.Exceptions;
using MidnightApi.Interfaces;
using MidnightApi.Models.Api;
using MidnightApi.Services;

namespace MidnightApi.Controllers;

[ApiController]
[Route("api/members")]
[Tags("Members")]
[Authorize]
public class MemberController : ControllerBase
{
    private readonly IMembersRepository _members;
    private readonly CommonService _commonService;

    public MemberController(IMembersRepository members, CommonService commonService)
    {
        _members = members;
        _commonService = commonService;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] InputMemberListQuery query)
    {
        _commonService.ValidateModelState(ModelState);

        var data = await _members.GetListAsync(User.GetFamilyId(), query);

        return Ok(new ApiResponse<OutputPagedMembers>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Success.",
            Data = data,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById([FromRoute] InputMemberRouteRequest request)
    {
        _commonService.ValidateModelState(ModelState);

        var profile = await _members.GetProfileAsync(User.GetFamilyId(), request.Id)
            ?? throw new NotFoundException("Member not found.");

        return Ok(new ApiResponse<OutputMemberProfile>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Success.",
            Data = profile,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] InputCreateMember request)
    {
        _commonService.ValidateModelState(ModelState);

        var created = await _members.CreateAsync(User.GetFamilyId(), request, User.GetUsername());

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<OutputMemberProfile>
        {
            Success = true,
            StatusCode = StatusCodes.Status201Created,
            Message = "Member created successfully.",
            Data = created,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update([FromRoute] InputMemberRouteRequest route, [FromBody] InputUpdateMember request)
    {
        _commonService.ValidateModelState(ModelState);

        var updated = await _members.UpdateAsync(User.GetFamilyId(), route.Id, request, User.GetUsername())
            ?? throw new NotFoundException("Member not found.");

        return Ok(new ApiResponse<OutputMemberProfile>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Member updated successfully.",
            Data = updated,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete([FromRoute] InputMemberRouteRequest request)
    {
        _commonService.ValidateModelState(ModelState);

        if (!await _members.SoftDeleteAsync(User.GetFamilyId(), request.Id, User.GetUsername()))
        {
            throw new NotFoundException("Member not found.");
        }

        return Ok(new ApiResponse<object?>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Member deleted successfully.",
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPost("map-spouse")]
    public async Task<IActionResult> MapSpouse([FromBody] InputMapSpouse request)
    {
        _commonService.ValidateModelState(ModelState);

        await _members.MapSpouseAsync(User.GetFamilyId(), request.MemberId, request.SpouseId, User.GetUsername());

        return Ok(new ApiResponse<object?>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Spouse relationship mapped successfully.",
            TraceId = HttpContext.TraceIdentifier
        });
    }
}
