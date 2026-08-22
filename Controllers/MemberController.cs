using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MidnightApi.Auth;
using MidnightApi.Exceptions;
using MidnightApi.Models;
using MidnightApi.Repositories.Interfaces;
using MidnightApi.Services.Interfaces;

namespace MidnightApi.Controllers;

[ApiController]
[Route("api/members")]
[Tags("Members")]
[Authorize]
public class MemberController : ControllerBase
{
    private readonly IMembersRepository _iMembersRepository;
    private readonly ICommonService _iCommonService;
    private readonly IImageFileService _iImageFileService;
    private readonly IFamilyStorageService _iFamilyStorageService;
    private readonly IAccessAuthorizationService _iAccessAuthorizationService;

    public MemberController(
        IMembersRepository members,
        ICommonService commonService,
        IImageFileService images,
        IFamilyStorageService storage,
        IAccessAuthorizationService authz)
    {
        _iMembersRepository = members;
        _iCommonService = commonService;
        _iImageFileService = images;
        _iFamilyStorageService = storage;
        _iAccessAuthorizationService = authz;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] InputMemberListQueryView query)
    {
        _iCommonService.ValidateModelState(ModelState);

        var data = await _iMembersRepository.GetListAsync(new InputMemberList
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
        var data = await _iMembersRepository.GetTreeAsync(new InputMemberTree
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
        _iCommonService.ValidateModelState(ModelState);

        var profile = await _iMembersRepository.GetByIdAsync(new InputGetMember
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
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Create([FromForm] InputCreateMemberView request, CancellationToken cancellationToken)
    {
        _iCommonService.ValidateModelState(ModelState);
        await _iAccessAuthorizationService.EnsureCanCreateMemberAsync(User, request.ParentId);

        var images = request.Images ?? [];
        if (request.ProfileImage is { Length: > 0 })
        {
            await _iFamilyStorageService.EnsureCanUploadAsync(User.GetFamilyId(), request.ProfileImage.Length);

            var saved = await _iImageFileService.SaveAsync(
                new ImageUploadRequest
                {
                    File = request.ProfileImage,
                    Mode = ImageUploadMode.MemberProfile,
                    FamilyId = User.GetFamilyId()
                },
                Request,
                cancellationToken);

            await _iFamilyStorageService.EnsureCanUploadAsync(User.GetFamilyId(), saved.FileSize);

            images =
            [
                new InputCreateMemberImageView
                {
                    ImageUrl = saved.Url,
                    Caption = "Profile",
                    IsPrimary = true,
                    SortOrder = 0,
                    FileSize = saved.FileSize
                },
                ..images
            ];
        }

        var result = await _iMembersRepository.CreateAsync(new InputCreateMember
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
            LocationName = request.LocationName,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Addresses = _iCommonService.ToJson(request.Addresses),
            Images = _iCommonService.ToJson(images.Count == 0 ? null : images),
            Events = _iCommonService.ToJson(request.Events),
            Notes = _iCommonService.ToJson(request.Notes),
            SocialLinks = _iCommonService.ToJson(request.SocialLinks),
            CreatedBy = User.GetUsername()
        });

        _iCommonService.EnsureSuccess(result);
        var profile = await LoadMemberOrThrow(ResolveMemberId(result));

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<OutputGetMember>
        {
            Success = true,
            StatusCode = StatusCodes.Status201Created,
            Message = result.ResponseMessage ?? "Member created successfully.",
            Data = profile,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPut("{id:long}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Update(
        [FromRoute] InputMemberRouteRequestView route,
        [FromForm] InputUpdateMemberView request,
        CancellationToken cancellationToken)
    {
        _iCommonService.ValidateModelState(ModelState);
        await _iAccessAuthorizationService.EnsureCanEditMemberAsync(User, route.Id);

        var images = request.Images;
        if (request.ProfileImage is { Length: > 0 })
        {
            await _iFamilyStorageService.EnsureCanUploadAsync(User.GetFamilyId(), request.ProfileImage.Length);

            var saved = await _iImageFileService.SaveAsync(
                new ImageUploadRequest
                {
                    File = request.ProfileImage,
                    Mode = ImageUploadMode.MemberProfile,
                    FamilyId = User.GetFamilyId()
                },
                Request,
                cancellationToken);

            await _iFamilyStorageService.EnsureCanUploadAsync(User.GetFamilyId(), saved.FileSize);

            images ??= (await _iMembersRepository.GetByIdAsync(new InputGetMember
            {
                FamilyId = User.GetFamilyId(),
                MemberId = route.Id
            }))?.Images
                .Select(img => new InputUpdateMemberImageView
                {
                    Id = img.Id,
                    ImageUrl = img.ImageUrl,
                    Caption = img.Caption,
                    IsPrimary = false,
                    SortOrder = img.SortOrder,
                    FileSize = img.FileSize
                })
                .ToList() ?? [];

            foreach (var img in images)
            {
                img.IsPrimary = false;
            }

            images.Insert(0, new InputUpdateMemberImageView
            {
                ImageUrl = saved.Url,
                Caption = "Profile",
                IsPrimary = true,
                SortOrder = 0,
                FileSize = saved.FileSize
            });
        }

        var result = await _iMembersRepository.UpdateAsync(new InputUpdateMember
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
            LocationName = request.LocationName,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Addresses = _iCommonService.ToJson(request.Addresses),
            Images = _iCommonService.ToJson(images),
            Events = _iCommonService.ToJson(request.Events),
            Notes = _iCommonService.ToJson(request.Notes),
            SocialLinks = _iCommonService.ToJson(request.SocialLinks),
            UpdatedBy = User.GetUsername()
        });

        _iCommonService.EnsureSuccess(result);
        var profile = await LoadMemberOrThrow(route.Id);

        return Ok(new ApiResponse<OutputGetMember>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = result.ResponseMessage ?? "Member updated successfully.",
            Data = profile,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete([FromRoute] InputMemberRouteRequestView request)
    {
        _iCommonService.ValidateModelState(ModelState);
        await _iAccessAuthorizationService.EnsureCanDeleteMemberAsync(User, request.Id);

        var result = await _iMembersRepository.SoftDeleteAsync(new InputDeleteMember
        {
            FamilyId = User.GetFamilyId(),
            MemberId = request.Id,
            DeletedBy = User.GetUsername()
        });

        return _iCommonService.ToActionResult(result, HttpContext.TraceIdentifier);
    }

    [HttpPost("map-spouse")]
    public async Task<IActionResult> MapSpouse([FromBody] InputMapSpouseView request)
    {
        _iCommonService.ValidateModelState(ModelState);
        await _iAccessAuthorizationService.EnsureCanMapSpouseAsync(User, request.MemberId, request.SpouseId);

        var result = await _iMembersRepository.MapSpouseAsync(new InputMapSpouse
        {
            FamilyId = User.GetFamilyId(),
            MemberId = request.MemberId,
            SpouseId = request.SpouseId,
            UpdatedBy = User.GetUsername()
        });

        return _iCommonService.ToActionResult(result, HttpContext.TraceIdentifier);
    }

    private async Task<OutputGetMember> LoadMemberOrThrow(long memberId)
    {
        return await _iMembersRepository.GetByIdAsync(new InputGetMember
        {
            FamilyId = User.GetFamilyId(),
            MemberId = memberId
        }) ?? throw new NotFoundException("Member not found.");
    }

    private static long ResolveMemberId(OutputCreateMember result)
    {
        if (result.Data?.Id > 0)
        {
            return result.Data.Id;
        }

        if (result.ResponseCode > 0)
        {
            return result.ResponseCode;
        }

        throw new BadRequestException(result.ResponseMessage ?? "Member save failed.");
    }
}
