using Microsoft.EntityFrameworkCore;
using MidnightApi.Models;

namespace MidnightApi.Data;

public class DbConnectionClass : DbContext
{
    public DbConnectionClass(DbContextOptions<DbConnectionClass> options) : base(options)
    {
    }

    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();
    public DbSet<MarriageUnion> MarriageUnions => Set<MarriageUnion>();
    public DbSet<Milestone> Milestones => Set<Milestone>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FamilyMember>().ToTable("family_members");
        modelBuilder.Entity<MarriageUnion>().ToTable("marriage_unions");
        modelBuilder.Entity<Milestone>().ToTable("milestones");
    }
}
