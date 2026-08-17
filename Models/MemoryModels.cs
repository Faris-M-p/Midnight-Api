using System.ComponentModel.DataAnnotations;
using MidnightApi.DataAccess;
using MidnightApi.Validation.CustomModelValidation;

namespace MidnightApi.Models;

/// <summary>
/// Entity shape for table "Memories".
/// </summary>
public class Memory
{
    public long ID_Memories { get; set; }
    public long FK_Families { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime MemoryDate { get; set; }
    public string? Location { get; set; }
    public string? CoverImageUrl { get; set; }
    public string CreatedBy { get; set; } = "system";
    public DateTime CreatedOn { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public bool IsCancelled { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? CancelledOn { get; set; }
}

/// <summary>
/// Entity shape for table "MemoryImages".
/// </summary>
public class MemoryImage
{
    public long ID_MemoryImages { get; set; }
    public long FK_Memories { get; set; }
    public long FK_Families { get; set; }
    public string? ImageUrl { get; set; }
    public string? StorageKey { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    public long FileSize { get; set; }
    public int SortOrder { get; set; }
    public bool IsCoverImage { get; set; }
    public string CreatedBy { get; set; } = "system";
    public DateTime CreatedOn { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public bool IsCancelled { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? CancelledOn { get; set; }
}

// --- API query / form views ---

public class InputMemoryListQueryView
{
    [StringLength(200)]
    [TrimmedString]
    public string? Search { get; set; }

    /// <summary>recent | oldest | title</summary>
    [StringLength(20)]
    [TrimmedString]
    public string SortBy { get; set; } = "recent";

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 12;
}

public class InputMemoryRouteRequestView
{
    [Range(1, long.MaxValue)]
    public long Id { get; set; }
}

public class InputMemoryImageRouteRequestView
{
    [Range(1, long.MaxValue)]
    public long Id { get; set; }
}

public class InputMemoryImageDeleteRouteRequestView
{
    [Range(1, long.MaxValue)]
    public long Id { get; set; }

    [Range(1, long.MaxValue)]
    public long ImageId { get; set; }
}

public class InputUploadMemoryImageView
{
    [Display(Name = "Image")]
    [Required(ErrorMessage = "Image is required.")]
    public IFormFile? Image { get; set; }
}

public class InputSetMemoryCoverView
{
    [Display(Name = "Image Id")]
    [Range(1, long.MaxValue)]
    public long ImageId { get; set; }

    /// <summary>Accepted for older clients that still post MediaId.</summary>
    [Range(1, long.MaxValue)]
    public long MediaId
    {
        get => ImageId;
        set
        {
            if (value > 0 && ImageId <= 0)
            {
                ImageId = value;
            }
        }
    }
}

public class InputCreateMemoryView
{
    [Display(Name = "Title")]
    [Required, StringLength(200)]
    [TrimmedString]
    [NoScriptTags]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Description")]
    [Required]
    [NoScriptTags]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Memory Date")]
    [Required]
    public DateTime MemoryDate { get; set; }

    [Display(Name = "Location")]
    [StringLength(200)]
    [TrimmedString]
    [NoScriptTags]
    public string? Location { get; set; }

    [Display(Name = "Cover Image")]
    [Required(ErrorMessage = "Cover image is required.")]
    public IFormFile? CoverImage { get; set; }

    [Display(Name = "Images")]
    public List<IFormFile>? Images { get; set; }
}

public class InputUpdateMemoryView
{
    [Display(Name = "Title")]
    [Required, StringLength(200)]
    [TrimmedString]
    [NoScriptTags]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Description")]
    [Required]
    [NoScriptTags]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Memory Date")]
    [Required]
    public DateTime MemoryDate { get; set; }

    [Display(Name = "Location")]
    [StringLength(200)]
    [TrimmedString]
    [NoScriptTags]
    public string? Location { get; set; }

    [Display(Name = "Cover Image")]
    public IFormFile? CoverImage { get; set; }
}

// --- DB inputs ---

public class InputMemoryList
{
    [DbParam("p_FK_Families")]
    public long FamilyId { get; set; }

    [DbParam("p_Search")]
    public string? Search { get; set; }

    [DbParam("p_SortBy")]
    public string SortBy { get; set; } = "recent";

    [DbParam("p_Page")]
    public int Page { get; set; } = 1;

    [DbParam("p_PageSize")]
    public int PageSize { get; set; } = 12;
}

public class InputGetMemory
{
    [DbParam("p_FK_Families")]
    public long FamilyId { get; set; }

    [DbParam("p_ID_Memories")]
    public long Id { get; set; }
}

public class InputCreateMemory
{
    [DbParam("p_FK_Families")]
    public long FamilyId { get; set; }

    [DbParam("p_Title")]
    public string Title { get; set; } = string.Empty;

    [DbParam("p_Description")]
    public string? Description { get; set; }

    [DbParam("p_MemoryDate")]
    public DateTime MemoryDate { get; set; }

    [DbParam("p_Location")]
    public string? Location { get; set; }

    [DbParam("p_CreatedBy")]
    public string CreatedBy { get; set; } = string.Empty;
}

public class InputUpdateMemory
{
    [DbParam("p_FK_Families")]
    public long FamilyId { get; set; }

    [DbParam("p_ID_Memories")]
    public long Id { get; set; }

    [DbParam("p_Title")]
    public string Title { get; set; } = string.Empty;

    [DbParam("p_Description")]
    public string? Description { get; set; }

    [DbParam("p_MemoryDate")]
    public DateTime MemoryDate { get; set; }

    [DbParam("p_Location")]
    public string? Location { get; set; }

    [DbParam("p_UpdatedBy")]
    public string UpdatedBy { get; set; } = string.Empty;
}

public class InputDeleteMemory
{
    [DbParam("p_FK_Families")]
    public long FamilyId { get; set; }

    [DbParam("p_ID_Memories")]
    public long Id { get; set; }

    [DbParam("p_CancelledBy")]
    public string CancelledBy { get; set; } = string.Empty;
}

public class InputMemoryImageUploadContext
{
    [DbParam("p_FK_Families")]
    public long FamilyId { get; set; }

    [DbParam("p_ID_Memories")]
    public long MemoryId { get; set; }
}

public class InputMemoryImageCommit
{
    [DbParam("p_FK_Families")]
    public long FamilyId { get; set; }

    [DbParam("p_FK_Memories")]
    public long MemoryId { get; set; }

    [DbParam("p_FileName")]
    public string? FileName { get; set; }

    [DbParam("p_StorageKey")]
    public string StorageKey { get; set; } = string.Empty;

    [DbParam("p_ImageUrl")]
    public string? ImageUrl { get; set; }

    [DbParam("p_MimeType")]
    public string? MimeType { get; set; }

    [DbParam("p_FileSize")]
    public long FileSize { get; set; }

    [DbParam("p_SortOrder")]
    public int SortOrder { get; set; }

    [DbParam("p_SetAsCover")]
    public bool SetAsCover { get; set; }

    [DbParam("p_CreatedBy")]
    public string CreatedBy { get; set; } = string.Empty;
}

public class InputMemoryImageGet
{
    [DbParam("p_FK_Families")]
    public long FamilyId { get; set; }

    [DbParam("p_ID_Memories")]
    public long MemoryId { get; set; }

    [DbParam("p_ID_MemoryImages")]
    public long ImageId { get; set; }
}

public class InputMemoryImageDelete
{
    [DbParam("p_FK_Families")]
    public long FamilyId { get; set; }

    [DbParam("p_ID_Memories")]
    public long MemoryId { get; set; }

    [DbParam("p_ID_MemoryImages")]
    public long ImageId { get; set; }

    [DbParam("p_CancelledBy")]
    public string CancelledBy { get; set; } = string.Empty;
}

public class InputMemoryCoverSet
{
    [DbParam("p_FK_Families")]
    public long FamilyId { get; set; }

    [DbParam("p_ID_Memories")]
    public long MemoryId { get; set; }

    [DbParam("p_ID_MemoryImages")]
    public long ImageId { get; set; }

    [DbParam("p_UpdatedBy")]
    public string UpdatedBy { get; set; } = string.Empty;
}

// --- Outputs ---

public class OutputMemoryListItem
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime MemoryDate { get; set; }
    public string? Location { get; set; }
    public long? CoverImageId { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? CoverUrl { get; set; }
    public int ImageCount { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class OutputPagedMemories
{
    public List<OutputMemoryListItem> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class OutputMemoryImageItem
{
    public long Id { get; set; }
    public string? FileName { get; set; }
    public string? StorageKey { get; set; }
    public string? ImageUrl { get; set; }
    public string? FileUrl { get; set; }
    public string? MimeType { get; set; }
    public long? FileSize { get; set; }
    public int SortOrder { get; set; }
    public bool IsCoverImage { get; set; }
    public bool IsCover { get; set; }
}

public class OutputMemoryCover
{
    public long Id { get; set; }
    public string? FileName { get; set; }
    public string? StorageKey { get; set; }
    public string? ImageUrl { get; set; }
    public string? FileUrl { get; set; }
    public string? MimeType { get; set; }
    public long? FileSize { get; set; }
}

public class OutputGetMemory
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime MemoryDate { get; set; }
    public string? Location { get; set; }
    public long? CoverImageId { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? CoverUrl { get; set; }
    public OutputMemoryCover? Cover { get; set; }
    public List<OutputMemoryImageItem> Images { get; set; } = [];
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
}

public class OutputCreateMemory : CommonResponse<IdResponse>
{
}

public class OutputUpdateMemory : CommonResponse<IdResponse>
{
}

public class OutputDeleteMemory : CommonResponse<IdResponse>
{
}

public class OutputMemoryImageUploadContext
{
    public long MemoryId { get; set; }
    public int ImageCount { get; set; }
    public int MaxImageCount { get; set; } = 10;
    public int NextSortOrder { get; set; }
}

public class OutputMemoryImageAction
{
    public long ImageId { get; set; }
    public string? Url { get; set; }
    public long FileSize { get; set; }
    public int ImageCount { get; set; }
    public int MaxImageCount { get; set; } = 10;
    public long StorageUsedBytes { get; set; }
    public long StorageLimitBytes { get; set; }
}

public class OutputMemoryImageDetail
{
    public long ImageId { get; set; }
    public long MemoryId { get; set; }
    public string? FileName { get; set; }
    public string? StorageKey { get; set; }
    public string? ImageUrl { get; set; }
    public string? FileUrl { get; set; }
    public long FileSize { get; set; }
    public bool IsCoverImage { get; set; }
}

public class OutputMemoryImageCommit : CommonResponse<OutputMemoryImageAction>
{
}

public class OutputMemoryImageDelete : CommonResponse<OutputMemoryImageAction>
{
}

public class OutputMemoryCoverSet : CommonResponse<IdResponse>
{
}
