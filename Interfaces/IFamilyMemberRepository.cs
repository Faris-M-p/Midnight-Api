using MidnightApi.Models;

namespace MidnightApi.Interfaces;

public interface IFamilyMemberRepository
{
    Task<List<FamilyMember>> GetAllAsync();
    Task<FamilyMember?> GetByIdAsync(string id);
    Task<FamilyMember> CreateAsync(FamilyMember member);
    Task<FamilyMember?> UpdateAsync(string id, FamilyMember member);
    Task<bool> DeleteAsync(string id);
}
