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
    private readonly IEventsRepository _iEventsRepository;
    private readonly ICommonService _iCommonService;
    private readonly IAccessAuthorizationService _iAccessAuthorizationService;

    public EventsController(
        IEventsRepository events,
        ICommonService commonService,
        IAccessAuthorizationService authz)
    {
        _iEventsRepository = events;
        _iCommonService = commonService;
        _iAccessAuthorizationService = authz;
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
    public async Task<IActionResult> Create([FromBody] InputCreateEventView request)
    {
        _iCommonService.ValidateModelState(ModelState);
        _iAccessAuthorizationService.EnsureCanEditFamily(User);

        var data = await _iEventsService.CreateAsync(
        {
            FamilyId = User.GetFamilyId(),
            Title = request.Title,
            EventType = request.EventType,
            EventDate = request.EventDate.Date,
            EventTime = TrimOptional(request.EventTime),
            Location = request.Location,
            Description = request.Description,
            MemberIds = EventMemberIdsJson.FromIds(request.MemberIds),
            CreatedBy = User.GetUsername()
        });
        _iCommonService.EnsureSuccess(create);

        var data = await _iEventsRepository.GetByIdAsync(new InputGetEvent
        {
            FamilyId = User.GetFamilyId(),
            Id = create.ResponseCode
        }) ?? throw new NotFoundException("Event not found.");

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<OutputGetEvent>
        {
            Success = true,
            StatusCode = StatusCodes.Status201Created,
            Message = "Event created successfully.",
            Data = data,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        [FromRoute] InputEventRouteRequestView route,
        [FromBody] InputUpdateEventView request)
    {
        _iCommonService.ValidateModelState(ModelState);
        _iAccessAuthorizationService.EnsureCanEditFamily(User);

        _ = await _iEventsRepository.GetByIdAsync(new InputGetEvent
        {
            FamilyId = User.GetFamilyId(),
            Id = route.Id
        }) ?? throw new NotFoundException("Event not found.");

        var update = await _iEventsRepository.UpdateAsync(new InputUpdateEvent
        {
            FamilyId = User.GetFamilyId(),
            Id = route.Id,
            Title = request.Title,
            EventType = request.EventType,
            EventDate = request.EventDate.Date,
            EventTime = TrimOptional(request.EventTime),
            Location = request.Location,
            Description = request.Description,
            MemberIds = EventMemberIdsJson.FromIds(request.MemberIds),
            UpdatedBy = User.GetUsername()
        });
        _iCommonService.EnsureSuccess(update);

        var data = await _iEventsRepository.GetByIdAsync(new InputGetEvent
        {
            FamilyId = User.GetFamilyId(),
            Id = route.Id
        }) ?? throw new NotFoundException("Event not found.");

        return Ok(new ApiResponse<OutputGetEvent>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Event updated successfully.",
            Data = data,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete([FromRoute] InputEventRouteRequestView route)
    {
        _iCommonService.ValidateModelState(ModelState);
        _iAccessAuthorizationService.EnsureCanEditFamily(User);

        _ = await _iEventsRepository.GetByIdAsync(new InputGetEvent
        {
            FamilyId = User.GetFamilyId(),
            Id = route.Id
        }) ?? throw new NotFoundException("Event not found.");

        var result = await _iEventsRepository.SoftDeleteAsync(new InputDeleteEvent
        {
            FamilyId = User.GetFamilyId(),
            Id = route.Id,
            CancelledBy = User.GetUsername()
        });
        _iCommonService.EnsureSuccess(result);

        return Ok(new ApiResponse<object?>
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = "Event deleted successfully.",
            Data = null,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    private static string? TrimOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
