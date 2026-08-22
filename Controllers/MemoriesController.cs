using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MidnightApi.Auth;
using MidnightApi.Models;
using MidnightApi.Services.Interfaces;

namespace MidnightApi.Controllers;

[ApiController]
[Route("api/memories")]
[Tags("Memories")]
[Authorize]
public class MemoriesController : ControllerBase
{
    private const long CreateMultipartLimitBytes = 110L * 1024 * 1024;

    private readonly IMemoriesService _iMemoriesService;
    private readonly ICommonService _iCommonService;
    private readonly IAccessAuthorizationService _iAccessAuthorizationService;

    public MemoriesController(
        IMemoriesService memories,
        ICommonService commonService,
        IAccessAuthorizationService authz)
    {
        _iMemoriesService = memories;
        _iCommonService = commonService;
        _iAccessAuthorizationService = authz;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] InputMemoryListQueryView query)
    {
        _iCommonService.ValidateModelState(ModelState);

        var data = await _iMemoriesService.ListAsync(User.GetFamilyId(), query);

        return Ok(new ApiResponse<OutputPagedMemories>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Success.",
            Data = data,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById([FromRoute] InputMemoryRouteRequestView route)
    {
        _iCommonService.ValidateModelState(ModelState);

        var data = await _iMemoriesService.GetAsync(User.GetFamilyId(), route.Id);

        return Ok(new ApiResponse<OutputGetMemory>
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
    [RequestSizeLimit(CreateMultipartLimitBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = CreateMultipartLimitBytes)]
    public async Task<IActionResult> Create(
        [FromForm] InputCreateMemoryView request,
        CancellationToken cancellationToken)
    {
        _iCommonService.ValidateModelState(ModelState);
        _iAccessAuthorizationService.EnsureCanEditFamily(User);

        var data = await _iMemoriesService.CreateAsync(
            User.GetFamilyId(),
            User.GetUsername(),
            request,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<OutputGetMemory>
        {
            Success = true,
            StatusCode = StatusCodes.Status201Created,
            Message = "Memory created successfully.",
            Data = data,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPut("{id:long}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Update(
        [FromRoute] InputMemoryRouteRequestView route,
        [FromForm] InputUpdateMemoryView request,
        CancellationToken cancellationToken)
    {
        _iCommonService.ValidateModelState(ModelState);
        _iAccessAuthorizationService.EnsureCanEditFamily(User);

        var data = await _iMemoriesService.UpdateAsync(
            User.GetFamilyId(),
            route.Id,
            User.GetUsername(),
            request,
            cancellationToken);

        return Ok(new ApiResponse<OutputGetMemory>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Memory updated successfully.",
            Data = data,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(
        [FromRoute] InputMemoryRouteRequestView route,
        CancellationToken cancellationToken)
    {
        _iCommonService.ValidateModelState(ModelState);
        _iAccessAuthorizationService.EnsureCanEditFamily(User);

        await _iMemoriesService.DeleteAsync(
            User.GetFamilyId(),
            route.Id,
            User.GetUsername(),
            cancellationToken);

        return Ok(new ApiResponse<object?>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Memory deleted successfully.",
            Data = null,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPost("{id:long}/images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(
        [FromRoute] InputMemoryImageRouteRequestView route,
        [FromForm] InputUploadMemoryImageView request,
        CancellationToken cancellationToken)
    {
        _iCommonService.ValidateModelState(ModelState);
        _iAccessAuthorizationService.EnsureCanEditFamily(User);

        var data = await _iMemoriesService.UploadImageAsync(
            User.GetFamilyId(),
            route.Id,
            User.GetUsername(),
            request,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<OutputMemoryImageAction>
        {
            Success = true,
            StatusCode = StatusCodes.Status201Created,
            Message = "Image uploaded successfully.",
            Data = data,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpDelete("{id:long}/images/{imageId:long}")]
    public async Task<IActionResult> DeleteImage(
        [FromRoute] InputMemoryImageDeleteRouteRequestView route,
        CancellationToken cancellationToken)
    {
        _iCommonService.ValidateModelState(ModelState);
        _iAccessAuthorizationService.EnsureCanEditFamily(User);

        var data = await _iMemoriesService.DeleteImageAsync(
            User.GetFamilyId(),
            route.Id,
            route.ImageId,
            User.GetUsername(),
            cancellationToken);

        return Ok(new ApiResponse<OutputMemoryImageAction>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Image deleted successfully.",
            Data = data,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPut("{id:long}/cover")]
    public async Task<IActionResult> SetCover(
        [FromRoute] InputMemoryImageRouteRequestView route,
        [FromBody] InputSetMemoryCoverView request)
    {
        _iCommonService.ValidateModelState(ModelState);
        _iAccessAuthorizationService.EnsureCanEditFamily(User);

        var data = await _iMemoriesService.SetCoverAsync(
            User.GetFamilyId(),
            route.Id,
            request.ImageId,
            User.GetUsername());

        return Ok(new ApiResponse<OutputGetMemory>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Cover image updated successfully.",
            Data = data,
            TraceId = HttpContext.TraceIdentifier
        });
    }
}
