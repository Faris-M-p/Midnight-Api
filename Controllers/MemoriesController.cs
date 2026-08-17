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

    private readonly IMemoriesService _memories;
    private readonly ICommonService _commonService;
    private readonly IAccessAuthorizationService _authz;

    public MemoriesController(
        IMemoriesService memories,
        ICommonService commonService,
        IAccessAuthorizationService authz)
    {
        _memories = memories;
        _commonService = commonService;
        _authz = authz;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] InputMemoryListQueryView query)
    {
        _commonService.ValidateModelState(ModelState);

        var data = await _memories.ListAsync(User.GetFamilyId(), query);

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
        _commonService.ValidateModelState(ModelState);

        var data = await _memories.GetAsync(User.GetFamilyId(), route.Id);

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
        _commonService.ValidateModelState(ModelState);
        _authz.EnsureCanEditFamily(User);

        var data = await _memories.CreateAsync(
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
        _commonService.ValidateModelState(ModelState);
        _authz.EnsureCanEditFamily(User);

        var data = await _memories.UpdateAsync(
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
        _commonService.ValidateModelState(ModelState);
        _authz.EnsureCanEditFamily(User);

        await _memories.DeleteAsync(
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
        _commonService.ValidateModelState(ModelState);
        _authz.EnsureCanEditFamily(User);

        var data = await _memories.UploadImageAsync(
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
        _commonService.ValidateModelState(ModelState);
        _authz.EnsureCanEditFamily(User);

        var data = await _memories.DeleteImageAsync(
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
        _commonService.ValidateModelState(ModelState);
        _authz.EnsureCanEditFamily(User);

        var data = await _memories.SetCoverAsync(
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
