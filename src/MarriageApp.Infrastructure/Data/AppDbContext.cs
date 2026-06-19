using MarriageApp.Core.Entities;
using MarriageApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MarriageApp.Infrastructure.Data;

/// <summary>
/// EF Core context combining ASP.NET Identity tables with the matchmaking domain.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<FamilyDetails> FamilyDetails => Set<FamilyDetails>();
    public DbSet<MatchRequirements> MatchRequirements => Set<MatchRequirements>();
    public DbSet<RequirementResidence> RequirementResidences => Set<RequirementResidence>();
    public DbSet<ProfilePhoto> ProfilePhotos => Set<ProfilePhoto>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchResult> MatchResults => Set<MatchResult>();
    public DbSet<PhotoAccessLog> PhotoAccessLogs => Set<PhotoAccessLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b); // Identity tables

        // ---- Profile ----
        b.Entity<Profile>(e =>
        {
            e.HasIndex(p => p.UserId).IsUnique();
            // Indexes that speed up candidate filtering in the matching query.
            e.HasIndex(p => new { p.Gender, p.Status });
            e.HasIndex(p => p.Age);
            e.HasIndex(p => p.HeightCm);

            // One-to-one: Profile <-> the Identity user (FK on Profile.UserId).
            e.HasOne<ApplicationUser>()
                .WithOne(u => u.Profile)
                .HasForeignKey<Profile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.FamilyDetails)
                .WithOne(f => f.Profile)
                .HasForeignKey<FamilyDetails>(f => f.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.Requirements)
                .WithOne(r => r.Profile)
                .HasForeignKey<MatchRequirements>(r => r.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(p => p.Photos)
                .WithOne(ph => ph.Profile)
                .HasForeignKey(ph => ph.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---- MatchRequirements: store [Flags] enums as int; child residence collection ----
        b.Entity<MatchRequirements>(e =>
        {
            e.Property(r => r.AcceptedEducationLevels).HasConversion<int>();
            e.Property(r => r.AcceptedReligiousCommitments).HasConversion<int>();
            e.Property(r => r.AcceptedDressCodes).HasConversion<int>();
            e.Property(r => r.AcceptedMaritalStatuses).HasConversion<int>();
            e.Property(r => r.AcceptedFamilyCommitmentLevels).HasConversion<int>();

            e.HasMany(r => r.AcceptedResidences)
                .WithOne(rr => rr.MatchRequirements)
                .HasForeignKey(rr => rr.MatchRequirementsId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---- Match: two FKs into Profile. Restrict delete to avoid multiple cascade paths. ----
        b.Entity<Match>(e =>
        {
            e.Property(m => m.Score).HasPrecision(5, 2);

            e.HasOne(m => m.MaleProfile)
                .WithMany()
                .HasForeignKey(m => m.MaleProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(m => m.FemaleProfile)
                .WithMany()
                .HasForeignKey(m => m.FemaleProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(m => new { m.MaleProfileId, m.FemaleProfileId }).IsUnique();
            e.HasIndex(m => m.Status);
        });

        // ---- MatchResult: cached top-N. Restrict deletes (two Profile FKs again). ----
        b.Entity<MatchResult>(e =>
        {
            e.Property(m => m.Score).HasPrecision(5, 2);

            e.HasOne(m => m.SubjectProfile)
                .WithMany()
                .HasForeignKey(m => m.SubjectProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(m => m.CandidateProfile)
                .WithMany()
                .HasForeignKey(m => m.CandidateProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(m => new { m.SubjectProfileId, m.Rank });
        });

        // ---- PhotoAccessLog ----
        b.Entity<PhotoAccessLog>(e =>
        {
            e.HasOne(l => l.Photo)
                .WithMany(p => p.AccessLogs)
                .HasForeignKey(l => l.PhotoId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(l => l.AccessedAt);
        });

        // ---- Notification ----
        b.Entity<Notification>(e =>
        {
            e.HasIndex(n => new { n.UserId, n.IsRead });
        });
    }
}
