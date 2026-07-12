using MidnightApi.Models;

namespace MidnightApi.Interfaces;

public interface IFamilyTreeRepository
{
    Task<FamilyTreeSnapshot> GetTreeSnapshotAsync();
}
