using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MidnightApi.Auth;
using MidnightApi.Exceptions;
using MidnightApi.Interfaces;
using MidnightApi.Services;

namespace MidnightApi.Controllers;

using MidnightApi.Models.Api;

[ApiController]
[Route("api/members")]
[Tags("Members")]
[Authorize]
public class MemberController : ControllerBase
{
    private readonly IMembersRepository _members;
    private readonly MemberValidationService _validation;

    public MemberController(IMembersRepository members, MemberValidationService validation)
    {
        _members = members;
        _validation = validation;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] InputMemberListQuery query)
    {
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

    [HttpGet("tree")]
    public async Task<IActionResult> GetTree()
    {
        var data = await _members.GetTreeAsync(User.GetFamilyId());

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
    public async Task<IActionResult> GetById(long id)
    {
        var profile = await _members.GetProfileAsync(User.GetFamilyId(), id)
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
    public async Task<IActionResult> Create([FromBody] InputSaveMember request)
    {
        await ValidateSaveAsync(request, memberId: null);

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
    public async Task<IActionResult> Update(long id, [FromBody] InputSaveMember request)
    {
        await ValidateSaveAsync(request, id);

        var updated = await _members.UpdateAsync(User.GetFamilyId(), id, request, User.GetUsername())
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
    public async Task<IActionResult> Delete(long id)
    {
        if (!await _members.SoftDeleteAsync(User.GetFamilyId(), id, User.GetUsername()))
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

    [HttpPost("{memberId:long}/child")]
    public async Task<IActionResult> AddChild(long memberId, [FromBody] InputAddChild request)
    {
        if (!await _members.ExistsInFamilyAsync(User.GetFamilyId(), memberId))
        {
            throw new NotFoundException("Parent member not found.");
        }

        request.Child.ParentId = memberId;
        request.Child.IsRoot = false;
        await ValidateSaveAsync(request.Child, memberId: null);

        var child = await _members.AddChildAsync(User.GetFamilyId(), memberId, request.Child, User.GetUsername());

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<OutputMemberProfile>
        {
            Success = true,
            StatusCode = StatusCodes.Status201Created,
            Message = "Child added successfully.",
            Data = child,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPost("{memberId:long}/spouse")]
    public async Task<IActionResult> AddSpouse(long memberId, [FromBody] InputAddSpouse request)
    {
        if (!await _members.ExistsInFamilyAsync(User.GetFamilyId(), memberId))
        {
            throw new NotFoundException("Member not found.");
        }

        request.Spouse.IsRoot = false;
        request.Spouse.ParentId = null;
        request.Spouse.SpouseId = null;
        await ValidateSaveAsync(request.Spouse, memberId: null);

        var spouse = await _members.AddSpouseAsync(User.GetFamilyId(), memberId, request.Spouse, User.GetUsername());

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<OutputMemberProfile>
        {
            Success = true,
            StatusCode = StatusCodes.Status201Created,
            Message = "Spouse added successfully.",
            Data = spouse,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPost("map-spouse")]
    public async Task<IActionResult> MapSpouse([FromBody] InputMapSpouse request)
    {
        var familyId = User.GetFamilyId();

        if (!await _members.ExistsInFamilyAsync(familyId, request.MemberId)
            || !await _members.ExistsInFamilyAsync(familyId, request.SpouseId))
        {
            throw new NotFoundException("One or both members were not found in this family.");
        }

        var error = await _validation.ValidateMapSpouseAsync(
            request.MemberId,
            request.SpouseId,
            async id =>
            {
                var relation = await _members.GetRelationAsync(familyId, id)
                    ?? throw new NotFoundException("Member not found.");
                return relation;
            });

        if (error is not null)
        {
            throw new BadRequestException(error);
        }

        await _members.MapSpouseAsync(familyId, request.MemberId, request.SpouseId, User.GetUsername());

        return Ok(new ApiResponse<object?>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Spouse relationship mapped successfully.",
            TraceId = HttpContext.TraceIdentifier
        });
    }

    private async Task ValidateSaveAsync(InputSaveMember request, long? memberId)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new BadRequestException("First name and last name are required.");
        }

        var emailError = _validation.ValidateEmail(request.Email);
        if (emailError is not null)
        {
            throw new BadRequestException(emailError);
        }

        var phoneError = _validation.ValidatePhone(request.Phone);
        if (phoneError is not null)
        {
            throw new BadRequestException(phoneError);
        }

        var selfError = _validation.ValidateSelfReference(memberId ?? 0, request.ParentId, request.SpouseId);
        if (selfError is not null)
        {
            throw new BadRequestException(selfError);
        }

        var familyId = User.GetFamilyId();
        var rootError = _validation.ValidateRootMember(
            request.IsRoot,
            await _members.HasRootMemberAsync(familyId, memberId));
        if (rootError is not null)
        {
            throw new BadRequestException(rootError);
        }

        if (request.ParentId.HasValue && !await _members.ExistsInFamilyAsync(familyId, request.ParentId.Value))
        {
            throw new NotFoundException("Parent member not found.");
        }

        if (request.SpouseId.HasValue && !await _members.ExistsInFamilyAsync(familyId, request.SpouseId.Value))
        {
            throw new NotFoundException("Spouse member not found.");
        }

        var circular = await _validation.ValidateCircularParentAsync(
            memberId ?? 0,
            request.ParentId,
            _members.GetParentIdAsync);
        if (circular is not null)
        {
            throw new BadRequestException(circular);
        }
    }
}
