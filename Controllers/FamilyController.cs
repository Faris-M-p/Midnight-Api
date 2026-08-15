using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MidnightApi.Auth;
using MidnightApi.Exceptions;
using MidnightApi.Models;
using MidnightApi.Repositories.Interfaces;
using MidnightApi.Services.Interfaces;

namespace MidnightApi.Controllers;

[ApiController]
[Route("api/family")]
[Tags("Family")]
[Authorize]
public class FamilyController : ControllerBase
{
    private readonly IFamiliesRepository _families;
    private readonly IMembersRepository _members;
    private readonly ICommonService _commonService;
    private readonly IImageFileService _images;
    private readonly IAccessAuthorizationService _authz;

    public FamilyController(
        IFamiliesRepository families,
        IMembersRepository members,
        ICommonService commonService,
        IImageFileService images,
        IAccessAuthorizationService authz)
    {
        _families = families;
        _members = members;
        _commonService = commonService;
        _images = images;
        _authz = authz;
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
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Update([FromForm] InputUpdateFamilyView request, CancellationToken cancellationToken)
    {
        _commonService.ValidateModelState(ModelState);
        _authz.EnsureCanEditFamily(User);

        var familyId = User.GetFamilyId();
        var photoUrl = request.PhotoUrl;
        if (request.FamilyPhoto is { Length: > 0 })
        {
            photoUrl = await _images.SaveAsync(
                new ImageUploadRequest
                {
                    File = request.FamilyPhoto,
                    Mode = ImageUploadMode.FamilyLogo,
                    FamilyId = familyId
                },
                Request,
                cancellationToken);
        }
        else if (string.IsNullOrWhiteSpace(photoUrl))
        {
            photoUrl = (await _families.GetByIdAsync(new InputGetFamily { Id = familyId }))?.PhotoUrl;
        }

        var result = await _families.UpdateAsync(new InputUpdateFamily
        {
            Id = familyId,
            FamilyName = request.FamilyName.Trim(),
            Description = request.Description,
            PhotoUrl = photoUrl,
            UpdatedBy = User.GetUsername()
        });

        return _commonService.ToActionResult(result, HttpContext.TraceIdentifier);
    }

    [HttpPut("cover")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UpdateCover(
        [FromForm] InputUpdateFamilyCoverView request,
        CancellationToken cancellationToken)
    {
        _commonService.ValidateModelState(ModelState);
        _authz.EnsureCanEditFamily(User);

        if (request.FamilyCover is not { Length: > 0 })
        {
            throw new BadRequestException("Please choose a cover image.");
        }

        var familyId = User.GetFamilyId();
        var coverUrl = await _images.SaveAsync(
            new ImageUploadRequest
            {
                File = request.FamilyCover,
                Mode = ImageUploadMode.FamilyCover,
                FamilyId = familyId
            },
            Request,
            cancellationToken);

        var result = await _families.UpdateCoverAsync(new InputUpdateFamilyCover
        {
            Id = familyId,
            CoverUrl = coverUrl,
            UpdatedBy = User.GetUsername()
        });

        return _commonService.ToActionResult(result, HttpContext.TraceIdentifier);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        _commonService.ValidateModelState(ModelState);

        var data = await _members.GetDashboardAsync(new InputMemberDashboard
        {
            FamilyId = User.GetFamilyId()
        }) ?? throw new NotFoundException("Family not found.");

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
