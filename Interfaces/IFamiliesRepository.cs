
namespace MidnightApi.Interfaces;

using MidnightApi.Models.Api;

public interface IFamiliesRepository
{
    Task<List<OutputGetFamily>> GetAllAsync();
    Task<OutputGetFamily?> GetByIdAsync(long id);
    Task<OutputGetFamily?> GetByCodeAsync(string code);
    Task<OutputGetFamily> CreateAsync(InputCreateFamily input, string createdBy);
    Task<OutputGetFamily?> UpdateAsync(long id, InputUpdateFamily input, string updatedBy);
    Task<bool> SoftDeleteAsync(long id, string deletedBy);
    Task<bool> ExistsByCodeAsync(string code, long? excludeFamilyId = null);
}
