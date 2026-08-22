using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using MidnightApi.DataAccess;
using MidnightApi.Validation.CustomModelValidation;

namespace MidnightApi.Models;

/// <summary>
/// Entity shape for table "Events".
/// </summary>
public class FamilyEvent
{
    public long ID_Events { get; set; }
    public long FK_Families { get; set; }
    public string Title { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset EventDateTime { get; set; }
    public string? LocationName { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? CoverStorageKey { get; set; }
    public long CoverFileSize { get; set; }
    public string? CoverMimeType { get; set; }
    public string CreatedBy { get; set; } = "system";
    public DateTime CreatedOn { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public bool IsCancelled { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? CancelledOn { get; set; }
}

// --- API query / body views ---

public class InputEventListQueryView
{
    [StringLength(200)]
    [TrimmedString]
    public string? Search { get; set; }

    /// <summary>date | recent | title</summary>
    [StringLength(20)]
    [TrimmedString]
    public string SortBy { get; set; } = "date";

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 200)]
    public int PageSize { get; set; } = 100;
}

public class InputEventRouteRequestView
{
    [Range(1, long.MaxValue)]
    public long Id { get; set; }
}

public class InputCreateEventView
{
    [Display(Name = "Title")]
    [Required, StringLength(200)]
    [TrimmedString]
    [NoScriptTags]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Event Type")]
    [Required, StringLength(50)]
    [TrimmedString]
    [MidnightApi.Validation.CustomModelValidation.AllowedValues("birthday", "anniversary", "memorial", "gathering", "achievement", "custom")]
    public string EventType { get; set; } = string.Empty;

    [Display(Name = "Event Date & Time")]
    [Required]
    public DateTimeOffset EventDateTime { get; set; }

    [Display(Name = "Location")]
    [StringLength(500)]
    [TrimmedString]
    [NoScriptTags]
    public string? LocationName { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [Display(Name = "Description")]
    [StringLength(4000)]
    [NoScriptTags]
    public string? Description { get; set; }

    [Display(Name = "Members")]
    public List<long>? MemberIds { get; set; }

    [Display(Name = "Cover Image")]
    public IFormFile? CoverImage { get; set; }
}

public class InputUpdateEventView
{
    [Display(Name = "Title")]
    [Required, StringLength(200)]
    [TrimmedString]
    [NoScriptTags]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Event Type")]
    [Required, StringLength(50)]
    [TrimmedString]
    [MidnightApi.Validation.CustomModelValidation.AllowedValues("birthday", "anniversary", "memorial", "gathering", "achievement", "custom")]
    public string EventType { get; set; } = string.Empty;

    [Display(Name = "Event Date & Time")]
    [Required]
    public DateTimeOffset EventDateTime { get; set; }

    [Display(Name = "Location")]
    [StringLength(500)]
    [TrimmedString]
    [NoScriptTags]
    public string? LocationName { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [Display(Name = "Description")]
    [StringLength(4000)]
    [NoScriptTags]
    public string? Description { get; set; }

    [Display(Name = "Members")]
    public List<long>? MemberIds { get; set; }

    [Display(Name = "Cover Image")]
    public IFormFile? CoverImage { get; set; }

    public bool RemoveCover { get; set; }
}

// --- Repository inputs ---

public class InputEventList : CommonModel.BaseFamilyInput
{
    [DbParam("p_Search")]
    public string? Search { get; set; }

    [DbParam("p_SortBy")]
    public string SortBy { get; set; } = "date";

    [DbParam("p_Page")]
    public int Page { get; set; } = 1;

    [DbParam("p_PageSize")]
    public int PageSize { get; set; } = 100;
}

public class InputGetEvent : CommonModel.BaseFamilyInput
{
    [DbParam("p_ID_Events")]
    public long Id { get; set; }
}

public class InputCreateEvent : CommonModel.BaseFamilyInput
{
    [DbParam("p_CreatedBy")]
    public string CreatedBy { get; set; } = string.Empty;

    [DbParam("p_Title")]
    public string Title { get; set; } = string.Empty;

    [DbParam("p_EventType")]
    public string EventType { get; set; } = string.Empty;

    [DbParam("p_EventDateTime")]
    public DateTimeOffset EventDateTime { get; set; }

    [DbParam("p_LocationName")]
    public string? Location { get; set; }

    [DbParam("p_Latitude")]
    public double? Latitude { get; set; }

    [DbParam("p_Longitude")]
    public double? Longitude { get; set; }

    [DbParam("p_Description")]
    public string? Description { get; set; }

    [DbParam("p_CoverImageUrl")]
    public string? CoverImageUrl { get; set; }

    [DbParam("p_CoverStorageKey")]
    public string? CoverStorageKey { get; set; }

    [DbParam("p_CoverFileSize")]
    public long CoverFileSize { get; set; }

    [DbParam("p_CoverMimeType")]
    public string? CoverMimeType { get; set; }

    [DbParam("p_MemberIds")]
    public string MemberIdsJson { get; set; } = "[]";

    public List<long>? MemberIds
    {
        set => MemberIdsJson = EventMemberIdsJson.FromIds(value);
    }
}

public class InputUpdateEvent : CommonModel.BaseFamilyInput
{
    [DbParam("p_ID_Events")]
    public long Id { get; set; }

    [DbParam("p_UpdatedBy")]
    public string UpdatedBy { get; set; } = string.Empty;

    [DbParam("p_Title")]
    public string Title { get; set; } = string.Empty;

    [DbParam("p_EventType")]
    public string EventType { get; set; } = string.Empty;

    [DbParam("p_EventDateTime")]
    public DateTimeOffset EventDateTime { get; set; }

    [DbParam("p_LocationName")]
    public string? Location { get; set; }

    [DbParam("p_Latitude")]
    public double? Latitude { get; set; }

    [DbParam("p_Longitude")]
    public double? Longitude { get; set; }

    [DbParam("p_Description")]
    public string? Description { get; set; }

    [DbParam("p_RemoveCover")]
    public bool RemoveCover { get; set; }

    [DbParam("p_CoverImageUrl")]
    public string? CoverImageUrl { get; set; }

    [DbParam("p_CoverStorageKey")]
    public string? CoverStorageKey { get; set; }

    [DbParam("p_CoverFileSize")]
    public long CoverFileSize { get; set; }

    [DbParam("p_CoverMimeType")]
    public string? CoverMimeType { get; set; }

    [DbParam("p_MemberIds")]
    public string MemberIdsJson { get; set; } = "[]";

    public List<long>? MemberIds
    {
        set => MemberIdsJson = EventMemberIdsJson.FromIds(value);
    }
}

public class InputDeleteEvent : CommonModel.BaseFamilyInput
{
    [DbParam("p_ID_Events")]
    public long Id { get; set; }

    [DbParam("p_CancelledBy")]
    public string CancelledBy { get; set; } = string.Empty;
}

public class InputEventCoverCommit : CommonModel.BaseFamilyInput
{
    [DbParam("p_ID_Events")]
    public long Id { get; set; }

    [DbParam("p_CoverImageUrl")]
    public string CoverImageUrl { get; set; } = string.Empty;

    [DbParam("p_CoverStorageKey")]
    public string CoverStorageKey { get; set; } = string.Empty;

    [DbParam("p_CoverFileSize")]
    public long CoverFileSize { get; set; }

    [DbParam("p_CoverMimeType")]
    public string? CoverMimeType { get; set; }

    [DbParam("p_UpdatedBy")]
    public string UpdatedBy { get; set; } = string.Empty;
}

public class InputEventCoverRemove : CommonModel.BaseFamilyInput
{
    [DbParam("p_ID_Events")]
    public long Id { get; set; }

    [DbParam("p_UpdatedBy")]
    public string UpdatedBy { get; set; } = string.Empty;
}

// --- Outputs ---

public class OutputEventMemberItem
{
    public long Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
}

public class OutputEventListItem
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset EventDateTime { get; set; }
    public string? LocationName { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public int MemberCount { get; set; }
    public string? MemberNames { get; set; }
    public List<OutputEventMemberItem> Members { get; set; } = [];
    public DateTime CreatedOn { get; set; }
}

public class OutputPagedEvents
{
    public List<OutputEventListItem> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class OutputGetEvent
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset EventDateTime { get; set; }
    public string? LocationName { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? CoverStorageKey { get; set; }
    public long CoverFileSize { get; set; }
    public string? CoverMimeType { get; set; }
    public List<OutputEventMemberItem> Members { get; set; } = [];
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
}

public class OutputCreateEvent : CommonResponse<OutputGetEvent>
{
}

public class OutputUpdateEvent : CommonResponse<OutputGetEvent>
{
}

public class OutputDeleteEvent : CommonResponse<IdResponse>
{
}

public class OutputEventCoverAction : CommonResponse<EventCoverActionData>
{
}

public class EventCoverActionData
{
    public long Id { get; set; }
    public string? PreviousStorageKey { get; set; }
}

public static class EventMemberIdsJson
{
    public static string FromIds(IEnumerable<long>? memberIds)
    {
        var ids = (memberIds ?? [])
            .Where(id => id > 0)
            .Distinct()
            .ToArray();
        return JsonSerializer.Serialize(ids);
    }
}
