namespace MidnightApi.Models;

public class FamilyTreeSnapshot
{
    public List<FamilyMember> Members { get; set; } = [];
    public List<MarriageUnion> Unions { get; set; } = [];
    public List<Milestone> Milestones { get; set; } = [];
}
