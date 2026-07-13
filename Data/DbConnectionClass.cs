using Microsoft.EntityFrameworkCore;
using MidnightApi.Models.Entities;

namespace MidnightApi.Data;

public class DbConnectionClass : DbContext
{
    public DbConnectionClass(DbContextOptions<DbConnectionClass> options) : base(options)
    {
    }

    public DbSet<Family> Families => Set<Family>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MemberAddress> MemberAddresses => Set<MemberAddress>();
    public DbSet<MemberImage> MemberImages => Set<MemberImage>();
    public DbSet<MemberEvent> MemberEvents => Set<MemberEvent>();
    public DbSet<MemberSocialLink> MemberSocialLinks => Set<MemberSocialLink>();
    public DbSet<MemberNote> MemberNotes => Set<MemberNote>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Family>().ToTable("Families");
        modelBuilder.Entity<Member>().ToTable("Members");
        modelBuilder.Entity<MemberAddress>().ToTable("MemberAddresses");
        modelBuilder.Entity<MemberImage>().ToTable("MemberImages");
        modelBuilder.Entity<MemberEvent>().ToTable("MemberEvents");
        modelBuilder.Entity<MemberSocialLink>().ToTable("MemberSocialLinks");
        modelBuilder.Entity<MemberNote>().ToTable("MemberNotes");
        modelBuilder.Entity<UserAccount>().ToTable("UserAccounts");

        modelBuilder.Entity<Member>()
            .HasOne(m => m.Parent)
            .WithMany(m => m.Children)
            .HasForeignKey(m => m.FK_Members_Parent)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Member>()
            .HasOne(m => m.Spouse)
            .WithMany()
            .HasForeignKey(m => m.FK_Members_Spouse)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Member>()
            .HasOne(m => m.Family)
            .WithMany(f => f.Members)
            .HasForeignKey(m => m.FK_Families)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserAccount>()
            .HasOne(u => u.Family)
            .WithOne(f => f.UserAccount)
            .HasForeignKey<UserAccount>(u => u.FK_Families)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserAccount>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<UserAccount>()
            .HasIndex(u => u.FK_Families)
            .IsUnique();

        modelBuilder.Entity<Family>()
            .HasIndex(f => f.FamilyCode)
            .IsUnique();
    }
}
