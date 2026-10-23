
using Microsoft.EntityFrameworkCore;
using RentalSaaS.Api.Models;

namespace RentalSaaS.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<RentalUnit> RentalUnits => Set<RentalUnit>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Lease> Leases => Set<Lease>();
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<PropertyManagerAssignment> PropertyManagerAssignments =>
        Set<PropertyManagerAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Property and rental unit relationship
        modelBuilder.Entity<Property>()
            .HasMany(p => p.Units)
            .WithOne(u => u.Property)
            .HasForeignKey(u => u.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RentalUnit>()
            .Property(u => u.MonthlyRent)
            .HasPrecision(12, 2);

        // Tenant configuration
        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.NationalId)
            .IsUnique();

        modelBuilder.Entity<Tenant>()
            .HasOne<Property>()
            .WithMany()
            .HasForeignKey("PropertyId")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Application user configuration
        modelBuilder.Entity<ApplicationUser>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Property manager assignments
        modelBuilder.Entity<PropertyManagerAssignment>()
            .HasIndex(a => new { a.ApplicationUserId, a.PropertyId })
            .IsUnique();

        modelBuilder.Entity<PropertyManagerAssignment>()
            .HasOne(a => a.ApplicationUser)
            .WithMany(u => u.PropertyAssignments)
            .HasForeignKey(a => a.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PropertyManagerAssignment>()
            .HasOne(a => a.Property)
            .WithMany()
            .HasForeignKey(a => a.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Lease configuration
        modelBuilder.Entity<Lease>()
            .Property(l => l.MonthlyRent)
            .HasPrecision(12, 2);

        modelBuilder.Entity<Lease>()
            .HasOne(l => l.Tenant)
            .WithMany()
            .HasForeignKey(l => l.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Lease>()
            .HasOne(l => l.RentalUnit)
            .WithMany()
            .HasForeignKey(l => l.RentalUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // Only one active lease is allowed per rental unit.
        // Ended leases remain in the database as history.
        modelBuilder.Entity<Lease>()
            .HasIndex(l => l.RentalUnitId)
            .IsUnique()
            .HasFilter("\"Status\" = 'Active'");
    }
}
