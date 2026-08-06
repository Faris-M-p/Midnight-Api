using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MidnightApi.Auth;
using MidnightApi.Exceptions;
using MidnightApi.Interfaces;
using MidnightApi.Models;
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

    public MemberController(
        IMembersRepository members,
        CommonService commonService)
    {
        _members = members;
        _commonService = commonService;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] InputMemberListQueryView query)
    {
        _commonService.ValidateModelState(ModelState);

        var data = await _members.GetListAsync(new InputMemberList
        {
            FamilyId = User.GetFamilyId(),
            Search = query.Search,
            Gender = query.Gender,
            SortBy = query.SortBy,
            SortDesc = query.SortDesc,
            Page = query.Page,
            PageSize = query.PageSize
        }) ?? new OutputPagedMembers();

        return Ok(new ApiResponse<OutputPagedMembers>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Success.",
            Data = data,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpGet("tree")]
    public async Task<IActionResult> GetTree()
    {
        var data = await _members.GetTreeAsync(new InputMemberTree
        {
            FamilyId = User.GetFamilyId()
        }) ?? new OutputFamilyTree();

        return Ok(new ApiResponse<OutputFamilyTree>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Success.",
            Data = data,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById([FromRoute] InputMemberRouteRequestView request)
    {
        _commonService.ValidateModelState(ModelState);

        var profile = await _members.GetByIdAsync(new InputGetMember
        {
            FamilyId = User.GetFamilyId(),
            MemberId = request.Id
        }) ?? throw new NotFoundException("Member not found.");

        return Ok(new ApiResponse<OutputGetMember>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Success.",
            Data = profile,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] InputCreateMemberView request)
    {
        _commonService.ValidateModelState(ModelState);

        var result = await _members.CreateAsync(new InputCreateMember
        {
            FamilyId = User.GetFamilyId(),
            ParentId = request.ParentId,
            SpouseId = request.SpouseId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth,
            DateOfDeath = request.DateOfDeath,
            IsRoot = request.IsRoot,
            Nickname = request.Nickname,
            Biography = request.Biography,
            Profession = request.Profession,
            Addresses = _commonService.ToJson(request.Addresses),
            Images = _commonService.ToJson(request.Images),
            Events = _commonService.ToJson(request.Events),
            Notes = _commonService.ToJson(request.Notes),
            SocialLinks = _commonService.ToJson(request.SocialLinks),
            CreatedBy = User.GetUsername()
        });

        return _commonService.ToActionResult(result, HttpContext.TraceIdentifier);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        [FromRoute] InputMemberRouteRequestView route,
        [FromBody] InputUpdateMemberView request)
    {
        _commonService.ValidateModelState(ModelState);

        var result = await _members.UpdateAsync(new InputUpdateMember
        {
            FamilyId = User.GetFamilyId(),
            MemberId = route.Id,
            ParentId = request.ParentId,
            SpouseId = request.SpouseId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth,
            DateOfDeath = request.DateOfDeath,
            IsRoot = request.IsRoot,
            Nickname = request.Nickname,
            Biography = request.Biography,
            Profession = request.Profession,
            Addresses = _commonService.ToJson(request.Addresses),
            Images = _commonService.ToJson(request.Images),
            Events = _commonService.ToJson(request.Events),
            Notes = _commonService.ToJson(request.Notes),
            SocialLinks = _commonService.ToJson(request.SocialLinks),
            UpdatedBy = User.GetUsername()
        });

        return _commonService.ToActionResult(result, HttpContext.TraceIdentifier);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete([FromRoute] InputMemberRouteRequestView request)
    {
        _commonService.ValidateModelState(ModelState);

        var result = await _members.SoftDeleteAsync(new InputDeleteMember
        {
            FamilyId = User.GetFamilyId(),
            MemberId = request.Id,
            DeletedBy = User.GetUsername()
        });

        return _commonService.ToActionResult(result, HttpContext.TraceIdentifier);
    }

    [HttpPost("map-spouse")]
    public async Task<IActionResult> MapSpouse([FromBody] InputMapSpouseView request)
    {
        _commonService.ValidateModelState(ModelState);

        var result = await _members.MapSpouseAsync(new InputMapSpouse
        {
            FamilyId = User.GetFamilyId(),
            MemberId = request.MemberId,
            SpouseId = request.SpouseId,
            UpdatedBy = User.GetUsername()
        });

        return _commonService.ToActionResult(result, HttpContext.TraceIdentifier);
    }
}
