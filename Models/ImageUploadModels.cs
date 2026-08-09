namespace MidnightApi.Models;

public enum ImageUploadMode
{
    MemberProfile = 1,
    FamilyLogo = 2,
    Gallery = 3,
    Story = 4,
    Event = 5
}

public class ImageUploadRequest
{
    public required IFormFile File { get; init; }
    public ImageUploadMode Mode { get; init; }
    public long FamilyId { get; init; }
    public long? EntityId { get; init; }
}
