using AdoptionAgency.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AdoptionAgency.Api.Data;

public class AdoptionDbContext : DbContext
{
    public AdoptionDbContext(DbContextOptions<AdoptionDbContext> options)
        : base(options) { }

    public DbSet<Animal> Animals => Set<Animal>();
    public DbSet<AnimalPhoto> AnimalPhotos => Set<AnimalPhoto>();
    public DbSet<BehaviorProfile> BehaviorProfiles => Set<BehaviorProfile>();
    public DbSet<Intake> Intakes => Set<Intake>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Applicant> Applicants => Set<Applicant>();
    public DbSet<Placement> Placements => Set<Placement>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<OutreachEvent> OutreachEvents => Set<OutreachEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Animal>()
            .HasIndex(a => a.PublicId)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Animal>()
            .HasOne(a => a.BehaviorProfile)
            .WithOne(b => b.Animal)
            .HasForeignKey<BehaviorProfile>(b => b.AnimalId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Applicant)
            .WithOne(a => a.User)
            .HasForeignKey<Applicant>(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
