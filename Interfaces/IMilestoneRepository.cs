using MidnightApi.Models;

namespace MidnightApi.Interfaces;

public interface IMilestoneRepository
{
    Task<List<Milestone>> GetAllAsync();
    Task<Milestone?> GetByIdAsync(string id);
    Task<Milestone> CreateAsync(Milestone milestone);
    Task<Milestone?> UpdateAsync(string id, Milestone milestone);
    Task<bool> DeleteAsync(string id);
}
