namespace MidnightApi.Interfaces;

using MidnightApi.Models.Api;

public interface IFamiliesRepository
{
    Task<OutputGetFamily?> GetByIdAsync(long id);
    Task<OutputGetFamily> CreateAsync(InputCreateFamily input, string createdBy);
    Task<OutputGetFamily?> UpdateAsync(long id, InputUpdateFamily input, string updatedBy);
    Task<bool> ExistsByCodeAsync(string code, long? excludeFamilyId = null);
}
