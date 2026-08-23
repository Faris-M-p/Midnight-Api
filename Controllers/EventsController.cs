using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MidnightApi.Auth;
using MidnightApi.Exceptions;
using MidnightApi.Models;
using MidnightApi.Repositories.Interfaces;
using MidnightApi.Services.Interfaces;

namespace MidnightApi.Controllers;

[ApiController]
[Route("api/events")]
[Tags("Events")]
[Authorize]
public class EventsController : ControllerBase
{
    private const long MultipartLimitBytes = 11L * 1024 * 1024;

    private readonly IEventsRepository _iEventsRepository;
    private readonly ICommonService _iCommonService;
    private readonly IAccessAuthorizationService _iAccessAuthorizationService;
    private readonly IFileStorageService _iFileStorageService;
    private readonly IFamilyStorageService _iFamilyStorageService;

    public EventsController(
        IEventsRepository eventsRepository,
        ICommonService commonService,
        IAccessAuthorizationService authz,
        IFileStorageService fileStorageService,
        IFamilyStorageService familyStorageService)
    {
        _iEventsRepository = eventsRepository;
        _iCommonService = commonService;
        _iAccessAuthorizationService = authz;
        _iFileStorageService = fileStorageService;
        _iFamilyStorageService = familyStorageService;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] InputEventListQueryView query)
    {
        _iCommonService.ValidateModelState(ModelState);

        var data = await _iEventsRepository.GetListAsync(new InputEventList
        {
            FamilyId = User.GetFamilyId(),
            Search = query.Search,
            SortBy = query.SortBy,
            Page = query.Page,
            PageSize = query.PageSize
        }) ?? new OutputPagedEvents();

        return Ok(new ApiResponse<OutputPagedEvents>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Success.",
            Data = data,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById([FromRoute] InputEventRouteRequestView route)
    {
        _iCommonService.ValidateModelState(ModelState);

        var data = await _iEventsRepository.GetByIdAsync(new InputGetEvent
        {
            FamilyId = User.GetFamilyId(),
            Id = route.Id
        }) ?? throw new NotFoundException("Event not found.");

        return Ok(new ApiResponse<OutputGetEvent>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Success.",
            Data = data,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MultipartLimitBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MultipartLimitBytes)]
    public async Task<IActionResult> Create(
        [FromForm] InputCreateEventView request,
        CancellationToken cancellationToken)
    {
        _iCommonService.ValidateModelState(ModelState);
        _iAccessAuthorizationService.EnsureCanEditFamily(User);

        var familyId = User.GetFamilyId();
        var username = User.GetUsername();

        FileStorageUploadResult? uploadedCover = null;

        try
        {
            if (request.CoverImage is { Length: > 0 })
            {
                await _iFamilyStorageService.EnsureCanUploadAsync(familyId, request.CoverImage.Length);

                await using (var stream = request.CoverImage.OpenReadStream())
                {
                    uploadedCover = await _iFileStorageService.UploadAsync(new FileStorageUploadRequest
                    {
                        FamilyId = familyId,
                        EventId = 0,
                        Content = stream,
                        OriginalFileName = request.CoverImage.FileName,
                        ContentType = request.CoverImage.ContentType,
                        ContentLength = request.CoverImage.Length
                    }, cancellationToken);
                }

                await _iFamilyStorageService.EnsureCanUploadAsync(familyId, uploadedCover.FileSize);
            }

            var result = await _iEventsRepository.CreateAsync(new InputCreateEvent
            {
                FamilyId = familyId,
                CreatedBy = username,
                Title = request.Title,
                EventType = request.EventType,
                EventDateTime = request.EventDateTime.ToUniversalTime(),
                Location = request.LocationName,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Description = request.Description,
                MemberIds = request.MemberIds,
                CoverImageUrl = uploadedCover?.FileUrl,
                CoverStorageKey = uploadedCover?.StorageKey,
                CoverFileSize = uploadedCover?.FileSize ?? 0,
                CoverMimeType = uploadedCover?.MimeType
            });
            _iCommonService.EnsureSuccess(result);

            var data = result.Data ?? throw new NotFoundException("Event not found.");

            return StatusCode(StatusCodes.Status201Created, new ApiResponse<OutputGetEvent>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Event created successfully.",
                Data = data,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch
        {
            if (uploadedCover is not null
                && !string.IsNullOrWhiteSpace(uploadedCover.StorageKey))
            {
                await _iFileStorageService.DeleteAsync(uploadedCover.StorageKey, cancellationToken);
            }

            throw;
        }
    }

    [HttpPut("{id:long}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MultipartLimitBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MultipartLimitBytes)]
    public async Task<IActionResult> Update(
        [FromRoute] InputEventRouteRequestView route,
        [FromForm] InputUpdateEventView request,
        CancellationToken cancellationToken)
    {
        _iCommonService.ValidateModelState(ModelState);
        _iAccessAuthorizationService.EnsureCanEditFamily(User);

        var familyId = User.GetFamilyId();
        var username = User.GetUsername();

        FileStorageUploadResult? uploadedCover = null;

        try
        {
            if (!request.RemoveCover && request.CoverImage is { Length: > 0 })
            {
                await _iFamilyStorageService.EnsureCanUploadAsync(familyId, request.CoverImage.Length);

                await using (var stream = request.CoverImage.OpenReadStream())
                {
                    uploadedCover = await _iFileStorageService.UploadAsync(new FileStorageUploadRequest
                    {
                        FamilyId = familyId,
                        EventId = 0,
                        Content = stream,
                        OriginalFileName = request.CoverImage.FileName,
                        ContentType = request.CoverImage.ContentType,
                        ContentLength = request.CoverImage.Length
                    }, cancellationToken);
                }

                await _iFamilyStorageService.EnsureCanUploadAsync(familyId, uploadedCover.FileSize);
            }

            var result = await _iEventsRepository.UpdateAsync(new InputUpdateEvent
            {
                FamilyId = familyId,
                Id = route.Id,
                UpdatedBy = username,
                Title = request.Title,
                EventType = request.EventType,
                EventDateTime = request.EventDateTime.ToUniversalTime(),
                Location = request.LocationName,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Description = request.Description,
                MemberIds = request.MemberIds,
                RemoveCover = request.RemoveCover,
                CoverImageUrl = uploadedCover?.FileUrl,
                CoverStorageKey = uploadedCover?.StorageKey,
                CoverFileSize = uploadedCover?.FileSize ?? 0,
                CoverMimeType = uploadedCover?.MimeType
            });
            _iCommonService.EnsureSuccess(result);

            var data = result.Data ?? throw new NotFoundException("Event not found.");

            if ((request.RemoveCover || uploadedCover is not null)
                && !string.IsNullOrWhiteSpace(data.PreviousStorageKey)
                && !string.Equals(data.PreviousStorageKey, uploadedCover?.StorageKey, StringComparison.Ordinal))
            {
                await _iFileStorageService.DeleteAsync(data.PreviousStorageKey, cancellationToken);
            }

            return Ok(new ApiResponse<OutputGetEvent>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Event updated successfully.",
                Data = data,
                TraceId = HttpContext.TraceIdentifier
            });
        }
        catch
        {
            if (uploadedCover is not null
                && !string.IsNullOrWhiteSpace(uploadedCover.StorageKey))
            {
                await _iFileStorageService.DeleteAsync(uploadedCover.StorageKey, cancellationToken);
            }

            throw;
        }
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(
        [FromRoute] InputEventRouteRequestView route,
        CancellationToken cancellationToken)
    {
        _iCommonService.ValidateModelState(ModelState);
        _iAccessAuthorizationService.EnsureCanEditFamily(User);

        var result = await _iEventsRepository.SoftDeleteAsync(new InputDeleteEvent
        {
            FamilyId = User.GetFamilyId(),
            Id = route.Id,
            CancelledBy = User.GetUsername()
        });
        _iCommonService.EnsureSuccess(result);

        var previousStorageKey = result.Data?.PreviousStorageKey;
        if (!string.IsNullOrWhiteSpace(previousStorageKey))
        {
            await _iFileStorageService.DeleteAsync(previousStorageKey, cancellationToken);
        }

        return Ok(new ApiResponse<object?>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Event deleted successfully.",
            Data = null,
            TraceId = HttpContext.TraceIdentifier
        });
    }
}
