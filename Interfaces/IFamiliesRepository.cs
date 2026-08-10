namespace MidnightApi.Interfaces;

using MidnightApi.Models;

public interface IFamiliesRepository
{
    Task<OutputGetFamily?> GetByIdAsync(InputGetFamily input);
    Task<OutputFamilyByCode?> GetByCodeAsync(InputGetFamilyByCode input);
    Task<OutputCreateFamily> CreateAsync(InputCreateFamily input);
    Task<OutputUpdateFamily> UpdateAsync(InputUpdateFamily input);
}
