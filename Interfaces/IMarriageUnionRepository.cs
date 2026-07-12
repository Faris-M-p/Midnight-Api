using MidnightApi.Models;

namespace MidnightApi.Interfaces;

public interface IMarriageUnionRepository
{
    Task<List<MarriageUnion>> GetAllAsync();
    Task<MarriageUnion?> GetByIdAsync(string id);
    Task<MarriageUnion> CreateAsync(MarriageUnion union);
    Task<MarriageUnion?> UpdateAsync(string id, MarriageUnion union);
    Task<bool> DeleteAsync(string id);
}
