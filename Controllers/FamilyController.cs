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
    private readonly IFamiliesRepository _iFamiliesRepository;
    private readonly IMembersRepository _iMembersRepository;
    private readonly ICommonService _iCommonService;
    private readonly IImageFileService _iImageFileService;
    private readonly IAccessAuthorizationService _iAccessAuthorizationService;

    public FamilyController(
        IFamiliesRepository families,
        IMembersRepository members,
        ICommonService commonService,
        IImageFileService images,
        IAccessAuthorizationService authz)
    {
        _iFamiliesRepository = families;
        _iMembersRepository = members;
        _iCommonService = commonService;
        _iImageFileService = images;
        _iAccessAuthorizationService = authz;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        _iCommonService.ValidateModelState(ModelState);

        var family = await _iFamiliesRepository.GetByIdAsync(new InputGetFamily { Id = User.GetFamilyId() })
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

    [HttpGet("storage")]
    public async Task<IActionResult> GetStorage([FromServices] IFamilyStorageService storage)
    {
        var data = await storage.GetStorageUsageAsync(User.GetFamilyId());

        return Ok(new ApiResponse<OutputFamilyStorageUsage>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Success.",
            Data = data,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPut]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Update([FromForm] InputUpdateFamilyView request, CancellationToken cancellationToken)
    {
        _iCommonService.ValidateModelState(ModelState);
        _iAccessAuthorizationService.EnsureCanEditFamily(User);

        var familyId = User.GetFamilyId();
        var photoUrl = request.PhotoUrl;
        if (request.FamilyPhoto is { Length: > 0 })
        {
            var saved = await _iImageFileService.SaveAsync(
                new ImageUploadRequest
                {
                    File = request.FamilyPhoto,
                    Mode = ImageUploadMode.FamilyLogo,
                    FamilyId = familyId
                },
                Request,
                cancellationToken);
            photoUrl = saved.Url;
        }
        else if (string.IsNullOrWhiteSpace(photoUrl))
        {
            photoUrl = (await _iFamiliesRepository.GetByIdAsync(new InputGetFamily { Id = familyId }))?.PhotoUrl;
        }

        var result = await _iFamiliesRepository.UpdateAsync(new InputUpdateFamily
        {
            Id = familyId,
            FamilyName = request.FamilyName.Trim(),
            Description = request.Description,
            PhotoUrl = photoUrl,
            UpdatedBy = User.GetUsername()
        });

        return _iCommonService.ToActionResult(result, HttpContext.TraceIdentifier);
    }

    [HttpPut("cover")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UpdateCover(
        [FromForm] InputUpdateFamilyCoverView request,
        CancellationToken cancellationToken)
    {
        _iCommonService.ValidateModelState(ModelState);
        _iAccessAuthorizationService.EnsureCanEditFamily(User);

        if (request.FamilyCover is not { Length: > 0 })
        {
            throw new BadRequestException("Please choose a cover image.");
        }

        var familyId = User.GetFamilyId();
        var saved = await _iImageFileService.SaveAsync(
            new ImageUploadRequest
            {
                File = request.FamilyCover,
                Mode = ImageUploadMode.FamilyCover,
                FamilyId = familyId
            },
            Request,
            cancellationToken);
        var coverUrl = saved.Url;

        var result = await _iFamiliesRepository.UpdateCoverAsync(new InputUpdateFamilyCover
        {
            Id = familyId,
            CoverUrl = coverUrl,
            UpdatedBy = User.GetUsername()
        });

        return _iCommonService.ToActionResult(result, HttpContext.TraceIdentifier);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        _iCommonService.ValidateModelState(ModelState);

        var data = await _iMembersRepository.GetDashboardAsync(new InputMemberDashboard
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
        _iCommonService.ValidateModelState(ModelState);

        var data = await _iMembersRepository.GetTimelineAsync(new InputMemberTimeline
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
